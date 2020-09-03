using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Nethereum.Contracts;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using OTHub.BackendSync.Models.Database;
using OTHub.Settings;

namespace OTHub.BackendSync.Tasks
{
    public class GetLatestContractsTask : TaskRun
    {
        public override async Task Execute(Source source)
        {
            ClientBase.ConnectionTimeout = new TimeSpan(0, 0, 5, 0);

            using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                //{
                //    OTContract.InsertOrUpdate(connection, new OTContract
                //    {
                //        Address = "0x94d3370de31a9a16eb29195f2e9e44bc83656677",
                //        Type = (int)ContractType.Holding,
                //        IsLatest = true
                //    }, true);
                //    return;
                //}

                //if (OTHubSettings.Instance.Blockchain.Network == BlockchainNetwork.Testnet)
                //{
                    var allHubAddresses = new List<String>();
                    allHubAddresses.Add(OTHubSettings.Instance.Blockchain.HubAddress);
                    var addresses = allHubAddresses.Distinct();

                    foreach (var address in addresses)
                    {
                        await NewTestnetMethod(connection, address, address == OTHubSettings.Instance.Blockchain.HubAddress);
                    }
                //}
                //else
                //{
                //    await OldLiveMethod(connection);
                //}
            }
        }

        private static async Task NewTestnetMethod(MySqlConnection connection, string hubAddress, bool isLatest)
        {
            using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("OTHub.BackendSync.Abis.hub.json"))
            using (StreamReader reader = new StreamReader(resource))
            {
                var hubContract = new Contract(TaskRun.eth, reader.ReadToEnd(), hubAddress);

                
                var tokenAddress = await hubContract.GetFunction("getContractAddress").CallAsync<string>(ContractType.Token.ToString());
                
                var approvalAddress = await hubContract.GetFunction("getContractAddress").CallAsync<string>(ContractType.Approval.ToString());
                
                var holdingStorageAddress = await hubContract.GetFunction("getContractAddress").CallAsync<string>(ContractType.HoldingStorage.ToString());
                
                var holdingAddress = await hubContract.GetFunction("getContractAddress").CallAsync<string>(ContractType.Holding.ToString());
                
                var profileStorageAddress = await hubContract.GetFunction("getContractAddress").CallAsync<string>(ContractType.ProfileStorage.ToString());
                
                var profileAddress = await hubContract.GetFunction("getContractAddress").CallAsync<string>(ContractType.Profile.ToString());
                
                var readingAddress = await hubContract.GetFunction("getContractAddress").CallAsync<string>(ContractType.Reading.ToString());
                
                var readingStorageAddress = await hubContract.GetFunction("getContractAddress").CallAsync<string>(ContractType.ReadingStorage.ToString());
                
                var litigationAddress = await hubContract.GetFunction("getContractAddress").CallAsync<string>(ContractType.Litigation.ToString());
                
                var litigationStorageAddress = await hubContract.GetFunction("getContractAddress").CallAsync<string>(ContractType.LitigationStorage.ToString());
                
                var replacementAddress = await hubContract.GetFunction("getContractAddress").CallAsync<string>(ContractType.Replacement.ToString());
                

                if (OTHubSettings.Instance.Blockchain.Network == BlockchainNetwork.Mainnet)
                {
                    OTContract.InsertOrUpdate(connection, new OTContract
                    {
                        Address = "0xefa914bd9ea22848df987d344eb75bc4dfd92b42",
                        Type = (int) ContractType.Profile,
                        IsLatest = false
                    }, true);

                    OTContract.InsertOrUpdate(connection,
                        new OTContract
                        {
                            Address = "0x407da012319e0d97c6f17ac72e8dd8a56c3e1556",
                            IsLatest = false,
                            Type = (int) ContractType.Holding
                        }, true);


                    OTContract.InsertOrUpdate(connection,
                        new OTContract
                        {
                            Address = "0xcae2df21e532d92b05d55c9ec75d579ea24d8521",
                            IsLatest = false,
                            Type = (int) ContractType.Profile
                        }, true);
                    OTContract.InsertOrUpdate(connection,
                        new OTContract
                        {
                            Address = "0xaa7a9ca87d3694b5755f213b5d04094b8d0f0a6f",
                            IsLatest = false,
                            Type = (int) ContractType.Token
                        }, true);
                    OTContract.InsertOrUpdate(connection,
                        new OTContract
                        {
                            Address = "0x2be3cf5bd3609fd63b77aa40d0971c778db77c8a",
                            IsLatest = false,
                            Type = (int) ContractType.HoldingStorage
                        }, true);
                    OTContract.InsertOrUpdate(connection,
                        new OTContract
                        {
                            Address = "0xf130e4df48aeef509a3e106223febcde1f9d1a4b",
                            IsLatest = false,
                            Type = (int) ContractType.Holding
                        }, true);
                    OTContract.InsertOrUpdate(connection,
                        new OTContract
                        {
                            Address = "0x24d4ce2c8538290b9f283fad8ff423c601d1e114",
                            IsLatest = false,
                            Type = (int) ContractType.Approval
                        }, true);
                    OTContract.InsertOrUpdate(connection,
                        new OTContract
                        {
                            Address = "0x306d5e8af6aeb73359dcc5e22c894e2588f76ffb",
                            IsLatest = false,
                            Type = (int) ContractType.ProfileStorage
                        }, true);
                    OTContract.InsertOrUpdate(connection,
                        new OTContract
                        {
                            Address = "0x1ea5cc419c6167ae8712d5bb1ba67120f37cbec8",
                            IsLatest = false,
                            Type = (int) ContractType.Profile
                        }, true);
                    OTContract.InsertOrUpdate(connection,
                        new OTContract
                        {
                            Address = "0x951a11842f8a81e8f1ab31d029e4f11cf80c697a",
                            IsLatest = false,
                            Type = (int) ContractType.Profile
                        }, true);
                    OTContract.InsertOrUpdate(connection,
                        new OTContract
                        {
                            Address = "0xc3af0b170a02d108f55e224d6b2605fc3e93d68e",
                            IsLatest = false,
                            Type = (int) ContractType.Profile
                        }, true);
                    OTContract.InsertOrUpdate(connection,
                        new OTContract
                        {
                            Address = "0xe7db7f713b2ea963d0dcb67514b50394f1295cc1",
                            IsLatest = false,
                            Type = (int) ContractType.Profile
                        }, true);
                    OTContract.InsertOrUpdate(connection,
                        new OTContract
                        {
                            Address = "0x6763c4c8293796b8726d9450a988d374a8e9f994",
                            IsLatest = false,
                            Type = (int) ContractType.Profile
                        }, true);
                    OTContract.InsertOrUpdate(connection,
                        new OTContract
                        {
                            Address = "0x2b29bcc72a7420f791722da79e255852f171b38d",
                            IsLatest = false,
                            Type = (int) ContractType.Holding
                        }, true);
                    OTContract.InsertOrUpdate(connection,
                        new OTContract
                        {
                            Address = "0x283a70a58c65112da7ee377a21a1fd3286581ffb",
                            IsLatest = false,
                            Type = (int) ContractType.Holding
                        }, true);
                    OTContract.InsertOrUpdate(connection,
                        new OTContract
                        {
                            Address = "0x8d92ee115c126b751cfb0849efa629d2aadb8753",
                            IsLatest = false,
                            Type = (int) ContractType.Holding
                        }, true);
                    OTContract.InsertOrUpdate(connection,
                        new OTContract
                        {
                            Address = "0x87e04af76ecbb0114fc2d681c89a11eee457a268",
                            IsLatest = false,
                            Type = (int) ContractType.Holding
                        }, true);
                }

                Event contractsChangedEvent = hubContract.GetEvent("ContractsChanged");

                

                var eventsOfChange = await contractsChangedEvent.GetAllChangesDefault(
                    contractsChangedEvent.CreateFilterInput(new BlockParameter(TaskRun.FromBlockNumber),
                        BlockParameter.CreateLatest()));

                foreach (var eventLog in eventsOfChange)
                {
                    

                    Function setContractAddressFunction = hubContract.GetFunction("setContractAddress");

                    var transaction = await cl.Eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);
                    var data = setContractAddressFunction.DecodeInput(transaction.Input);

                    var contractName = (string)data.First(d => d.Parameter.Name == "contractName").Result;
                    var newContractAddress = (string)data.First(d => d.Parameter.Name == "newContractAddress").Result;

                    if (Enum.TryParse(typeof(ContractType), contractName, true, out var result))
                    {
                        OTContract.InsertOrUpdate(connection,
                            new OTContract
                            {
                                Address = newContractAddress,
                                IsLatest = false,
                                Type = (int)result
                            }, true);
                    }
                }


