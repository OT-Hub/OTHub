
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using OTHub.BackendSync.Blockchain.Web3Helper;
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
        public virtual void BlockchainStartup(int blockchainId, BlockchainType blockchain, BlockchainNetwork network)
        {
        }

        public virtual TimeSpan GetExecutingInterval(BlockchainType type)
        {
            return TimeSpan.FromMinutes(5);

        }

        public override string ParentName => _childTasks.Any() ? null : "System";

        public virtual bool ContinueRunningChildrenOnError { get; } = false;

        protected TaskRunBlockchain(string name) : base(name)
        {
        }

        private static readonly ConcurrentDictionary<Tuple<BlockchainType, BlockchainNetwork>, int> _blockchainSyncSizeDictionary = new ConcurrentDictionary<Tuple<BlockchainType, BlockchainNetwork>, int>();
        protected async Task<int> GetBlockchainSyncSize(MySqlConnection connection, BlockchainType blockchain, BlockchainNetwork network)
        {
            int size;

            if (!_blockchainSyncSizeDictionary.TryGetValue(new Tuple<BlockchainType, BlockchainNetwork>(blockchain, network), out size))
            {
                size = await connection.ExecuteScalarAsync<int?>(
                    "select BlockSyncSize FROM blockchains where BlockchainName = @blockchainName AND NetworkName = @networkName", new
                    {
                        blockchainName = blockchain.ToString(),
                        networkName = network.ToString()
                    }) ?? 10000;

                _blockchainSyncSizeDictionary[new Tuple<BlockchainType, BlockchainNetwork>(blockchain, network)] = size;
            }

            return size;
        }

        private static readonly ConcurrentDictionary<Tuple<BlockchainType, BlockchainNetwork>, int?> _blockchainIDDictionary = new ConcurrentDictionary<Tuple<BlockchainType, BlockchainNetwork>, int?>();

        public async Task<int> GetBlockchainID(MySqlConnection connection, BlockchainType blockchain, BlockchainNetwork network)
        {
            int? id;

            if (!_blockchainIDDictionary.TryGetValue(new Tuple<BlockchainType, BlockchainNetwork>(blockchain, network), out id))
            {
                id = await connection.ExecuteScalarAsync<int?>(
                    "select ID FROM blockchains where BlockchainName = @blockchainName AND NetworkName = @networkName", new
                    {
                        blockchainName = blockchain.ToString(),
                        networkName = network.ToString()
                    });

                _blockchainIDDictionary[new Tuple<BlockchainType, BlockchainNetwork>(blockchain, network)] = id;
            }

            return id.Value;
        }


        private static readonly ConcurrentDictionary<int, Web3Factory> _blockchainWeb3Dictionary = new ConcurrentDictionary<int, Web3Factory>();
        public async Task<Web3Factory> GetWeb3(MySqlConnection connection, int blockchainID, BlockchainType type)
        {
            using (await LockManager.GetLock(LockType.GetWeb3).Lock())
            {
                if (!_blockchainWeb3Dictionary.TryGetValue(blockchainID, out Web3Factory loadBalancer))
                {
                    Rpc[] rpcs = await Rpc.GetByBlockchainID(connection, blockchainID);

                    loadBalancer = new Web3Factory(type, rpcs);


                    _blockchainWeb3Dictionary[blockchainID] = loadBalancer;
                }

                return loadBalancer;
            }
        }

        public abstract Task<bool> Execute(Source source, BlockchainType blockchain, BlockchainNetwork network, IWeb3 cl, int blockchainID);

        protected async Task<bool> RunChildren(Source source, BlockchainType blockchain, BlockchainNetwork network,
            int blockchainId, IWeb3 web3)
        {
            bool anyChildFailed = false;

            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                foreach (TaskRunBlockchain childTask in _childTasks)
                {
                    var status = new SystemStatus(childTask.Name, blockchainId);

                    Logger.WriteLine(Source.BlockchainSync, "Starting " + childTask.Name);
                    try
                    {

                        await status.InsertOrUpdate(connection, true, null, true, Name);


                        await childTask.Execute(source, blockchain, network, web3, blockchainId);
                    }
                    catch (Exception ex)
                    {
                        anyChildFailed = true;
                        try
                        {

                            await status.InsertOrUpdate(connection, false, null, false, Name);

                        }
                        catch
                        {

                        }

                        if (ContinueRunningChildrenOnError)
                        {
                            Logger.WriteLine(source, ex.ToString());
                            Logger.WriteLine(source, "Continuing to next child task.");
                            continue;
                        }
                        else
                        {
                            throw;
                        }
                    }

                    try
                    {

                        await status.InsertOrUpdate(connection, true, null, false, Name);

                    }
                    catch
                    {

                    }
                }
            }

            return !anyChildFailed;
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

                        await status.InsertOrUpdate(connection, true, null, true, Name);


                        await childTask.Execute(source);
                    }
                    catch
                    {
                        try
                        {

                            await status.InsertOrUpdate(connection, false, null, false, Name);

                        }
                        catch
                        {

                        }

                        throw;
                    }

                    try
                    {

                        await status.InsertOrUpdate(connection, true, null, false, Name);

                    }
                    catch
                    {

                    }
                }
            }
        }
    }
}