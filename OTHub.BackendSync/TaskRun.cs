
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC;
using Nethereum.Web3;
using OTHub.BackendSync.Database.Models;
using OTHub.BackendSync.Logging;
using OTHub.Settings;

namespace OTHub.BackendSync
{
    public abstract class TaskRun
    {
        private readonly List<TaskRun> _childTasks = new List<TaskRun>();

        private HexBigInteger _latestBlockNumber;


        //public static uint SyncBlockNumber { get; }
        //public static uint FromBlockNumber { get; }
        
        
        

        //public static string[] OldHubAddressesSinceVersion3 { get; } = new string[0];

        static TaskRun()
        {

            //SyncBlockNumber = OTHubSettings.Instance.Blockchain.StartSyncFromBlockNumber;
            //FromBlockNumber = OTHubSettings.Instance.Blockchain.StartSyncFromBlockNumber;


        }

        protected int GetBlockchainID(MySqlConnection connection, BlockchainType blockchain, BlockchainNetwork network)
        {
            var id = connection.ExecuteScalar<int?>(
                "select ID FROM blockchains where BlockchainName = @blockchainName AND NetworkName = @networkName", new
                {
                    blockchainName = blockchain.ToString(),
                    networkName = network.ToString()
                });

            return id.Value;
        }

        public HexBigInteger LatestBlockNumber
        {
            get => _latestBlockNumber;
            protected set
            {
                _latestBlockNumber = value;

                foreach (var childTask in _childTasks)
                {
                    childTask.LatestBlockNumber = value;
                }
            }
        }

        protected void Add(TaskRun task)
        {
            _childTasks.Add(task);
        }

        protected Web3 GetWeb3(MySqlConnection connection, int blockchainID)
        {
            string nodeUrl = connection.ExecuteScalar<string>(@"SELECT BlockchainNodeUrl FROM blockchains WHERE id = @id", new
            {
                id = blockchainID
            });

            var cl = new Web3(nodeUrl);

            RequestInterceptor r = new LogRequestInterceptor();
            cl.Client.OverridingRequestInterceptor = r;

            return cl;
        }

        protected async Task RunChildren(Source source, BlockchainType blockchain, BlockchainNetwork network)
        {
            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var cl = GetWeb3(connection, GetBlockchainID(connection, blockchain, network));

                var latestBlockNumber = await cl.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                LatestBlockNumber = new HexBigInteger(latestBlockNumber.Value - 1);

                foreach (var childTask in _childTasks)
                {
                    var status = new SystemStatus(childTask.Name);

                    Logger.WriteLine(Source.BlockchainSync, "Starting " + childTask.Name);
                    try
                    {

                        status.InsertOrUpdate(connection, true, null, true);


                        await childTask.Execute(source, blockchain, network);
                    }
                    catch
                    {
                        try
                        {

                            status.InsertOrUpdate(connection, false, null, false);

                        }
                        catch
                        {

                        }

                        throw;
                    }

                    try
                    {

                        status.InsertOrUpdate(connection, true, null, false);

                    }
                    catch
                    {

                    }
                }
            }
        }

        public string Name { get; }

        protected TaskRun(string name)
        {
            Name = name;
        }
        public abstract Task Execute(Source source, BlockchainType blockchain, BlockchainNetwork network);
    }
}