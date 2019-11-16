using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC;
using Nethereum.Web3;
using OTHub.Settings;

namespace OTHelperNetStandard.Tasks
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
                await childTask.Execute(source);
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