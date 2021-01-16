using System.Collections.Generic;
using System.Threading.Tasks;
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
        public static Web3 cl { get; } 
        public static EthApiService eth { get; }

        public static uint SyncBlockNumber { get; }
        public static uint FromBlockNumber { get; }
        
        
        

        //public static string[] OldHubAddressesSinceVersion3 { get; } = new string[0];

        static TaskRun()
        {

            SyncBlockNumber = OTHubSettings.Instance.Blockchain.StartSyncFromBlockNumber;
            FromBlockNumber = OTHubSettings.Instance.Blockchain.StartSyncFromBlockNumber;

            cl = new Web3(OTHubSettings.Instance.Infura.Url);
            eth = new EthApiService(cl.Client);

            RequestInterceptor r = new LogRequestInterceptor();
            cl.Client.OverridingRequestInterceptor = r;
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

        protected async Task RunChildren(Source source, Blockchain blockchain, Network network)
        {
            var latestBlockNumber = await cl.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            LatestBlockNumber = new HexBigInteger(latestBlockNumber.Value - 1);

            foreach (var childTask in _childTasks)
            {
                var status = new SystemStatus(childTask.Name);

                Logger.WriteLine(Source.BlockchainSync, "Starting " + childTask.Name);
                try
                {
                    using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                    {
                        status.InsertOrUpdate(connection, true, null, true);
                    }

                    await childTask.Execute(source, blockchain, network);
                }
                catch
                {
                    try
                    {
                        using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                        {
                            status.InsertOrUpdate(connection, false, null, false);
                        }
                    }
                    catch
                    {

                    }

                    throw;
                }

                try
                {
                    using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                    {
                        status.InsertOrUpdate(connection, true, null, false);
                    }
                }
                catch
                {

                }
            }
        }

        public string Name { get; }

        protected TaskRun(string name)
        {
            Name = name;
        }
        public abstract Task Execute(Source source, Blockchain blockchain, Network network);
    }
}