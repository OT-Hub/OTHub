using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
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

namespace OTHub.BackendSync.Blockchain.Tasks.Misc.Children
{
    public class GetLatestContractsTask : TaskRunBlockchain
    {
        public override async Task<bool> Execute(Source source, BlockchainType blockchain, BlockchainNetwork network)
        {
            ClientBase.ConnectionTimeout = new TimeSpan(0, 0, 5, 0);

            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                int blockchainID = await GetBlockchainID(connection, blockchain, network);

                string currentHubAddress = await connection.ExecuteScalarAsync<string>("select HubAddress from blockchains where id = @id", new
                {
                    id = blockchainID
                });

                var allHubAddresses = new List<String>();
                allHubAddresses.Add(currentHubAddress);
                var addresses = allHubAddresses.Distinct();




                foreach (var address in addresses)
                {
                    await PopulateSmartContracts(connection, address,
                        address == currentHubAddress, blockchainID, blockchain, network);
                }
            }

            await SmartContractManager.Load();

            return true;
        }

        private async Task<HubAddress> GetHubAddressModel(MySqlConnection connection, string hubAddress,
            int blockchainID, bool isLatest)
        {
            ulong fromBlockNumber = await connection.ExecuteScalarAsync<UInt64>(
                "SELECT FromBlockNumber FROM blockchains where id = @id", new
                {
                    id = blockchainID
                });

            if (!await HubAddress.Exists(connection, blockchainID, hubAddress))
            {
                await HubAddress.Insert(connection, blockchainID, hubAddress, fromBlockNumber);
            }

            HubAddress hubAddressModel = await HubAddress.GetByID(connection, blockchainID, hubAddress);

            if (!isLatest && !hubAddressModel.DateReplaced.HasValue)
            {
                await HubAddress.MarkAsReplaced(connection, blockchainID, hubAddress);
            }

            return hubAddressModel;
        }

        private async Task PopulateSmartContracts(MySqlConnection connection, string hubAddress, bool isLatest,
            int blockchainID, BlockchainType blockchain, BlockchainNetwork network)
        {
            var web3 = await GetWeb3(connection, blockchainID, blockchain);
            EthApiService eth = new EthApiService(web3.Client);

            var hubContract = new Contract(eth, AbiHelper.GetContractAbi(ContractTypeEnum.Hub, blockchain, network),
                hubAddress);

            HubAddress hubAddressModel = await GetHubAddressModel(connection, hubAddress, blockchainID, isLatest);

            if (blockchain == BlockchainType.Ethereum && network == BlockchainNetwork.Mainnet)
            {
                await PopulateOriginalETHContracts(connection, blockchainID, hubAddressModel.FromBlockNumber);
            }

            Event contractsChangedEvent = hubContract.GetEvent("ContractsChanged");

            ulong diff = (ulong) LatestBlockNumber.Value - hubAddressModel.SyncBlockNumber;

            ulong size = (ulong) 10000;

            beforeSync:
            if (diff > size)
            {
                ulong currentStart = hubAddressModel.SyncBlockNumber;
                ulong currentEnd = currentStart + size;

                if (currentEnd > LatestBlockNumber.Value)
                {
                    currentEnd = (ulong) LatestBlockNumber.Value;
                }

                bool canRetry = true;
                while (currentStart == 0 || currentStart < LatestBlockNumber.Value)
                {
                    start:
                    try
                    {
                        await SyncContractsChanged(contractsChangedEvent,
                            currentStart, currentEnd,
                            hubContract, web3, connection, blockchainID);
                    }
                    catch (RpcResponseException ex) when (ex.Message.Contains("query returned more than"))
                    {
                        size = size / 2;

                        Logger.WriteLine(Source.BlockchainSync, "Swapping to block sync size of " + size);

                        goto beforeSync;
                    }
                    catch (RpcClientUnknownException ex) when (canRetry &&
                                                               ex.GetBaseException().Message.Contains("Gateway"))
                    {
                        canRetry = false;
                        goto start;
                    }

                    currentStart = currentEnd;
                    currentEnd = currentStart + size;

                    if (currentEnd > LatestBlockNumber.Value)
                    {
                        currentEnd = (ulong) LatestBlockNumber.Value;
                    }
                }
            }
            else
            {
                await SyncContractsChanged(contractsChangedEvent,
                    hubAddressModel.SyncBlockNumber, (ulong) LatestBlockNumber.Value,
                    hubContract, web3, connection, blockchainID);
            }

            await SyncLatestContractsOnHub(hubAddressModel.FromBlockNumber, hubContract, connection, blockchainID, isLatest);
        }

