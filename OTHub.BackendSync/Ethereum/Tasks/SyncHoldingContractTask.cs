using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.Contracts;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using OTHub.BackendSync.Database.Models;
using OTHub.BackendSync.Logging;
using OTHub.Settings;
using OTHub.Settings.Abis;

namespace OTHub.BackendSync.Ethereum.Tasks
{
    public class SyncHoldingContractTask : TaskRun
    {
        public override async Task Execute(Source source)
        {
            ClientBase.ConnectionTimeout = new TimeSpan(0, 0, 5, 0);

            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                foreach (var contract in OTContract.GetByType(connection, (int)ContractTypeEnum.Holding))
                {
                    if (contract.IsArchived && contract.LastSyncedTimestamp.HasValue && (DateTime.Now - contract.LastSyncedTimestamp.Value).TotalDays <= 5)
                    {
#if  DEBUG

                        Logger.WriteLine(source, "     Skipping contract: " + contract.Address);
#endif
                        continue;
                    }

                    Logger.WriteLine(source, "     Using contract: " + contract.Address);

                    var holdingContract = new Contract(eth, AbiHelper.GetContractAbi(ContractTypeEnum.Holding), contract.Address);

                    var offerCreatedEvent = holdingContract.GetEvent("OfferCreated");
                    var offerFinalizedEvent = holdingContract.GetEvent("OfferFinalized");
                    var paidOutEvent = holdingContract.GetEvent("PaidOut");
                    var ownershipTransferredEvent = holdingContract.GetEvent("OwnershipTransferred");
                    var offerTaskEvent = holdingContract.GetEvent("OfferTask");

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
                                await Sync(connection, contract, offerCreatedEvent, offerFinalizedEvent, paidOutEvent,
                                    ownershipTransferredEvent, offerTaskEvent, source, currentStart, currentEnd);
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
                        await Sync(connection, contract, offerCreatedEvent, offerFinalizedEvent, paidOutEvent, ownershipTransferredEvent, offerTaskEvent, source, contract.SyncBlockNumber, (ulong)LatestBlockNumber.Value);
                    }
                }
            }
        }

        private async Task Sync(MySqlConnection connection, OTContract contract, Event offerCreatedEvent,
            Event offerFinalizedEvent, Event paidOutEvent, Event ownershipTransferredEvent, Event offerTaskEvent,
            Source source, ulong start, ulong end)
        {
            Logger.WriteLine(source, "Syncing holding " + start + " to " + end);

            var toBlock = new BlockParameter(end);

            await Task.Delay(250);

            var createEvents = await offerCreatedEvent.GetAllChanges<Models.Program.OfferCreated>(
                offerCreatedEvent.CreateFilterInput(new BlockParameter(start),
                    toBlock));

            await Task.Delay(250);

            var finalizedEvents = await offerFinalizedEvent.GetAllChangesDefault(
                offerFinalizedEvent.CreateFilterInput(new BlockParameter(start),
                    toBlock));

            await Task.Delay(250);

            var payoutEvents = await paidOutEvent.GetAllChangesDefault(
                paidOutEvent.CreateFilterInput(new BlockParameter(start), toBlock));

            await Task.Delay(250);

            var ownershipTransferredEvents = await ownershipTransferredEvent.GetAllChangesDefault(
                ownershipTransferredEvent.CreateFilterInput(new BlockParameter(start),
                    toBlock));

            await Task.Delay(250);

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
                string offerID = HexHelper.ByteArrayToString(eventLog.Event.offerId);

                if (OTContract_Holding_OfferCreated.Exists(connection, offerID))
                    continue;

                var block = await BlockHelper.GetEthBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber, cl);

                var receipt = cl.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);
                await Task.Delay(100);
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
                    ContractAddress = contract.Address,
                    GasUsed = (UInt64)receipt.Result.GasUsed.Value,
                    GasPrice = (UInt64)transaction.Result.GasPrice.Value,
                    Data = eventLog.Log.Data
                };

                if (row.DCNodeId.Length > 40)
                {
                    row.DCNodeId = row.DCNodeId.Substring(row.DCNodeId.Length - 40);
                }

                OTContract_Holding_OfferCreated.InsertIfNotExist(connection, row);
            }

            foreach (EventLog<List<ParameterOutput>> eventLog in finalizedEvents)
            {
                var offerId =
                    HexHelper.ByteArrayToString((byte[])eventLog.Event
                        .First(e => e.Parameter.Name == "offerId").Result);

                if (OTContract_Holding_OfferFinalized.Exists(connection, offerId))
                    continue;

                var block = await BlockHelper.GetEthBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber,
                    cl);


                var holder1 = (string)eventLog.Event.FirstOrDefault(e => e.Parameter.Name == "holder1")
                    .Result;
                var holder2 = (string)eventLog.Event.FirstOrDefault(e => e.Parameter.Name == "holder2")
                    .Result;
                var holder3 = (string)eventLog.Event.FirstOrDefault(e => e.Parameter.Name == "holder3")
                    .Result;

                var receipt = cl.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);
                await Task.Delay(100);
                var transaction = cl.Eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);

                await transaction;
                await receipt;

                var row = new OTContract_Holding_OfferFinalized
                {
                    TransactionHash = eventLog.Log.TransactionHash,
                    BlockNumber = (UInt64)eventLog.Log.BlockNumber.Value,
                    Timestamp = block.Timestamp,
                    OfferID = offerId,
                    Holder1 = holder1,
                    Holder2 = holder2,
                    Holder3 = holder3,
                    ContractAddress = contract.Address,
                    GasUsed = (UInt64)receipt.Result.GasUsed.Value,
                    Data = eventLog.Log.Data,
                    GasPrice = (UInt64)transaction.Result.GasPrice.Value,
                };

                OTContract_Holding_OfferFinalized.InsertIfNotExist(connection, row);
            }

            foreach (EventLog<List<ParameterOutput>> eventLog in payoutEvents)
            {
                var offerId =
                    HexHelper.ByteArrayToString((byte[])eventLog.Event
                        .First(e => e.Parameter.Name == "offerId").Result);

                var amount = Web3.Convert.FromWei((BigInteger)eventLog.Event
                    .FirstOrDefault(e => e.Parameter.Name == "amount").Result);

                var holder = (string)eventLog.Event.FirstOrDefault(e => e.Parameter.Name == "holder")
                    .Result;

                if (OTContract_Holding_Paidout.Exists(connection, offerId, holder, amount, eventLog.Log.TransactionHash))
                    continue;

                var block = await BlockHelper.GetEthBlock(connection, eventLog.Log.BlockHash,
                    eventLog.Log.BlockNumber,
                    cl);


                var receipt = cl.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);
                await Task.Delay(100);
                var transaction = cl.Eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);

                await transaction;
                await receipt;

                OTContract_Holding_Paidout.InsertIfNotExist(connection, new OTContract_Holding_Paidout
                {
                    OfferID = offerId,
                    Holder = holder,
                    Amount = amount,
                    Timestamp = block.Timestamp,
                    TransactionHash = eventLog.Log.TransactionHash,
                    ContractAddress = contract.Address,
                    BlockNumber = (UInt64)eventLog.Log.BlockNumber.Value,
                    GasUsed = (UInt64)receipt.Result.GasUsed.Value,
                    Data = eventLog.Log.Data,
                    GasPrice = (UInt64)transaction.Result.GasPrice.Value,
                });
            }

            foreach (EventLog<List<ParameterOutput>> eventLog in ownershipTransferredEvents)
            {
                var block = await BlockHelper.GetEthBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber,
                    cl);
                var previousOwner = (string)eventLog.Event
                    .FirstOrDefault(e => e.Parameter.Name == "previousOwner").Result;
                var newOwner = (string)eventLog.Event.FirstOrDefault(e => e.Parameter.Name == "newOwner")
                    .Result;

                var transaction = eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);
                await Task.Delay(100);
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
                        GasUsed = (UInt64)receipt.Result.GasUsed.Value
                    });
            }

            foreach (EventLog<List<ParameterOutput>> eventLog in offerTaskEvents)
            {
                var block = await BlockHelper.GetEthBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber,
                    cl);

                var dataSetId =
                    HexHelper.ByteArrayToString((byte[])eventLog.Event
                        .First(e => e.Parameter.Name == "dataSetId").Result);
                var dcNodeId =
                    HexHelper.ByteArrayToString((byte[])eventLog.Event
                        .First(e => e.Parameter.Name == "dcNodeId").Result);
                var offerId =
                    HexHelper.ByteArrayToString((byte[])eventLog.Event
                        .First(e => e.Parameter.Name == "offerId").Result);
                var task = HexHelper.ByteArrayToString(
                    (byte[])eventLog.Event.First(e => e.Parameter.Name == "task").Result);

                var transaction = eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);
                await Task.Delay(100);
                var receipt = eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

                await transaction;
                await receipt;

                OTContract_Holding_OfferTask.InsertIfNotExist(connection, new OTContract_Holding_OfferTask
                {
                    BlockNumber = (UInt64)eventLog.Log.BlockNumber.Value,
                    Task = task,
                    TransactionHash = eventLog.Log.TransactionHash,
                    ContractAddress = contract.Address,
                    DCNodeId = dcNodeId,
                    DataSetId = dataSetId,
                    OfferId = offerId,
                    GasPrice = (UInt64)transaction.Result.GasPrice.Value,
                    GasUsed = (UInt64)receipt.Result.GasUsed.Value
                });
            }

            contract.LastSyncedTimestamp = DateTime.Now;
            contract.SyncBlockNumber = end;

            OTContract.Update(connection, contract, false, false);
        }

        public SyncHoldingContractTask() : base("Sync Holding Contract")
        {
        }
    }
}