//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Dapper;
//using MySqlConnector;
//using Nethereum.ABI.FunctionEncoding;
//using Nethereum.Contracts;
//using Nethereum.JsonRpc.Client;
//using Nethereum.RPC;
//using Nethereum.RPC.Accounts;
//using Nethereum.RPC.Eth.DTOs;
//using Nethereum.Web3;
//using Nethereum.Web3.Accounts;
//using OTHub.BackendSync.Blockchain.Tasks.BlockchainSync;
//using OTHub.BackendSync.Database.Models;
//using OTHub.BackendSync.Logging;
//using OTHub.Settings;
//using OTHub.Settings.Abis;

//namespace OTHub.BackendSync.Blockchain.Tasks.Misc.Children
//{
//    public class xDaiBountyTask : TaskRunGeneric
//    {
//        public xDaiBountyTask() : base("xDai Bounty Smart Contract")
//        {
//        }

//        public override async Task Execute(Source source)
//        {
//            if ((Settings.OTHubSettings.Instance.MariaDB.TempBountyKey ?? "") == "")
//                return;

//            Logger.WriteLine(source, "Starting xdai bounty task.");

//            await using (var connection =
//                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
//            {
//                string nodeUrl = connection.ExecuteScalar<string>(@"SELECT BlockchainNodeUrl FROM blockchains WHERE BlockchainName = 'xDai' and NetworkNAme = 'Mainnet'");

//                var cl = new Web3(nodeUrl);

//                RequestInterceptor r = new LogRequestInterceptor();
//                cl.Client.OverridingRequestInterceptor = r;

//                var eth = new EthApiService(cl.Client);

//                var abi = AbiHelper.GetContractAbi(ContractTypeEnum.StarfleetBounty, BlockchainType.xDai,
//                    BlockchainNetwork.Mainnet);

//                Contract contract = new Contract(eth, abi, "0xf5C684AF58fe45C9C572034694484a90258EFAD3");



//                Event addedEvent = contract.GetEvent("BountyAdded");

//                Event bountyWithdrawnEvent = contract.GetEvent("BountyWithdrawn");

//                var toBlock = BlockParameter.CreateLatest();

//                var events = await addedEvent.GetAllChangesDefault(
//                    addedEvent.CreateFilterInput(null,
//                        toBlock));

//                IAccount wallet = new Account(Settings.OTHubSettings.Instance.MariaDB.TempBountyKey);
//                var web3 = new Web3(wallet, nodeUrl);

//                foreach (EventLog<List<ParameterOutput>> eventLog in events)
//                {
//                    var staker = eventLog.Event.FirstOrDefault(p => p.Parameter.Name == "staker").Result as string;

//                    var exists = connection.ExecuteScalar<bool>(@"SELECT 1 FROM xdaibounty where Address = @address", new
//                    {
//                        address = staker
//                    });

//                    if (!exists)
//                    {
//                        connection.Execute(@"INSERT INTO xdaibounty(Address, HasClaimed, Tried, Sent) 
//VALUES (@address, @hasClaimed, @tried, @sent)", new
//                        {
//                            address = staker,
//                            hasClaimed = false,
//                            tried = false,
//                            sent = false
//                        });
//                        }
//                }

//                events = await bountyWithdrawnEvent.GetAllChangesDefault(
//                    bountyWithdrawnEvent.CreateFilterInput(null,
//                        toBlock));

//                foreach (EventLog<List<ParameterOutput>> eventLog in events)
//                {
//                    var staker = eventLog.Event.FirstOrDefault(p => p.Parameter.Name == "staker").Result as string;

//                    connection.Execute(@"UPDATE xdaibounty set hasclaimed = 1 where Address = @address", 
//                        new
//                        {
//                            address = staker
//                        });
//                }

//                var toTry = connection.Query(@"select * from xdaibounty where hasclaimed = 0 and tried = 0 and sent = 0").ToArray();

//                foreach (var o in toTry)
//                {
//                    string address = o.Address;

//                    var balance = await eth.GetBalance.SendRequestAsync(address);

//                    decimal formatted = Web3.Convert.FromWei(balance);

//                    if (formatted == 0)
//                    {
//                        try
//                        {
//                            Logger.WriteLine(source, "Sending 0.01 xDai to " + address);
//                            TransactionReceipt transaction = await web3.Eth.GetEtherTransferService()
//                                .TransferEtherAndWaitForReceiptAsync(address, 0.01m);
//                            Logger.WriteLine(source, "Sent 0.01 xDai to " + address);

//                            connection.Execute(@"UPDATE xdaibounty set TransactionHash = @hash, sent = 1 where Address = @address",
//                                new
//                                {
//                                    address = address,
//                                    hash = transaction.TransactionHash
//                                });
//                        }
//                        finally
//                        {
//                            connection.Execute(@"UPDATE xdaibounty set tried = 1 where Address = @address",
//                                new
//                                {
//                                    address = address
//                                });
//                        }
//                    }
//                    else
//                    {
//                        connection.Execute(@"UPDATE xdaibounty set tried = 1 where Address = @address",
//                            new
//                            {
//                                address = address
//                            });
//                    }
//                }


//            }
//        }
//    }
//}