        private static async Task PopulateOriginalETHContracts(MySqlConnection connection, int blockchainID,
            ulong fromBlockNumber)
        {
            await OTContract.InsertOrUpdate(connection, new OTContract(fromBlockNumber, fromBlockNumber)
            {
                Address = "0xefa914bd9ea22848df987d344eb75bc4dfd92b42",
                Type = (int) ContractTypeEnum.Profile,
                IsLatest = false,
                BlockchainID = blockchainID
            }, true);

            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = "0x407da012319e0d97c6f17ac72e8dd8a56c3e1556",
                    IsLatest = false,
                    Type = (int) ContractTypeEnum.Holding,
                    BlockchainID = blockchainID
                }, true);


            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = "0xcae2df21e532d92b05d55c9ec75d579ea24d8521",
                    IsLatest = false,
                    Type = (int) ContractTypeEnum.Profile,
                    BlockchainID = blockchainID
                }, true);
            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = "0xaa7a9ca87d3694b5755f213b5d04094b8d0f0a6f",
                    IsLatest = false,
                    Type = (int) ContractTypeEnum.Token,
                    BlockchainID = blockchainID
                }, true);
            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = "0x2be3cf5bd3609fd63b77aa40d0971c778db77c8a",
                    IsLatest = false,
                    Type = (int) ContractTypeEnum.HoldingStorage,
                    BlockchainID = blockchainID
                }, true);
            //TODO remove from database
            //OTContract.InsertOrUpdate(connection,
            //    new OTContract(fromBlockNumber, fromBlockNumber)
            //    {
            //        Address = "0xf130e4df48aeef509a3e106223febcde1f9d1a4b",
            //        IsLatest = false,
            //        Type = (int)ContractTypeEnum.Holding,
            //        BlockchainID = blockchainID
            //    }, true);
            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = "0x24d4ce2c8538290b9f283fad8ff423c601d1e114",
                    IsLatest = false,
                    Type = (int) ContractTypeEnum.Approval,
                    BlockchainID = blockchainID
                }, true);
            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = "0x306d5e8af6aeb73359dcc5e22c894e2588f76ffb",
                    IsLatest = false,
                    Type = (int) ContractTypeEnum.ProfileStorage,
                    BlockchainID = blockchainID
                }, true);
            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = "0x1ea5cc419c6167ae8712d5bb1ba67120f37cbec8",
                    IsLatest = false,
                    Type = (int) ContractTypeEnum.Profile,
                    BlockchainID = blockchainID
                }, true);
            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = "0x951a11842f8a81e8f1ab31d029e4f11cf80c697a",
                    IsLatest = false,
                    Type = (int) ContractTypeEnum.Profile,
                    BlockchainID = blockchainID
                }, true);
            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = "0xc3af0b170a02d108f55e224d6b2605fc3e93d68e",
                    IsLatest = false,
                    Type = (int) ContractTypeEnum.Profile,
                    BlockchainID = blockchainID
                }, true);
            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = "0xe7db7f713b2ea963d0dcb67514b50394f1295cc1",
                    IsLatest = false,
                    Type = (int) ContractTypeEnum.Profile,
                    BlockchainID = blockchainID
                }, true);
            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = "0x6763c4c8293796b8726d9450a988d374a8e9f994",
                    IsLatest = false,
                    Type = (int) ContractTypeEnum.Profile,
                    BlockchainID = blockchainID
                }, true);
            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = "0x2b29bcc72a7420f791722da79e255852f171b38d",
                    IsLatest = false,
                    Type = (int) ContractTypeEnum.Holding,
                    BlockchainID = blockchainID
                }, true);
            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = "0x283a70a58c65112da7ee377a21a1fd3286581ffb",
                    IsLatest = false,
                    Type = (int) ContractTypeEnum.Holding,
                    BlockchainID = blockchainID
                }, true);
            //TODO remove from database
            //OTContract.InsertOrUpdate(connection,
            //    new OTContract(fromBlockNumber, fromBlockNumber)
            //    {
            //        Address = "0x8d92ee115c126b751cfb0849efa629d2aadb8753",
            //        IsLatest = false,
            //        Type = (int)ContractTypeEnum.Holding,
            //        BlockchainID = blockchainID
            //    }, true);
            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = "0x87e04af76ecbb0114fc2d681c89a11eee457a268",
                    IsLatest = false,
                    Type = (int) ContractTypeEnum.Holding,
                    BlockchainID = blockchainID
                }, true);
        }

        private async Task SyncContractsChanged(Event contractsChangedEvent, ulong fromBlockNumber, ulong toBlockNumber,
            Contract hubContract, Web3 web3, MySqlConnection connection, int blockchainID)
        {
            var eventsOfChange = await contractsChangedEvent.GetAllChangesDefault(
                contractsChangedEvent.CreateFilterInput(new BlockParameter(fromBlockNumber),
                    new BlockParameter(toBlockNumber)));

            foreach (var eventLog in eventsOfChange)
            {
                Function setContractAddressFunction = hubContract.GetFunction("setContractAddress");

                var transaction = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);
                var data = setContractAddressFunction.DecodeInput(transaction.Input);

                if (data == null)
                    continue;

                var contractName = (string)data.First(d => d.Parameter.Name == "contractName").Result;
                var newContractAddress = (string)data.First(d => d.Parameter.Name == "newContractAddress").Result;

                if (Enum.TryParse(typeof(ContractTypeEnum), contractName, true, out var result))
                {
                    await OTContract.InsertOrUpdate(connection,
                        new OTContract(fromBlockNumber, fromBlockNumber)
                        {
                            Address = newContractAddress,
                            IsLatest = false,
                            Type = (int)result,
                            BlockchainID = blockchainID
                        }, true);
                }
            }

            await HubAddress.UpdateSyncBlockNumber(connection, blockchainID, hubContract.Address, toBlockNumber);
        }

        private async Task SyncLatestContractsOnHub(ulong fromBlockNumber, 
            Contract hubContract, MySqlConnection connection, int blockchainID, bool isLatest)
        {
            var tokenAddress = await hubContract.GetFunction("getContractAddress").CallAsync<string>(ContractTypeEnum.Token.ToString());

            var approvalAddress = await hubContract.GetFunction("getContractAddress").CallAsync<string>(ContractTypeEnum.Approval.ToString());

            var holdingStorageAddress = await hubContract.GetFunction("getContractAddress").CallAsync<string>(ContractTypeEnum.HoldingStorage.ToString());

            var holdingAddress = await hubContract.GetFunction("getContractAddress").CallAsync<string>(ContractTypeEnum.Holding.ToString());

            var profileStorageAddress = await hubContract.GetFunction("getContractAddress").CallAsync<string>(ContractTypeEnum.ProfileStorage.ToString());

            var profileAddress = await hubContract.GetFunction("getContractAddress").CallAsync<string>(ContractTypeEnum.Profile.ToString());

            var readingAddress = await hubContract.GetFunction("getContractAddress").CallAsync<string>(ContractTypeEnum.Reading.ToString());

            var readingStorageAddress = await hubContract.GetFunction("getContractAddress").CallAsync<string>(ContractTypeEnum.ReadingStorage.ToString());

            var litigationAddress = await hubContract.GetFunction("getContractAddress").CallAsync<string>(ContractTypeEnum.Litigation.ToString());

            var litigationStorageAddress = await hubContract.GetFunction("getContractAddress").CallAsync<string>(ContractTypeEnum.LitigationStorage.ToString());

            var replacementAddress = await hubContract.GetFunction("getContractAddress").CallAsync<string>(ContractTypeEnum.Replacement.ToString());



            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = approvalAddress,
                    IsLatest = isLatest,
                    Type = (int)ContractTypeEnum.Approval,
                    BlockchainID = blockchainID
                },
                true);
            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = holdingAddress,
                    IsLatest = isLatest,
                    Type = (int)ContractTypeEnum.Holding,
                    BlockchainID = blockchainID
                },
                true);
            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = holdingStorageAddress,
                    IsLatest = isLatest,
                    Type = (int)ContractTypeEnum.HoldingStorage,
                    BlockchainID = blockchainID
                }, true);
            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = profileAddress,
                    IsLatest = isLatest,
                    Type = (int)ContractTypeEnum.Profile,
                    BlockchainID = blockchainID
                },
                true);
            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = profileStorageAddress,
                    IsLatest = isLatest,
                    Type = (int)ContractTypeEnum.ProfileStorage,
                    BlockchainID = blockchainID
                }, true);
            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = tokenAddress,
                    IsLatest = isLatest,
                    Type = (int)ContractTypeEnum.Token,
                    BlockchainID = blockchainID
                },
                true);
            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = readingAddress,
                    IsLatest = isLatest,
                    Type = (int)ContractTypeEnum.Reading,
                    BlockchainID = blockchainID
                },
                true);
            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = readingStorageAddress,
                    IsLatest = isLatest,
                    Type = (int)ContractTypeEnum.ReadingStorage,
                    BlockchainID = blockchainID
                }, true);
            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = litigationAddress,
                    IsLatest = true,
                    Type = (int)ContractTypeEnum.Litigation,
                    BlockchainID = blockchainID
                }, true);
            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = litigationStorageAddress,
                    IsLatest = isLatest,
                    Type = (int)ContractTypeEnum.LitigationStorage,
                    BlockchainID = blockchainID
                }, true);
            await OTContract.InsertOrUpdate(connection,
                new OTContract(fromBlockNumber, fromBlockNumber)
                {
                    Address = replacementAddress,
                    IsLatest = isLatest,
                    Type = (int)ContractTypeEnum.Replacement,
                    BlockchainID = blockchainID
                }, true);
        }

        public GetLatestContractsTask() : base(TaskNames.GetLatestContracts)
        {
        }
    }
}