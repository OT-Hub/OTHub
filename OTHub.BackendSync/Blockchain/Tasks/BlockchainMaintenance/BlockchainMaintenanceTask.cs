using System;
using System.Threading.Tasks;
using MySqlConnector;
using Nethereum.Web3;
using OTHub.BackendSync.Blockchain.Tasks.Misc.Children;
using OTHub.BackendSync.Logging;
using OTHub.Settings;
using OTHub.Settings.Constants;

namespace OTHub.BackendSync.Blockchain.Tasks.BlockchainMaintenance
{
    public class BlockchainMaintenanceTask : TaskRunBlockchain
    {
        public override bool ContinueRunningChildrenOnError { get; } = false;

        public BlockchainMaintenanceTask() : base(TaskNames.BlockchainMaintenance)
        {
            Add(new GetLatestContractsTask());
            Add(new RefreshAllHolderLitigationStatusesTask());
        }

        public override TimeSpan GetExecutingInterval(BlockchainType type)
        {
            return TimeSpan.FromHours(4);
        }

        public override async Task<bool> Execute(Source source, BlockchainType blockchain, BlockchainNetwork network, IWeb3 web3, int blockchainID)
        {
                return await RunChildren(source, blockchain, network, blockchainID, web3);
        }
    }
}