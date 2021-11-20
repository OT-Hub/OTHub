﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using OTHub.BackendSync.Blockchain.Web3Helper;
using OTHub.BackendSync.Database.Models;
using OTHub.BackendSync.Logging;
using OTHub.Settings;
using OTHub.Settings.Abis;
using OTHub.Settings.Constants;

namespace OTHub.BackendSync.Blockchain.Tasks.BlockchainSync.Children
{
    public class SyncHoldingContractTask : TaskRunBlockchain
    {
        public override async Task<bool> Execute(Source source, BlockchainType blockchain, BlockchainNetwork network, IWeb3 web3, int blockchainID)
        {
            ClientBase.ConnectionTimeout = new TimeSpan(0, 0, 5, 0);

            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                ulong blockSize = (ulong)await GetBlockchainSyncSize(connection, blockchain, network);
                HexBigInteger blockNumber = web3.GetLoadBalancedBlockNumber();

                var eth = new EthApiService(web3.Client);

                foreach (var contract in await OTContract.GetByTypeAndBlockchain(connection, (int)ContractTypeEnum.Holding, blockchainID))
                {
                    if (contract.IsArchived && contract.LastSyncedTimestamp.HasValue && (DateTime.Now - contract.LastSyncedTimestamp.Value).TotalDays <= 5)
                    {
#if  DEBUG

                        Logger.WriteLine(source, "     Skipping contract: " + contract.Address);
#endif
                        continue;
                    }

                    Logger.WriteLine(source, "     Using contract: " + contract.Address);

                    var holdingContract = new Contract(eth, AbiHelper.GetContractAbi(ContractTypeEnum.Holding, blockchain, network), contract.Address);

                    var offerCreatedEvent = holdingContract.GetEvent("OfferCreated");
                    var offerFinalizedEvent = holdingContract.GetEvent("OfferFinalized");
                    var paidOutEvent = holdingContract.GetEvent("PaidOut");
                    var ownershipTransferredEvent = holdingContract.GetEvent("OwnershipTransferred");
                    var offerTaskEvent = holdingContract.GetEvent("OfferTask");


                    BlockBatcher batcher = BlockBatcher.Start(contract.SyncBlockNumber, (ulong)blockNumber.Value, blockSize,
                        async delegate (ulong start, ulong end)
                        {
                            await Sync(connection, contract, offerCreatedEvent, offerFinalizedEvent, paidOutEvent,
                                ownershipTransferredEvent, offerTaskEvent, source, start, end, blockchainID, web3);
                        });

                    await batcher.Execute();
                }
            }

            return true;
        }

        private async Task Sync(MySqlConnection connection, OTContract contract, Event offerCreatedEvent,
            Event offerFinalizedEvent, Event paidOutEvent, Event ownershipTransferredEvent, Event offerTaskEvent,
            Source source, ulong start, ulong end, int blockchainID, IWeb3 cl)
        {
            Logger.WriteLine(source, "Syncing holding " + start + " to " + end);

            var toBlock = new BlockParameter(end);


            var createEvents = await offerCreatedEvent.GetAllChanges<Models.Program.OfferCreated>(
                offerCreatedEvent.CreateFilterInput(new BlockParameter(start),
                    toBlock));


            var finalizedEvents = await offerFinalizedEvent.GetAllChangesDefault(
                offerFinalizedEvent.CreateFilterInput(new BlockParameter(start),
                    toBlock));



            var payoutEvents = await paidOutEvent.GetAllChangesDefault(
                paidOutEvent.CreateFilterInput(new BlockParameter(start), toBlock));



            var ownershipTransferredEvents = await ownershipTransferredEvent.GetAllChangesDefault(
                ownershipTransferredEvent.CreateFilterInput(new BlockParameter(start),
                    toBlock));


            var offerTaskEvents = await offerTaskEvent.GetAllChangesDefault(
                offerTaskEvent.CreateFilterInput(new BlockParameter(start), toBlock));


            if (createEvents.Any())
            {
                Logger.WriteLine(source, "Found " + createEvents.Count + " offer created events");
            }


            if (finalizedEvents.Any())
            {
                Logger.WriteLine(source, "Found " + finalizedEvents.Count + " offer finalized events");
            }


            if (payoutEvents.Any())
            {
                Logger.WriteLine(source, "Found " + payoutEvents.Count + " offer payout events");
            }


            if (ownershipTransferredEvents.Any())
            {
                Logger.WriteLine(source, "Found " + ownershipTransferredEvents.Count + " ownership transferred events");
            }


            if (offerTaskEvents.Any())
            {
                Logger.WriteLine(source, "Found " + offerTaskEvents.Count + " offer task events");
            }

            foreach (EventLog<Models.Program.OfferCreated> eventLog in createEvents)
            {
                await ProcessOfferCreated(connection, blockchainID, cl, contract.Address, eventLog);
            }

            foreach (EventLog<List<ParameterOutput>> eventLog in finalizedEvents)
            {
                await ProcessOfferFinalised(connection, blockchainID, cl, contract.Address, eventLog);
            }

            foreach (EventLog<List<ParameterOutput>> eventLog in payoutEvents)
            {
                await ProcessPayout(connection, blockchainID, cl, contract.Address, eventLog);
            }

            var eth = new EthApiService(cl.Client);

            foreach (EventLog<List<ParameterOutput>> eventLog in ownershipTransferredEvents)
            {
                var block = await BlockHelper.GetBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber,
                    cl, blockchainID);
                var previousOwner = (string)eventLog.Event
                    .FirstOrDefault(e => e.Parameter.Name == "previousOwner").Result;
                var newOwner = (string)eventLog.Event.FirstOrDefault(e => e.Parameter.Name == "newOwner")
                    .Result;

                var transaction = eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);

