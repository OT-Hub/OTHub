using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using OTHelperNetStandard.Models.Database;
using OTHub.Settings;

namespace OTHelperNetStandard.Tasks
{
    public class SyncProfileContractTask : TaskRun
    {
        public override async Task Execute(Source source)
        {
            ClientBase.ConnectionTimeout = new TimeSpan(0, 0, 5, 0);

            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                foreach (var contract in OTContract.GetByType(connection, (int)ContractType.Profile))
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

                    string abi = Constants.GetContractAbi(ContractType.Profile);


                    var profileContract = new Contract(eth, abi, contract.Address);

                    var profileCreatedEvent = profileContract.GetEvent("ProfileCreated");
                    var identityCreatedEvent = profileContract.GetEvent("IdentityCreated");
                    var identityTransferredEvent = profileContract.GetEvent("IdentityTransferred");
                    var tokensDepositedEvent = profileContract.GetEvent("TokensDeposited");
                    var tokensReleasedEvent = profileContract.GetEvent("TokensReleased");
                    var tokensWithdrawnEvent = profileContract.GetEvent("TokensWithdrawn");
                    var tokensTransferredEvent = profileContract.GetEvent("TokensTransferred");
                    var tokensReservedEvent = profileContract.GetEvent("TokensReserved");
                    Function createProfileFunction = profileContract.GetFunction("createProfile");
                    Function transferProfileFunction = profileContract.GetFunction("transferProfile");

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
                            try
                            {
                                await Sync(connection, profileCreatedEvent, identityCreatedEvent,
                                    identityTransferredEvent,
                                    tokensDepositedEvent,
                                    tokensReleasedEvent, tokensWithdrawnEvent, tokensTransferredEvent,
                                    tokensReservedEvent,
                                    contract, source, createProfileFunction, transferProfileFunction, currentStart,
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
                        await Sync(connection, profileCreatedEvent, identityCreatedEvent, identityTransferredEvent,
                            tokensDepositedEvent,
                            tokensReleasedEvent, tokensWithdrawnEvent, tokensTransferredEvent, tokensReservedEvent,
                            contract, source, createProfileFunction, transferProfileFunction, contract.SyncBlockNumber, (ulong)LatestBlockNumber.Value);
                    }
                }
            }
        }

        private async Task Sync(MySqlConnection connection, Event profileCreatedEvent, Event identityCreatedEvent,
            Event identityTransferredEvent, Event tokensDepositedEvent, Event tokensReleasedEvent,
            Event tokensWithdrawnEvent, Event tokensTransferredEvent, Event tokensReservedEvent, OTContract contract,
            Source source, Function createProfileFunction, Function transferProfileFunction, ulong start, ulong end)
        {
            Logger.WriteLine(source, "Syncing profile " + start + " to " + end);

            var toBlock = new BlockParameter(end);

            var identityCreatedEvents = await identityCreatedEvent.GetAllChangesDefault(
                identityCreatedEvent.CreateFilterInput(new BlockParameter(start),
                    toBlock));

            var identityTransferredEvents = await identityTransferredEvent.GetAllChangesDefault(
                identityTransferredEvent.CreateFilterInput(new BlockParameter(start),
                    toBlock));

            var profileCreatedEvents = await profileCreatedEvent.GetAllChangesDefault(
                profileCreatedEvent.CreateFilterInput(new BlockParameter(start),
                    toBlock));

            var tokensDepositedEvents = await tokensDepositedEvent.GetAllChangesDefault(
                tokensDepositedEvent.CreateFilterInput(new BlockParameter(start),
                    toBlock));

            var tokensReleasedEvents = await tokensReleasedEvent.GetAllChangesDefault(
                tokensReleasedEvent.CreateFilterInput(new BlockParameter(start),
                    toBlock));



            var tokensWithdrawnEvents = await tokensWithdrawnEvent.GetAllChangesDefault(
                tokensWithdrawnEvent.CreateFilterInput(new BlockParameter(start),
                    toBlock));

            var tokensTransferredEvents = await tokensTransferredEvent.GetAllChangesDefault(
                tokensTransferredEvent.CreateFilterInput(new BlockParameter(start),
                    toBlock));

            var tokensReservedEvents = await tokensReservedEvent.GetAllChangesDefault(
                tokensReservedEvent.CreateFilterInput(new BlockParameter(start),
                    toBlock));

            if (identityCreatedEvents.Any())
            {
                Logger.WriteLine(source, "Found " + identityCreatedEvents.Count + " identity created events");
            }

            if (identityTransferredEvents.Any())
            {
                Logger.WriteLine(source, "Found " + identityTransferredEvents.Count + " identity transferred events");
            }

            if (profileCreatedEvents.Any())
            {
                Logger.WriteLine(source, "Found " + profileCreatedEvents.Count + " profile created events");
            }

            if (tokensDepositedEvents.Any())
            {
                Logger.WriteLine(source, "Found " + tokensDepositedEvents.Count + " tokens deposited events");
            }

            if (tokensReleasedEvents.Any())
            {
                Logger.WriteLine(source, "Found " + tokensReleasedEvents.Count + " tokens released events");
            }

            if (tokensWithdrawnEvents.Any())
            {
                Logger.WriteLine(source, "Found " + tokensWithdrawnEvents.Count + " tokens withdrawn events");
            }

            if (tokensTransferredEvents.Any())
            {
                Logger.WriteLine(source, "Found " + tokensTransferredEvents.Count + " tokens transferred events");
            }

            if (tokensReservedEvents.Any())
            {
                Logger.WriteLine(source, "Found " + tokensReservedEvents.Count + " tokens reserved events");
            }



            foreach (EventLog<List<ParameterOutput>> eventLog in identityCreatedEvents)
            {
                var block = await Program.GetEthBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber,
                    cl);

                var profile = (string)eventLog.Event
                    .FirstOrDefault(p => p.Parameter.Name == "profile").Result;

                var newIdentity = (string)eventLog.Event
                    .FirstOrDefault(p => p.Parameter.Name == "newIdentity").Result;

                var transaction = eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);
                var receipt = eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

