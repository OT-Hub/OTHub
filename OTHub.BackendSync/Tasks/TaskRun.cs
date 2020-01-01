using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using OTHub.BackendSync.Models.Database;
using OTHub.Settings;

namespace OTHub.BackendSync.Tasks
{
    public class LogRequestInterceptor : RequestInterceptor
    {
        public override Task InterceptSendRequestAsync(Func<RpcRequest, string, Task> interceptedSendRequestAsync, RpcRequest request, string route = null)
        {
#if DEBUG
            Logger.WriteLine(Source.NodeUptimeAndMisc, request.Method);
#endif

            return base.InterceptSendRequestAsync(interceptedSendRequestAsync, request, route);
        }

        public override Task<object> InterceptSendRequestAsync<T>(Func<RpcRequest, string, Task<T>> interceptedSendRequestAsync, RpcRequest request, string route = null)
        {
#if DEBUG
            Logger.WriteLine(Source.NodeUptimeAndMisc, request.Method);
#endif

            return base.InterceptSendRequestAsync(interceptedSendRequestAsync, request, route);
        }

        public override Task<object> InterceptSendRequestAsync<T>(Func<string, string, object[], Task<T>> interceptedSendRequestAsync, string method, string route = null,
            params object[] paramList)
        {
#if DEBUG
            Logger.WriteLine(Source.NodeUptimeAndMisc, method);
#endif

            return base.InterceptSendRequestAsync(interceptedSendRequestAsync, method, route, paramList);
        }

        public override Task InterceptSendRequestAsync(Func<string, string, object[], Task> interceptedSendRequestAsync, string method, string route = null,
            params object[] paramList)
        {
#if DEBUG
            Logger.WriteLine(Source.NodeUptimeAndMisc, method);
#endif

            return base.InterceptSendRequestAsync(interceptedSendRequestAsync, method, route, paramList);
        }
    }

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

        protected async Task RunChildren(Source source)
        {
            var latestBlockNumber = await cl.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            LatestBlockNumber = new HexBigInteger(latestBlockNumber.Value - 1);

            foreach (var childTask in _childTasks)
            {
                Logger.WriteLine(Source.BlockchainSync, "Starting " + childTask.Name);
                try
                {
                    await childTask.Execute(source);
                }
                catch
                {
                    try
                    {
                        using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                        {
                            new SystemStatus(childTask.Name).InsertOrUpdate(connection, false);
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
                        new SystemStatus(childTask.Name).InsertOrUpdate(connection, true);
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
        public abstract Task Execute(Source source);
    }
}