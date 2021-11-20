﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using MySqlConnector;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.Contracts;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using OTHub.BackendSync.Database.Models;
using OTHub.BackendSync.Logging;
using OTHub.Settings;
using OTHub.Settings.Abis;
using OTHub.Settings.Constants;

namespace OTHub.BackendSync.Blockchain.Tasks.BlockchainSync.Children
{
    public class SyncLitigationContractTask : TaskRunBlockchain
    {
        public SyncLitigationContractTask() : base(TaskNames.LitigationContractSync)
        {
        }

        public override async Task<bool> Execute(Source source, BlockchainType blockchain, BlockchainNetwork network)
        {
            ClientBase.ConnectionTimeout = new TimeSpan(0, 0, 5, 0);

            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                int blockchainID = await GetBlockchainID(connection, blockchain, network);
                ulong blockSize = (ulong)await GetBlockchainSyncSize(connection, blockchain, network);

                var cl = await GetWeb3(connection, blockchainID, blockchain);
                var eth = new EthApiService(cl.Client);

                foreach (var contract in await OTContract.GetByTypeAndBlockchain(connection,
                    (int) ContractTypeEnum.Litigation, blockchainID))
                {
                    if (contract.IsArchived && contract.LastSyncedTimestamp.HasValue &&
                        (DateTime.Now - contract.LastSyncedTimestamp.Value).TotalDays <= 5)
                    {
#if DEBUG
                        Logger.WriteLine(source, "     Skipping contract: " + contract.Address);
#endif
                        continue;
                    }

                    Logger.WriteLine(source, "     Using contract: " + contract.Address);

                    var holdingContract = new Contract(eth,
                        AbiHelper.GetContractAbi(ContractTypeEnum.Litigation, blockchain, network), contract.Address);

                    var litigationInitiatedEvent = holdingContract.GetEvent("LitigationInitiated");
                    var litigationAnsweredEvent = holdingContract.GetEvent("LitigationAnswered");
                    var litigationTimedOutEvent = holdingContract.GetEvent("LitigationTimedOut");
                    var litigationCompletedEvent = holdingContract.GetEvent("LitigationCompleted");
                    var replacementStartedEvent = holdingContract.GetEvent("ReplacementStarted");


                    BlockBatcher batcher = BlockBatcher.Start(contract.SyncBlockNumber, (ulong) LatestBlockNumber.Value, blockSize,
                        async delegate(ulong start, ulong end)
                        {
                            await Sync(connection, litigationInitiatedEvent, litigationAnsweredEvent,
                                litigationTimedOutEvent,
                                litigationCompletedEvent,
                                replacementStartedEvent,
                                contract, source, start,
                                end, blockchainID, cl);
                        });

                    await batcher.Execute();
                }

            }

            return true;
        }

        private async Task Sync(MySqlConnection connection, Event litigationInitiatedEvent,
            Event litigationAnsweredEvent, Event litigationTimedOutEvent, Event litigationCompletedEvent,
            Event replacementStartedEvent, OTContract contract, Source source, ulong start, ulong end, int blockchainID,
            Web3 cl)
        {
            Logger.WriteLine(source, "Syncing litigation " + start + " to " + end);

            var toBlock = new BlockParameter(end);

            var litigationInitiatedEventsEventLogs = await litigationInitiatedEvent.GetAllChangesDefault(
                litigationInitiatedEvent.CreateFilterInput(new BlockParameter(start),
                    toBlock));


            var litigationAnsweredEvents = await litigationAnsweredEvent.GetAllChangesDefault(
                litigationAnsweredEvent.CreateFilterInput(new BlockParameter(start),
                    toBlock));

            var litigationTimedOutEvents = await litigationTimedOutEvent.GetAllChangesDefault(
                litigationTimedOutEvent.CreateFilterInput(new BlockParameter(start),
                    toBlock));


            var litigationCompletedEvents = await litigationCompletedEvent.GetAllChangesDefault(
                litigationCompletedEvent.CreateFilterInput(new BlockParameter(start),
                    toBlock));


            var replacementStartedEvents = await replacementStartedEvent.GetAllChangesDefault(
                replacementStartedEvent.CreateFilterInput(new BlockParameter(start),
                    toBlock));


            if (litigationInitiatedEventsEventLogs.Any())
            {
                Logger.WriteLine(source, "Found " + litigationInitiatedEventsEventLogs.Count + " litigation initiated events");
            }

            if (litigationAnsweredEvents.Any())
            {
                Logger.WriteLine(source, "Found " + litigationAnsweredEvents.Count + " litigation answered completed events");
            }

            if (litigationTimedOutEvents.Any())
            {
                Logger.WriteLine(source, "Found " + litigationTimedOutEvents.Count + " litigation timed out events");
            }

            if (litigationCompletedEvents.Any())
            {
                Logger.WriteLine(source, "Found " + litigationCompletedEvents.Count + " litigation completed events");
            }

            if (replacementStartedEvents.Any())
            {
                Logger.WriteLine(source, "Found " + replacementStartedEvents.Count + " replacement started events");
            }

            //var childTask = Task.Run(async () =>
            //{
            //    IEnumerable<IEventLog> items = litigationInitiatedEventsEventLogs
            //        .Union(litigationAnsweredEvents)
            //        .Union(litigationTimedOutEvents)
            //        .Union(litigationCompletedEvents)
            //        .Union(replacementStartedEvents);

            //    foreach (var eventLog in items)
            //    {
            //        await Program.GetEthBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber, cl);
            //    }
            //});

            var eth = new EthApiService(cl.Client);

            foreach (EventLog<List<ParameterOutput>> eventLog in litigationInitiatedEventsEventLogs)
            {
                await ProcessLitigationInitiated(connection, blockchainID, cl, eventLog, eth);
            }

            foreach (EventLog<List<ParameterOutput>> eventLog in litigationAnsweredEvents)
            {
                await ProcessLitigationAnswered(connection, blockchainID, cl, eventLog, eth);
            }

            foreach (EventLog<List<ParameterOutput>> eventLog in litigationTimedOutEvents)
            {
                await ProcessLitigationTimedOut(connection, blockchainID, cl, eventLog, eth);
            }

            foreach (EventLog<List<ParameterOutput>> eventLog in litigationCompletedEvents)
            {
                await ProcessLitigationCompleted(connection, blockchainID, cl, eventLog, eth);
            }

            foreach (EventLog<List<ParameterOutput>> eventLog in replacementStartedEvents)
            {
                await ProcessReplacementStarted(connection, blockchainID, cl, eventLog, eth);
            }

            contract.LastSyncedTimestamp = DateTime.Now;
            contract.SyncBlockNumber = end;

            await OTContract.Update(connection, contract, false, false);

            //childTask.Wait();
        }