                var receipt = eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

                await transaction;
                await receipt;

                OTContract_Holding_OwnershipTransferred.InsertIfNotExist(connection,
                    new OTContract_Holding_OwnershipTransferred
                    {
                        TransactionHash = eventLog.Log.TransactionHash,
                        BlockNumber = (UInt64)eventLog.Log.BlockNumber.Value,
                        ContractAddress = contract.Address,
                        NewOwner = newOwner,
                        PreviousOwner = previousOwner,
                        GasPrice = (UInt64)transaction.Result.GasPrice.Value,
                        GasUsed = (UInt64)receipt.Result.GasUsed.Value,
                        BlockchainID = blockchainID
                    });
            }

            foreach (EventLog<List<ParameterOutput>> eventLog in offerTaskEvents)
            {
                await ProcessOfferTasks(connection, blockchainID, cl, contract.Address, eventLog, eth);
            }

            contract.LastSyncedTimestamp = DateTime.Now;
            contract.SyncBlockNumber = end;

            await OTContract.Update(connection, contract, false, false);
        }

        public static async Task ProcessOfferTasks(MySqlConnection connection, int blockchainID, IWeb3 cl,
            string contractAddress, EventLog<List<ParameterOutput>> eventLog, EthApiService eth)
        {
            using (await LockManager.GetLock(LockType.OfferTask).Lock())
            {
                var offerId =
                    HexHelper.ByteArrayToString((byte[])eventLog.Event
                        .First(e => e.Parameter.Name == "offerId").Result);

                if (OTContract_Holding_OfferTask.Exists(connection, offerId, blockchainID))
                    return;

                var block = await BlockHelper.GetBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber,
                    cl, blockchainID);

                var dataSetId =
                    HexHelper.ByteArrayToString((byte[]) eventLog.Event
                        .First(e => e.Parameter.Name == "dataSetId").Result);
                var dcNodeId =
                    HexHelper.ByteArrayToString((byte[]) eventLog.Event
                        .First(e => e.Parameter.Name == "dcNodeId").Result);

                var task = HexHelper.ByteArrayToString(
                    (byte[]) eventLog.Event.First(e => e.Parameter.Name == "task").Result);

                var transaction = eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);

                var receipt = eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

                await transaction;
                await receipt;

                OTContract_Holding_OfferTask.InsertIfNotExist(connection, new OTContract_Holding_OfferTask
                {
                    BlockNumber = (UInt64) eventLog.Log.BlockNumber.Value,
                    Task = task,
                    TransactionHash = eventLog.Log.TransactionHash,
                    ContractAddress = contractAddress,
                    DCNodeId = dcNodeId,
                    DataSetId = dataSetId,
                    OfferId = offerId,
                    GasPrice = (UInt64) transaction.Result.GasPrice.Value,
                    GasUsed = (UInt64) receipt.Result.GasUsed.Value,
                    BlockchainID = blockchainID
                });
            }
        }

        public static async Task ProcessOfferCreated(MySqlConnection connection, int blockchainID, IWeb3 cl,
            string contractAddress, EventLog<Models.Program.OfferCreated> eventLog)
        {
            using (await LockManager.GetLock(LockType.OfferCreated).Lock())
            {
                string offerID = HexHelper.ByteArrayToString(eventLog.Event.offerId);

                if (OTContract_Holding_OfferCreated.Exists(connection, offerID, blockchainID))
                    return;

                var block = await BlockHelper.GetBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber, cl, blockchainID);

                var receipt = cl.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

                var transaction = cl.Eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);

                await transaction;
                await receipt;


                var row = new OTContract_Holding_OfferCreated
                {
                    Timestamp = block.Timestamp,
                    LitigationIntervalInMinutes = (UInt64)eventLog.Event.litigationIntervalInMinutes,
                    DCNodeId = HexHelper.ByteArrayToString(eventLog.Event.dcNodeId, false),
                    DataSetId = HexHelper.ByteArrayToString(eventLog.Event.dataSetId),
                    HoldingTimeInMinutes = (UInt64)eventLog.Event.holdingTimeInMinutes,
                    TokenAmountPerHolder = Web3.Convert.FromWei(eventLog.Event.tokenAmountPerHolder),
                    TransactionIndex = (UInt64)eventLog.Log.TransactionIndex.Value,
                    TransactionHash = eventLog.Log.TransactionHash,
                    BlockNumber = (UInt64)eventLog.Log.BlockNumber.Value,
                    OfferID = offerID,
                    DataSetSizeInBytes = (UInt64)eventLog.Event.dataSetSizeInBytes,
                    ContractAddress = contractAddress,
                    GasUsed = (UInt64)receipt.Result.GasUsed.Value,
                    GasPrice = (UInt64)transaction.Result.GasPrice.Value,
                    Data = eventLog.Log.Data,
                    BlockchainID = blockchainID
                };

                if (row.DCNodeId.Length > 40)
                {
                    row.DCNodeId = row.DCNodeId.Substring(row.DCNodeId.Length - 40);
                }

                OTContract_Holding_OfferCreated.InsertIfNotExist(connection, row);
            }
        }

        public static async Task ProcessOfferFinalised(MySqlConnection connection, int blockchainID, IWeb3 cl,
            string contractAddress, EventLog<List<ParameterOutput>> eventLog)
        {
            using (await LockManager.GetLock(LockType.OfferFinalised).Lock())
            {
                var offerId =
                    HexHelper.ByteArrayToString((byte[]) eventLog.Event
                        .First(e => e.Parameter.Name == "offerId").Result);

                if (!OTContract_Holding_OfferCreated.Exists(connection, offerId, blockchainID))
                {
                    //Lets get this via syncing later on as we've missed the creation of the job
                    return;
                }

                if (OTContract_Holding_OfferFinalized.Exists(connection, offerId, blockchainID))
                    return;

                var block = await BlockHelper.GetBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber,
                    cl, blockchainID);


                var holder1 = (string) eventLog.Event.FirstOrDefault(e => e.Parameter.Name == "holder1")
                    .Result;
                var holder2 = (string) eventLog.Event.FirstOrDefault(e => e.Parameter.Name == "holder2")
                    .Result;
                var holder3 = (string) eventLog.Event.FirstOrDefault(e => e.Parameter.Name == "holder3")
                    .Result;

                var receipt = cl.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

                var transaction =
                    cl.Eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);

                await transaction;
                await receipt;

                var row = new OTContract_Holding_OfferFinalized
                {
                    TransactionHash = eventLog.Log.TransactionHash,
                    BlockNumber = (UInt64) eventLog.Log.BlockNumber.Value,
                    Timestamp = block.Timestamp,
                    OfferID = offerId,
                    Holder1 = holder1,
                    Holder2 = holder2,
                    Holder3 = holder3,
                    ContractAddress = contractAddress,
                    GasUsed = (UInt64) receipt.Result.GasUsed.Value,
                    Data = eventLog.Log.Data,
                    GasPrice = (UInt64) transaction.Result.GasPrice.Value,
                    BlockchainID = blockchainID
                };

                OTContract_Holding_OfferFinalized.InsertIfNotExist(connection, row);
            }
        }

        public static async Task ProcessPayout(MySqlConnection connection, int blockchainID, IWeb3 cl, string contractAddress, EventLog<List<ParameterOutput>> eventLog)
        {
            using (await LockManager.GetLock(LockType.PayoutInsert).Lock())
            {
                var offerId =
                    HexHelper.ByteArrayToString((byte[]) eventLog.Event
                        .First(e => e.Parameter.Name == "offerId").Result);

                var amount = Web3.Convert.FromWei((BigInteger) eventLog.Event
                    .FirstOrDefault(e => e.Parameter.Name == "amount").Result);

                var holder = (string) eventLog.Event.FirstOrDefault(e => e.Parameter.Name == "holder")
                    .Result;

                if (OTContract_Holding_Paidout.Exists(connection, offerId, holder, amount, eventLog.Log.TransactionHash,
                    blockchainID))
                    return;

                var block = await BlockHelper.GetBlock(connection, eventLog.Log.BlockHash,
                    eventLog.Log.BlockNumber,
                    cl, blockchainID);


                var receipt = cl.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

                var transaction =
                    cl.Eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);

                await transaction;
                await receipt;

                OTContract_Holding_Paidout.InsertIfNotExist(connection, new OTContract_Holding_Paidout
                {
                    OfferID = offerId,
                    Holder = holder,
                    Amount = amount,
                    Timestamp = block.Timestamp,
                    TransactionHash = eventLog.Log.TransactionHash,
                    ContractAddress = contractAddress,
                    BlockNumber = (UInt64) eventLog.Log.BlockNumber.Value,
                    GasUsed = (UInt64) receipt.Result.GasUsed.Value,
                    Data = eventLog.Log.Data,
                    GasPrice = (UInt64) transaction.Result.GasPrice.Value,
                    BlockchainID = blockchainID
                });
            }
        }

        public SyncHoldingContractTask() : base(TaskNames.HoldingContractSync)
        {
        }
    }
}