                OTContract.InsertOrUpdate(connection,
                    new OTContract
                    {
                        Address = approvalAddress,
                        IsLatest = isLatest,
                        Type = (int)ContractType.Approval
                    },
                    true);
                OTContract.InsertOrUpdate(connection,
                    new OTContract
                    {
                        Address = holdingAddress,
                        IsLatest = isLatest,
                        Type = (int)ContractType.Holding
                    },
                    true);
                OTContract.InsertOrUpdate(connection,
                    new OTContract
                    {
                        Address = holdingStorageAddress,
                        IsLatest = isLatest,
                        Type = (int)ContractType.HoldingStorage
                    }, true);
                OTContract.InsertOrUpdate(connection,
                    new OTContract
                    {
                        Address = profileAddress,
                        IsLatest = isLatest,
                        Type = (int)ContractType.Profile
                    },
                    true);
                OTContract.InsertOrUpdate(connection,
                    new OTContract
                    {
                        Address = profileStorageAddress,
                        IsLatest = isLatest,
                        Type = (int)ContractType.ProfileStorage
                    }, true);
                OTContract.InsertOrUpdate(connection,
                    new OTContract { Address = tokenAddress, IsLatest = isLatest, Type = (int)ContractType.Token },
                    true);
                OTContract.InsertOrUpdate(connection,
                    new OTContract
                    {
                        Address = readingAddress,
                        IsLatest = isLatest,
                        Type = (int)ContractType.Reading
                    },
                    true);
                OTContract.InsertOrUpdate(connection,
                    new OTContract
                    {
                        Address = readingStorageAddress,
                        IsLatest = isLatest,
                        Type = (int)ContractType.ReadingStorage
                    }, true);
                OTContract.InsertOrUpdate(connection,
                    new OTContract
                    {
                        Address = litigationAddress,
                        IsLatest = true,
                        Type = (int)ContractType.Litigation
                    }, true);
                OTContract.InsertOrUpdate(connection,
                    new OTContract
                    {
                        Address = litigationStorageAddress,
                        IsLatest = isLatest,
                        Type = (int)ContractType.LitigationStorage
                    }, true);
                OTContract.InsertOrUpdate(connection,
                    new OTContract
                    {
                        Address = replacementAddress,
                        IsLatest = isLatest,
                        Type = (int)ContractType.Replacement
                    }, true);



            }
        }

        private static async Task OldLiveMethod(MySqlConnection connection)
        {
            var hubContract = new Contract(eth, Constants.OldHubAbi, OTHubSettings.Instance.Blockchain.HubAddress);
            var approvalAddress = await hubContract.GetFunction("approvalAddress").CallAsync<string>();
            var tokenAddress = await hubContract.GetFunction("tokenAddress").CallAsync<string>();
            var holdingStorageAddress = await hubContract.GetFunction("holdingStorageAddress").CallAsync<string>();
            var holdingAddress = await hubContract.GetFunction("holdingAddress").CallAsync<string>();
            var profileStorageAddress = await hubContract.GetFunction("profileStorageAddress").CallAsync<string>();
            var profileAddress = await hubContract.GetFunction("profileAddress").CallAsync<string>();
            var readingAddress = await hubContract.GetFunction("readingAddress").CallAsync<string>();
            var readingStorageAddress = await hubContract.GetFunction("readingStorageAddress").CallAsync<string>();

            //temp... added a load of ERC 0.0 profiles
            OTContract.InsertOrUpdate(connection, new OTContract
            {
                Address = "0xefa914bd9ea22848df987d344eb75bc4dfd92b42",
                Type = (int) ContractType.Profile,
                IsLatest = false
            }, true);

            OTContract.InsertOrUpdate(connection,
                new OTContract
                {
                    Address = "0x407da012319e0d97c6f17ac72e8dd8a56c3e1556",
                    IsLatest = false,
                    Type = (int) ContractType.Holding
                }, true);


            OTContract.InsertOrUpdate(connection,
                new OTContract
                {
                    Address = "0xcae2df21e532d92b05d55c9ec75d579ea24d8521",
                    IsLatest = false,
                    Type = (int) ContractType.Profile
                }, true);
            OTContract.InsertOrUpdate(connection,
                new OTContract
                {
                    Address = "0xaa7a9ca87d3694b5755f213b5d04094b8d0f0a6f",
                    IsLatest = false,
                    Type = (int) ContractType.Token
                }, true);
            OTContract.InsertOrUpdate(connection,
                new OTContract
                {
                    Address = "0x2be3cf5bd3609fd63b77aa40d0971c778db77c8a",
                    IsLatest = false,
                    Type = (int) ContractType.HoldingStorage
                }, true);
            OTContract.InsertOrUpdate(connection,
                new OTContract
                {
                    Address = "0xf130e4df48aeef509a3e106223febcde1f9d1a4b",
                    IsLatest = false,
                    Type = (int) ContractType.Holding
                }, true);
            OTContract.InsertOrUpdate(connection,
                new OTContract
                {
                    Address = "0x24d4ce2c8538290b9f283fad8ff423c601d1e114",
                    IsLatest = false,
                    Type = (int) ContractType.Approval
                }, true);
            OTContract.InsertOrUpdate(connection,
                new OTContract
                {
                    Address = "0x306d5e8af6aeb73359dcc5e22c894e2588f76ffb",
                    IsLatest = false,
                    Type = (int) ContractType.ProfileStorage
                }, true);
            OTContract.InsertOrUpdate(connection,
                new OTContract
                {
                    Address = "0x1ea5cc419c6167ae8712d5bb1ba67120f37cbec8",
                    IsLatest = false,
                    Type = (int) ContractType.Profile
                }, true);
            OTContract.InsertOrUpdate(connection,
                new OTContract
                {
                    Address = "0x951a11842f8a81e8f1ab31d029e4f11cf80c697a",
                    IsLatest = false,
                    Type = (int) ContractType.Profile
                }, true);
            OTContract.InsertOrUpdate(connection,
                new OTContract
                {
                    Address = "0xc3af0b170a02d108f55e224d6b2605fc3e93d68e",
                    IsLatest = false,
                    Type = (int) ContractType.Profile
                }, true);
            OTContract.InsertOrUpdate(connection,
                new OTContract
                {
                    Address = "0xe7db7f713b2ea963d0dcb67514b50394f1295cc1",
                    IsLatest = false,
                    Type = (int) ContractType.Profile
                }, true);
            OTContract.InsertOrUpdate(connection,
                new OTContract
                {
                    Address = "0x6763c4c8293796b8726d9450a988d374a8e9f994",
                    IsLatest = false,
                    Type = (int) ContractType.Profile
                }, true);
            OTContract.InsertOrUpdate(connection,
                new OTContract
                {
                    Address = "0x2b29bcc72a7420f791722da79e255852f171b38d",
                    IsLatest = false,
                    Type = (int) ContractType.Holding
                }, true);
            OTContract.InsertOrUpdate(connection,
                new OTContract
                {
                    Address = "0x283a70a58c65112da7ee377a21a1fd3286581ffb",
                    IsLatest = false,
                    Type = (int) ContractType.Holding
                }, true);
            OTContract.InsertOrUpdate(connection,
                new OTContract
                {
                    Address = "0x8d92ee115c126b751cfb0849efa629d2aadb8753",
                    IsLatest = false,
                    Type = (int) ContractType.Holding
                }, true);
            OTContract.InsertOrUpdate(connection,
                new OTContract
                {
                    Address = "0x87e04af76ecbb0114fc2d681c89a11eee457a268",
                    IsLatest = false,
                    Type = (int) ContractType.Holding
                }, true);

            OTContract.InsertOrUpdate(connection,
                new OTContract
                {
                    Address = approvalAddress,
                    IsLatest = true,
                    Type = (int) ContractType.Approval
                },
                true);
            OTContract.InsertOrUpdate(connection,
                new OTContract
                {
                    Address = holdingAddress,
                    IsLatest = true,
                    Type = (int) ContractType.Holding
                },
                true);
            OTContract.InsertOrUpdate(connection,
                new OTContract
                {
                    Address = holdingStorageAddress,
                    IsLatest = true,
                    Type = (int) ContractType.HoldingStorage
                }, true);
            OTContract.InsertOrUpdate(connection,
                new OTContract
                {
                    Address = profileAddress,
                    IsLatest = true,
                    Type = (int) ContractType.Profile
                },
                true);
            OTContract.InsertOrUpdate(connection,
                new OTContract
                {
                    Address = profileStorageAddress,
                    IsLatest = true,
                    Type = (int) ContractType.ProfileStorage
                }, true);
            OTContract.InsertOrUpdate(connection,
                new OTContract {Address = tokenAddress, IsLatest = true, Type = (int) ContractType.Token},
                true);
            OTContract.InsertOrUpdate(connection,
                new OTContract
                {
                    Address = readingAddress,
                    IsLatest = true,
                    Type = (int) ContractType.Reading
                },
                true);
            OTContract.InsertOrUpdate(connection,
                new OTContract
                {
                    Address = readingStorageAddress,
                    IsLatest = true,
                    Type = (int) ContractType.ReadingStorage
                }, true);
        }

        public GetLatestContractsTask() : base("Get Latest Contracts")
        {
        }
    }
}