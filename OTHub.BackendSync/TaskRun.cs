
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.Web3;
using OTHub.BackendSync.Database.Models;
using OTHub.BackendSync.Logging;
using OTHub.Settings;

namespace OTHub.BackendSync
{
    public abstract class TaskRunBase
    {
        public string Name { get; }
        public abstract string ParentName { get; }

        protected TaskRunBase(string name)
        {
            Name = name;
        }
    }

    public abstract class TaskRunBase<T> : TaskRunBase where T : TaskRunBase<T>
    {
        private protected readonly List<T> _childTasks = new List<T>();

        protected TaskRunBase(string name) : base(name)
        {
        }

        protected void Add(T task)
        {
            _childTasks.Add(task);
        }

        public bool HasChildTasks => _childTasks.Any();
    }

    public abstract class TaskRunBlockchain : TaskRunBase<TaskRunBlockchain>
    {
        private HexBigInteger _latestBlockNumber;

        public HexBigInteger LatestBlockNumber
        {
            get => _latestBlockNumber;
            protected set
            {
                _latestBlockNumber = value;

                foreach (TaskRunBlockchain childTask in _childTasks)
                {
                    childTask.LatestBlockNumber = value;
                }
            }
        }

        public virtual TimeSpan GetExecutingInterval(BlockchainType type)
        {
            return TimeSpan.FromMinutes(5);

        }

        public override string ParentName => _childTasks.Any() ? null : "System";

        protected TaskRunBlockchain(string name) : base(name)
        {
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

        public abstract Task Execute(Source source, BlockchainType blockchain, BlockchainNetwork network);

        protected async Task RunChildren(Source source, BlockchainType blockchain, BlockchainNetwork network,
            int blockchainId)
        {
            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var cl = GetWeb3(connection, GetBlockchainID(connection, blockchain, network));

                int defaultBlocksToIgnore = 2;

                if (blockchain == BlockchainType.xDai)
                {
                    defaultBlocksToIgnore = 40;
                }

                var latestBlockNumber = await cl.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                LatestBlockNumber = new HexBigInteger(latestBlockNumber.Value - defaultBlocksToIgnore);

                foreach (TaskRunBlockchain childTask in _childTasks)
                {
                    var status = new SystemStatus(childTask.Name, blockchainId);

                    Logger.WriteLine(Source.BlockchainSync, "Starting " + childTask.Name);
                    try
                    {

                        status.InsertOrUpdate(connection, true, null, true, Name);


                        await childTask.Execute(source, blockchain, network);
                    }
                    catch
                    {
                        try
                        {

                            status.InsertOrUpdate(connection, false, null, false, Name);

                        }
                        catch
                        {

                        }

                        throw;
                    }

                    try
                    {

                        status.InsertOrUpdate(connection, true, null, false, Name);

                    }
                    catch
                    {

                    }
                }
            }
        }
    }

    public abstract class TaskRunGeneric : TaskRunBase<TaskRunGeneric>
    {
        protected TaskRunGeneric(string name) : base(name)
        {
        }

        public abstract Task Execute(Source source);

        public override string ParentName => _childTasks.Any() ? null : "System";

        protected async Task RunChildren(Source source)
        {
            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                foreach (TaskRunGeneric childTask in _childTasks)
                {
                    var status = new SystemStatus(childTask.Name);

                    Logger.WriteLine(Source.BlockchainSync, "Starting " + childTask.Name);
                    try
                    {

                        status.InsertOrUpdate(connection, true, null, true, Name);


                        await childTask.Execute(source);
                    }
                    catch
                    {
                        try
                        {

                            status.InsertOrUpdate(connection, false, null, false, Name);

                        }
                        catch
                        {

                        }

                        throw;
                    }

                    try
                    {

                        status.InsertOrUpdate(connection, true, null, false, Name);

                    }
                    catch
                    {

                    }
                }
            }
        }
    }
}