                await transaction;
                await receipt;

                var row = new OTContract_Profile_IdentityCreated
                {
                    TransactionHash = eventLog.Log.TransactionHash,
                    ContractAddress = contract.Address,
                    Profile = profile,
                    NewIdentity = newIdentity,
                    BlockNumber = (UInt64)eventLog.Log.BlockNumber.Value,
                    GasUsed = (UInt64)receipt.Result.GasUsed.Value,
                    GasPrice = (UInt64)transaction.Result.GasPrice.Value,
                };

                OTContract_Profile_IdentityCreated.InsertOrUpdate(connection, row);
            }

            foreach (EventLog<List<ParameterOutput>> eventLog in profileCreatedEvents)
            {
                var block = await Program.GetEthBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber,
                    cl);

                var profile = (string)eventLog.Event.FirstOrDefault(p => p.Parameter.Name == "profile")
                    .Result;
                var initialBalance = Web3.Convert.FromWei((BigInteger)eventLog.Event
                    .FirstOrDefault(p => p.Parameter.Name == "initialBalance").Result);

                var transaction = eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);
                var receipt = eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

                await transaction;
                await receipt;

                var row = new OTContract_Profile_ProfileCreated
                {
                    BlockNumber = (UInt64)eventLog.Log.BlockNumber.Value,
                    TransactionHash = eventLog.Log.TransactionHash,
                    ContractAddress = contract.Address,
                    Profile = profile,
                    InitialBalance = initialBalance,
                    GasUsed = (UInt64)receipt.Result.GasUsed.Value,
                    GasPrice = (UInt64)transaction.Result.GasPrice.Value,
                };

                var createProfileInputData = createProfileFunction.DecodeInput(transaction.Result.Input);

                if (createProfileInputData != null)
                {
                    row.ManagementWallet = (string)createProfileInputData.FirstOrDefault(p => p.Parameter.Name == "managementWallet")?.Result;

                    row.NodeId = HexHelper.ByteArrayToString((byte[])createProfileInputData
                        .FirstOrDefault(p => p.Parameter.Name == "profileNodeId").Result, false).Substring(0, 40);
                }

                OTContract_Profile_ProfileCreated.InsertIfNotExist(connection, row);
            }


            foreach (EventLog<List<ParameterOutput>> eventLog in identityTransferredEvents)
            {
                var block = await Program.GetEthBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber,
                    cl);

                var oldIdentity = (string)eventLog.Event
                    .FirstOrDefault(p => p.Parameter.Name == "oldIdentity").Result;

                var newIdentity = (string)eventLog.Event
                    .FirstOrDefault(p => p.Parameter.Name == "newIdentity").Result;

                var nodeId = HexHelper.ByteArrayToString((byte[])eventLog.Event
                    .FirstOrDefault(p => p.Parameter.Name == "nodeId").Result, false);

                var transaction = eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);
                var receipt = eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

                await transaction;
                await receipt;

                var row = new OTContract_Profile_IdentityTransferred
                {
                    TransactionHash = eventLog.Log.TransactionHash,
                    ContractAddress = contract.Address,
                    NewIdentity = newIdentity,
                    BlockNumber = (UInt64)eventLog.Log.BlockNumber.Value,
                    OldIdentity = oldIdentity,
                    NodeId = nodeId,
                    GasUsed = (UInt64)receipt.Result.GasUsed.Value,
                    GasPrice = (UInt64)transaction.Result.GasPrice.Value,
                };

                var transferProfileFunctionData = transferProfileFunction.DecodeInput(transaction.Result.Input);

                if (transferProfileFunctionData != null)
                {
                    row.ManagementWallet = (string)transferProfileFunctionData.FirstOrDefault(p => p.Parameter.Name == "managementWallet")?.Result;
                }


                OTContract_Profile_IdentityTransferred.InsertIfNotExist(connection, row);
            }


            foreach (var group in tokensDepositedEvents.GroupBy(t => t.Log.TransactionHash))
            {
                if (OTContract_Profile_TokensDeposited.TransactionExists(connection, group.Key))
                {
                    continue;
                }

                foreach (var eventLog in group)
                {
                    var block = await Program.GetEthBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber, cl);

                    var profile = (string)eventLog.Event.FirstOrDefault(p => p.Parameter.Name == "profile")
                        .Result;
                    var amountDeposited = Web3.Convert.FromWei((BigInteger)eventLog.Event
                        .FirstOrDefault(p => p.Parameter.Name == "amountDeposited").Result);
                    var newBalance = Web3.Convert.FromWei((BigInteger)eventLog.Event
                        .FirstOrDefault(p => p.Parameter.Name == "newBalance").Result);


                    var transaction = eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);
                    var receipt = eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

                    await transaction;
                    await receipt;

                    OTContract_Profile_TokensDeposited.Insert(connection, new OTContract_Profile_TokensDeposited
                    {
                        BlockNumber = block.BlockNumber,
                        TransactionHash = eventLog.Log.TransactionHash,
                        ContractAddress = contract.Address,
                        Profile = profile,
                        AmountDeposited = amountDeposited,
                        NewBalance = newBalance,
                        GasUsed = (UInt64)receipt.Result.GasUsed.Value,
                        GasPrice = (UInt64)transaction.Result.GasPrice.Value,
                    }, block.Timestamp);
                }
            }

            foreach (var group in tokensReleasedEvents.GroupBy(t => t.Log.TransactionHash))
            {
                if (OTContract_Profile_TokensReleased.TransactionExists(connection, group.Key))
                {
                    continue;
                }

                foreach (var eventLog in group)
                {
                    var block = await Program.GetEthBlock(connection, eventLog.Log.BlockHash,
                        eventLog.Log.BlockNumber,
                        cl);

                    var profile = (string)eventLog.Event.FirstOrDefault(p => p.Parameter.Name == "profile")
                        .Result;
                    var amount = Web3.Convert.FromWei((BigInteger)eventLog.Event
                        .FirstOrDefault(p => p.Parameter.Name == "amount").Result);


                    var transaction = eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);
                    var receipt = eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

                    await transaction;
                    await receipt;

                    OTContract_Profile_TokensReleased.Insert(connection,
                        new OTContract_Profile_TokensReleased
                        {
                            BlockNumber = block.BlockNumber,
                            TransactionHash = eventLog.Log.TransactionHash,
                            ContractAddress = contract.Address,
                            Amount = amount,
                            Profile = profile,
                            GasUsed = (UInt64)receipt.Result.GasUsed.Value,
                            GasPrice = (UInt64)transaction.Result.GasPrice.Value,
                        });
                }
            }

            foreach (var group in tokensWithdrawnEvents.GroupBy(t => t.Log.TransactionHash))
            {
                if (OTContract_Profile_TokensWithdrawn.TransactionExists(connection, group.Key))
                {
                    continue;
                }

                foreach (var eventLog in group)
                {
                    var block = await Program.GetEthBlock(connection, eventLog.Log.BlockHash,
                        eventLog.Log.BlockNumber,
                        cl);

                    var profile = (string)eventLog.Event.FirstOrDefault(p => p.Parameter.Name == "profile")
                        .Result;
                    var amountWithdrawn = Web3.Convert.FromWei((BigInteger)eventLog.Event
                        .FirstOrDefault(p => p.Parameter.Name == "amountWithdrawn").Result);
                    var newBalance = Web3.Convert.FromWei((BigInteger)eventLog.Event
                        .FirstOrDefault(p => p.Parameter.Name == "newBalance").Result);

                    var transaction = eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);
                    var receipt = eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

                    await transaction;
                    await receipt;

                    OTContract_Profile_TokensWithdrawn.Insert(connection,
                        new OTContract_Profile_TokensWithdrawn
                        {
                            BlockNumber = block.BlockNumber,
                            TransactionHash = eventLog.Log.TransactionHash,
                            ContractAddress = contract.Address,
                            Profile = profile,
                            NewBalance = newBalance,
                            AmountWithdrawn = amountWithdrawn,
                            GasUsed = (UInt64)receipt.Result.GasUsed.Value,
                            GasPrice = (UInt64)transaction.Result.GasPrice.Value,
                        }, block.Timestamp);
                }
            }

            foreach (var group in tokensTransferredEvents.GroupBy(t => t.Log.TransactionHash))
            {
                if (OTContract_Profile_TokensTransferred.TransactionExists(connection, group.Key))
                {
                    continue;
                }

                foreach (var eventLog in group)
                {
                    var block = await Program.GetEthBlock(connection, eventLog.Log.BlockHash,
                        eventLog.Log.BlockNumber,
                        cl);

                    var sender = (string)eventLog.Event.FirstOrDefault(p => p.Parameter.Name == "sender")
                        .Result;

                    var receiver = (string)eventLog.Event
                        .FirstOrDefault(p => p.Parameter.Name == "receiver")
                        .Result;
                    var amount = Web3.Convert.FromWei((BigInteger)eventLog.Event
                        .FirstOrDefault(p => p.Parameter.Name == "amount").Result);


                    var transaction = eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);
                    var receipt = eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

                    await transaction;
                    await receipt;

                    OTContract_Profile_TokensTransferred.Insert(connection,
                        new OTContract_Profile_TokensTransferred
                        {
                            BlockNumber = block.BlockNumber,
                            TransactionHash = eventLog.Log.TransactionHash,
                            ContractAddress = contract.Address,
                            Amount = amount,
                            Receiver = receiver,
                            Sender = sender,
                            GasUsed = (UInt64)receipt.Result.GasUsed.Value,
                            GasPrice = (UInt64)transaction.Result.GasPrice.Value,
                        });
                }
            }

            foreach (var group in tokensReservedEvents.GroupBy(t => t.Log.TransactionHash))
            {
                if (OTContract_Profile_TokensReserved.TransactionExists(connection, group.Key))
                {
                    continue;
                }

                foreach (var eventLog in group)
                {
                    var block = await Program.GetEthBlock(connection, eventLog.Log.BlockHash,
                        eventLog.Log.BlockNumber,
                        cl);

                    var profile = (string)eventLog.Event.FirstOrDefault(p => p.Parameter.Name == "profile")
                        .Result;
                    var amountReserved = Web3.Convert.FromWei((BigInteger)eventLog.Event
                        .FirstOrDefault(p => p.Parameter.Name == "amountReserved").Result);


                    var transaction = eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);
                    var receipt = eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

                    await transaction;
                    await receipt;

                    OTContract_Profile_TokensReserved.Insert(connection,
                        new OTContract_Profile_TokensReserved
                        {
                            TransactionHash = eventLog.Log.TransactionHash,
                            BlockNumber = block.BlockNumber,
                            ContractAddress = contract.Address,
                            Profile = profile,
                            AmountReserved = amountReserved,
                            GasUsed = (UInt64)receipt.Result.GasUsed.Value,
                            GasPrice = (UInt64)transaction.Result.GasPrice.Value,
                        });
                }
            }

            contract.LastSyncedTimestamp = DateTime.Now;
            contract.SyncBlockNumber = end;

            OTContract.Update(connection, contract, false, false);
        }

        public SyncProfileContractTask() : base("Sync Profile Contract")
        {
        }
    }
}