        public static async Task ProcessReplacementStarted(MySqlConnection connection, int blockchainID, Web3 cl,
            EventLog<List<ParameterOutput>> eventLog, EthApiService eth)
        {
            if (OTContract_Litigation_ReplacementStarted.TransactionExists(connection, eventLog.Log.TransactionHash, blockchainID))
            {
                return;
            }

            var block = await BlockHelper.GetBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber,
                cl, blockchainID);

            var offerId =
                HexHelper.ByteArrayToString((byte[]) eventLog.Event
                    .First(e => e.Parameter.Name == "offerId").Result);

            var holderIdentity = (string) eventLog.Event
                .First(e => e.Parameter.Name == "holderIdentity").Result;

            var challengerIdentity = (string) eventLog.Event
                .First(e => e.Parameter.Name == "challengerIdentity").Result;

            var litigationRootHash = HexHelper.ByteArrayToString((byte[]) eventLog.Event
                .First(e => e.Parameter.Name == "litigationRootHash").Result);

            var transaction = await eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);

            var receipt = await eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

            var row = new OTContract_Litigation_ReplacementStarted()
            {
                TransactionHash = eventLog.Log.TransactionHash,
                BlockNumber = (UInt64) eventLog.Log.BlockNumber.Value,
                Timestamp = block.Timestamp,
                OfferId = offerId,
                HolderIdentity = holderIdentity,
                ChallengerIdentity = challengerIdentity,
                LitigationRootHash = litigationRootHash,
                GasPrice = (UInt64) transaction.GasPrice.Value,
                GasUsed = (UInt64) receipt.GasUsed.Value,
                BlockchainID = blockchainID
            };

            await OTContract_Litigation_ReplacementStarted.InsertIfNotExist(connection, row);
        }

        public static async Task ProcessLitigationCompleted(MySqlConnection connection, int blockchainID, Web3 cl,
            EventLog<List<ParameterOutput>> eventLog, EthApiService eth)
        {
            if (OTContract_Litigation_LitigationCompleted.TransactionExists(connection, eventLog.Log.TransactionHash, blockchainID))
            {
                return;
            }

            var block = await BlockHelper.GetBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber,
                cl, blockchainID);

            var offerId =
                HexHelper.ByteArrayToString((byte[]) eventLog.Event
                    .First(e => e.Parameter.Name == "offerId").Result);

            var holderIdentity = (string) eventLog.Event
                .First(e => e.Parameter.Name == "holderIdentity").Result;

            var dhWasPenalized = (bool) eventLog.Event
                .First(e => e.Parameter.Name == "DH_was_penalized").Result;

            var transaction = await eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);

            var receipt = await eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

            var row = new OTContract_Litigation_LitigationCompleted()
            {
                TransactionHash = eventLog.Log.TransactionHash,
                BlockNumber = (UInt64) eventLog.Log.BlockNumber.Value,
                Timestamp = block.Timestamp,
                OfferId = offerId,
                HolderIdentity = holderIdentity,
                DHWasPenalized = dhWasPenalized,
                GasPrice = (UInt64) transaction.GasPrice.Value,
                GasUsed = (UInt64) receipt.GasUsed.Value,
                BlockchainID = blockchainID
            };

            await OTContract_Litigation_LitigationCompleted.InsertIfNotExist(connection, row);
        }

        public static async Task ProcessLitigationTimedOut(MySqlConnection connection, int blockchainID, Web3 cl,
            EventLog<List<ParameterOutput>> eventLog, EthApiService eth)
        {
            if (OTContract_Litigation_LitigationTimedOut.TransactionExists(connection, eventLog.Log.TransactionHash, blockchainID))
            {
                return;
            }

            var block = await BlockHelper.GetBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber,
                cl, blockchainID);

            var offerId =
                HexHelper.ByteArrayToString((byte[]) eventLog.Event
                    .First(e => e.Parameter.Name == "offerId").Result);

            var holderIdentity = (string) eventLog.Event
                .First(e => e.Parameter.Name == "holderIdentity").Result;

            var transaction = await eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);

            var receipt = await eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

            var row = new OTContract_Litigation_LitigationTimedOut()
            {
                TransactionHash = eventLog.Log.TransactionHash,
                BlockNumber = (UInt64) eventLog.Log.BlockNumber.Value,
                Timestamp = block.Timestamp,
                OfferId = offerId,
                HolderIdentity = holderIdentity,
                GasPrice = (UInt64) transaction.GasPrice.Value,
                GasUsed = (UInt64) receipt.GasUsed.Value,
                BlockchainID = blockchainID
            };

            await OTContract_Litigation_LitigationTimedOut.InsertIfNotExist(connection, row);
        }

        public static async Task ProcessLitigationAnswered(MySqlConnection connection, int blockchainID, Web3 cl,
            EventLog<List<ParameterOutput>> eventLog, EthApiService eth)
        {
            if (OTContract_Litigation_LitigationAnswered.TransactionExists(connection, eventLog.Log.TransactionHash, blockchainID))
            {
                return;
            }

            var block = await BlockHelper.GetBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber,
                cl, blockchainID);

            var offerId =
                HexHelper.ByteArrayToString((byte[]) eventLog.Event
                    .First(e => e.Parameter.Name == "offerId").Result);

            var holderIdentity = (string) eventLog.Event
                .First(e => e.Parameter.Name == "holderIdentity").Result;

            var transaction = await eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);

            var receipt = await eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

            var row = new OTContract_Litigation_LitigationAnswered()
            {
                TransactionHash = eventLog.Log.TransactionHash,
                BlockNumber = (UInt64) eventLog.Log.BlockNumber.Value,
                Timestamp = block.Timestamp,
                OfferId = offerId,
                HolderIdentity = holderIdentity,
                GasPrice = (UInt64) transaction.GasPrice.Value,
                GasUsed = (UInt64) receipt.GasUsed.Value,
                BlockchainID = blockchainID
            };

            await OTContract_Litigation_LitigationAnswered.InsertIfNotExist(connection, row);
        }

        public static async Task ProcessLitigationInitiated(MySqlConnection connection, int blockchainID, Web3 cl,
            EventLog<List<ParameterOutput>> eventLog, EthApiService eth)
        {
            if (OTContract_Litigation_LitigationInitiated.TransactionExists(connection, eventLog.Log.TransactionHash, blockchainID))
            {
                return;
            }

            var block = await BlockHelper.GetBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber,
                cl, blockchainID);

            var offerId =
                HexHelper.ByteArrayToString((byte[]) eventLog.Event
                    .First(e => e.Parameter.Name == "offerId").Result);

            var holderIdentity = (string) eventLog.Event
                .First(e => e.Parameter.Name == "holderIdentity").Result;

            var requestedObjectIndex = (BigInteger) eventLog.Event
                .First(e => e.Parameter.Name == "requestedObjectIndex").Result;


            var requestedBlockIndex = (BigInteger) eventLog.Event
                .First(e => e.Parameter.Name == "requestedBlockIndex").Result;

            var transaction = await eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);

            var receipt = await eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

            var row = new OTContract_Litigation_LitigationInitiated
            {
                TransactionHash = eventLog.Log.TransactionHash,
                BlockNumber = (UInt64) eventLog.Log.BlockNumber.Value,
                Timestamp = block.Timestamp,
                OfferId = offerId,
                RequestedObjectIndex = (UInt64) requestedObjectIndex,
                RequestedBlockIndex = (UInt64) requestedBlockIndex,
                HolderIdentity = holderIdentity,
                GasPrice = (UInt64) transaction.GasPrice.Value,
                GasUsed = (UInt64) receipt.GasUsed.Value,
                BlockchainID = blockchainID
            };

            await OTContract_Litigation_LitigationInitiated.InsertIfNotExist(connection, row);
        }
    }
}