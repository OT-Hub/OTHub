//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Numerics;
//using System.Text;
//using System.Threading.Tasks;
//using Dapper;
//using MySqlConnector;
//using Nethereum.ABI.FunctionEncoding;
//using Nethereum.Contracts;
//using Nethereum.Hex.HexTypes;
//using Nethereum.JsonRpc.Client;
//using Nethereum.RPC;
//using Nethereum.RPC.Eth.DTOs;
//using Nethereum.Web3;
//using OTHub.BackendSync.Logging;
//using OTHub.Settings;
//using OTHub.Settings.Abis;

//namespace OTHub.BackendSync.Blockchain.Tasks
//{
//    public class BoardingContractSyncTask : TaskRunGeneric
//    {
//        public BoardingContractSyncTask() : base("Boarding Contract Sync")
//        {
//        }

//        public override async Task Execute(Source source)
//        {

//            await using (var connection =
//                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
//            {
          

//                var cl = GetWeb3();

//                var eth = new EthApiService(cl.Client);

//                var contract = new Contract(eth,
//                    AbiHelper.GetContractAbi(ContractTypeEnum.StarfleetStake, BlockchainType.Ethereum,
//                        BlockchainNetwork.Mainnet), "0x14b9cc4a9a2382d5c5c2b38e6763e51d8fda3777");

//                Event tokenStakedEvent = contract.GetEvent("TokenStaked");
//                Event tokenWithdrawnEvent = contract.GetEvent("TokenWithdrawn");

//                var latestBlockNumber = await cl.Eth.Blocks.GetBlockNumber.SendRequestAsync();

//                latestBlockNumber = new HexBigInteger(latestBlockNumber.Value - 1);

//                BigInteger? startingBlockNumber = connection.ExecuteScalar<long?>(@"SELECT MAX(BlockNumber) FROM starfleetboarding_deposit");

//                if (startingBlockNumber == null)
//                {
//                    startingBlockNumber = 11881527;
//                }
//                else
//                {
//                    startingBlockNumber++;
//                }

//                List<EventLog<List<ParameterOutput>>> eventsOfChange = await tokenStakedEvent.GetAllChangesDefault(
//                    tokenStakedEvent.CreateFilterInput(new BlockParameter(new HexBigInteger(startingBlockNumber.Value)),
//                        new BlockParameter(latestBlockNumber)));

//                foreach (var eventLog in eventsOfChange)
//                {
//                    string staker = eventLog.Event.FirstOrDefault(p => p.Parameter.Name == "staker").Result as string;

//                    decimal amountDeposited = Web3.Convert.FromWei((BigInteger)eventLog.Event
//                        .FirstOrDefault(p => p.Parameter.Name == "amount").Result);

//                    await connection.ExecuteAsync(
//                        @"INSERT INTO starfleetboarding_deposit(Address, Amount, BlockNumber, TransactionHash) VALUES (@staker, @amount, @block, @hash)",
//                        new
//                        {
//                            staker = staker,
//                            amount = amountDeposited,
//                            block = (long)eventLog.Log.BlockNumber.Value,
//                            hash = eventLog.Log.TransactionHash
//                        });
//                }
//            }
//        }

//        protected Web3 GetWeb3()
//        {


//            var cl = new Web3("https://mainnet.infura.io/v3/ec02a1d156fd4da09edbcffb7abad63c");

//            RequestInterceptor r = new LogRequestInterceptor();
//            cl.Client.OverridingRequestInterceptor = r;

//            return cl;
//        }
//    }
//}