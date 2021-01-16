using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using MySqlConnector;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.Contracts;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using OTHub.BackendSync.Database.Models;
using OTHub.BackendSync.Logging;
using OTHub.Settings;
using OTHub.Settings.Abis;

namespace OTHub.BackendSync.Ethereum.Tasks
{
    public class SyncLitigationContractTask : TaskRun
    {
        public SyncLitigationContractTask() : base("Sync Litigation Contract")
        {
        }

        public override async Task Execute(Source source, Blockchain blockchain, Network network)
        {
            ClientBase.ConnectionTimeout = new TimeSpan(0, 0, 5, 0);

            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                foreach (var contract in OTContract.GetByType(connection, (int)ContractTypeEnum.Litigation))
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

                    var holdingContract = new Contract(eth, AbiHelper.GetContractAbi(ContractTypeEnum.Litigation), contract.Address);
                    
                    var litigationInitiatedEvent = holdingContract.GetEvent("LitigationInitiated");
                    var litigationAnsweredEvent = holdingContract.GetEvent("LitigationAnswered");
                    var litigationTimedOutEvent = holdingContract.GetEvent("LitigationTimedOut");
                    var litigationCompletedEvent = holdingContract.GetEvent("LitigationCompleted");
                    var replacementStartedEvent = holdingContract.GetEvent("ReplacementStarted");

                    var diff = (ulong)LatestBlockNumber.Value - contract.SyncBlockNumber;

                    ulong size = 100000;
                    ulong smallSize = 10000;

                    beforeSync:


                    if (diff > size)
                    {
                        int batchesTotal = (int)Math.Ceiling((decimal)diff / size);

                        var batches = Enumerable.Range(0, batchesTotal).ToArray();

                        UInt64 currentStart = contract.SyncBlockNumber;
                        UInt64 currentEnd = currentStart + size;

                        foreach (var batch in batches)
                        {
                            await Task.Delay(500);

                            try
                            {
                                await Sync(connection, litigationInitiatedEvent, litigationAnsweredEvent,
                                    litigationTimedOutEvent,
                                    litigationCompletedEvent,
                                    replacementStartedEvent,
                                    contract, source, currentStart,
                                    currentEnd);
                            }
                            catch (RpcResponseException ex) when (ex.Message.Contains("query returned more than"))
                            {
                                if (size != smallSize)
                                {
                                    Logger.WriteLine(source, "Swapping to block sync size of " + smallSize);

                                    size = smallSize;

                                    goto beforeSync;
                                }
                            }

                            currentStart = currentEnd;
                            currentEnd = currentStart + size;

                            if (currentEnd > LatestBlockNumber.Value)
                            {
                                currentEnd = (ulong)LatestBlockNumber.Value;
                            }

                            if (currentStart == currentEnd)
                                break;
                        }
                    }
                    else
                    {
                        await Sync(connection, litigationInitiatedEvent, litigationAnsweredEvent,
                            litigationTimedOutEvent,
                            litigationCompletedEvent,
                            replacementStartedEvent,
                            contract, source, contract.SyncBlockNumber, (ulong)LatestBlockNumber.Value);
                    }
                }

            }
        }

        private async Task Sync(MySqlConnection connection, Event litigationInitiatedEvent, Event litigationAnsweredEvent, Event litigationTimedOutEvent, Event litigationCompletedEvent, Event replacementStartedEvent, OTContract contract, Source source, ulong start, ulong end)
        {
            Logger.WriteLine(source, "Syncing litigation " + start + " to " + end);

            var toBlock = new BlockParameter(end);

            await Task.Delay(250);

            var litigationInitiatedEventsEventLogs = await litigationInitiatedEvent.GetAllChangesDefault(
                litigationInitiatedEvent.CreateFilterInput(new BlockParameter(start),
                    toBlock));

            await Task.Delay(250);

            var litigationAnsweredEvents = await litigationAnsweredEvent.GetAllChangesDefault(
                litigationAnsweredEvent.CreateFilterInput(new BlockParameter(start),
                    toBlock));

            await Task.Delay(250);

            var litigationTimedOutEvents = await litigationTimedOutEvent.GetAllChangesDefault(
                litigationTimedOutEvent.CreateFilterInput(new BlockParameter(start),
                    toBlock));

            await Task.Delay(250);

            var litigationCompletedEvents = await litigationCompletedEvent.GetAllChangesDefault(
                litigationCompletedEvent.CreateFilterInput(new BlockParameter(start),
                    toBlock));

            await Task.Delay(250);

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

            foreach (EventLog<List<ParameterOutput>> eventLog in litigationInitiatedEventsEventLogs)
            {
                var block = await BlockHelper.GetEthBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber,
                    cl);

                var offerId =
                    HexHelper.ByteArrayToString((byte[])eventLog.Event
                        .First(e => e.Parameter.Name == "offerId").Result);

                var holderIdentity = (string)eventLog.Event
                        .First(e => e.Parameter.Name == "holderIdentity").Result;

                var requestedObjectIndex = (BigInteger)eventLog.Event
                        .First(e => e.Parameter.Name == "requestedObjectIndex").Result;


                var requestedBlockIndex = (BigInteger)eventLog.Event
                    .First(e => e.Parameter.Name == "requestedBlockIndex").Result;

                var transaction = await eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);
                await Task.Delay(100);
                var receipt = await eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

                var row = new OTContract_Litigation_LitigationInitiated()
                {
                    TransactionHash = eventLog.Log.TransactionHash,
                    BlockNumber = (UInt64)eventLog.Log.BlockNumber.Value,
                    Timestamp = block.Timestamp,
                    OfferId = offerId,
                    RequestedObjectIndex = (UInt64)requestedObjectIndex,
                    RequestedBlockIndex = (UInt64)requestedBlockIndex,
                    HolderIdentity = holderIdentity,
                    GasPrice = (UInt64)transaction.GasPrice.Value,
                    GasUsed = (UInt64)receipt.GasUsed.Value
                };

                OTContract_Litigation_LitigationInitiated.InsertIfNotExist(connection, row);
            }

            foreach (EventLog<List<ParameterOutput>> eventLog in litigationAnsweredEvents)
            {
                var block = await BlockHelper.GetEthBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber,
                    cl);

                var offerId =
                    HexHelper.ByteArrayToString((byte[])eventLog.Event
                        .First(e => e.Parameter.Name == "offerId").Result);

                var holderIdentity = (string)eventLog.Event
                    .First(e => e.Parameter.Name == "holderIdentity").Result;

                var transaction = await eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);
                await Task.Delay(100);
                var receipt = await eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

                var row = new OTContract_Litigation_LitigationAnswered()
                {
                    TransactionHash = eventLog.Log.TransactionHash,
                    BlockNumber = (UInt64)eventLog.Log.BlockNumber.Value,
                    Timestamp = block.Timestamp,
                    OfferId = offerId,
                    HolderIdentity = holderIdentity,
                    GasPrice = (UInt64)transaction.GasPrice.Value,
                    GasUsed = (UInt64)receipt.GasUsed.Value
                };

                OTContract_Litigation_LitigationAnswered.InsertIfNotExist(connection, row);
            }

            foreach (EventLog<List<ParameterOutput>> eventLog in litigationTimedOutEvents)
            {
                var block = await BlockHelper.GetEthBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber,
                    cl);

                var offerId =
                    HexHelper.ByteArrayToString((byte[])eventLog.Event
                        .First(e => e.Parameter.Name == "offerId").Result);

                var holderIdentity = (string)eventLog.Event
                    .First(e => e.Parameter.Name == "holderIdentity").Result;

                var transaction = await eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);
                await Task.Delay(100);
                var receipt = await eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

                var row = new OTContract_Litigation_LitigationTimedOut()
                {
                    TransactionHash = eventLog.Log.TransactionHash,
                    BlockNumber = (UInt64)eventLog.Log.BlockNumber.Value,
                    Timestamp = block.Timestamp,
                    OfferId = offerId,
                    HolderIdentity = holderIdentity,
                    GasPrice = (UInt64)transaction.GasPrice.Value,
                    GasUsed = (UInt64)receipt.GasUsed.Value
                };

                OTContract_Litigation_LitigationTimedOut.InsertIfNotExist(connection, row);
            }

            foreach (EventLog<List<ParameterOutput>> eventLog in litigationCompletedEvents)
            {
                var block = await BlockHelper.GetEthBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber,
                    cl);

                var offerId =
                    HexHelper.ByteArrayToString((byte[])eventLog.Event
                        .First(e => e.Parameter.Name == "offerId").Result);

                var holderIdentity = (string)eventLog.Event
                    .First(e => e.Parameter.Name == "holderIdentity").Result;

                var dhWasPenalized = (bool)eventLog.Event
                    .First(e => e.Parameter.Name == "DH_was_penalized").Result;

                var transaction = await eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);
                await Task.Delay(100);
                var receipt = await eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

                var row = new OTContract_Litigation_LitigationCompleted()
                {
                    TransactionHash = eventLog.Log.TransactionHash,
                    BlockNumber = (UInt64)eventLog.Log.BlockNumber.Value,
                    Timestamp = block.Timestamp,
                    OfferId = offerId,
                    HolderIdentity = holderIdentity,
                    DHWasPenalized = dhWasPenalized,
                    GasPrice = (UInt64)transaction.GasPrice.Value,
                    GasUsed = (UInt64)receipt.GasUsed.Value
                };

                OTContract_Litigation_LitigationCompleted.InsertIfNotExist(connection, row);
            }

            foreach (EventLog<List<ParameterOutput>> eventLog in replacementStartedEvents)
            {
                var block = await BlockHelper.GetEthBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber,
                    cl);

                var offerId =
                    HexHelper.ByteArrayToString((byte[])eventLog.Event
                        .First(e => e.Parameter.Name == "offerId").Result);

                var holderIdentity = (string)eventLog.Event
                    .First(e => e.Parameter.Name == "holderIdentity").Result;

                var challengerIdentity = (string)eventLog.Event
                    .First(e => e.Parameter.Name == "challengerIdentity").Result;

                var litigationRootHash = HexHelper.ByteArrayToString((byte[])eventLog.Event
                        .First(e => e.Parameter.Name == "litigationRootHash").Result);

                var transaction = await eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);
                await Task.Delay(100);
                var receipt = await eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

                var row = new OTContract_Litigation_ReplacementStarted()
                {
                    TransactionHash = eventLog.Log.TransactionHash,
                    BlockNumber = (UInt64)eventLog.Log.BlockNumber.Value,
                    Timestamp = block.Timestamp,
                    OfferId = offerId,
                    HolderIdentity = holderIdentity,
                    ChallengerIdentity = challengerIdentity,
                    LitigationRootHash = litigationRootHash,
                    GasPrice = (UInt64)transaction.GasPrice.Value,
                    GasUsed = (UInt64)receipt.GasUsed.Value
                };

                OTContract_Litigation_ReplacementStarted.InsertIfNotExist(connection, row);
            }

            contract.LastSyncedTimestamp = DateTime.Now;
            contract.SyncBlockNumber = end;

            OTContract.Update(connection, contract, false, false);

            //childTask.Wait();
        }
    }
}