using System;
using System.Threading.Tasks;
using MySqlConnector;
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
            Add(new MarkOldContractsAsArchived());
        }

        public override TimeSpan GetExecutingInterval(BlockchainType type)
        {
            return TimeSpan.FromHours(3);
        }

        public override async Task<bool> Execute(Source source, BlockchainType blockchain, BlockchainNetwork network)
        {
            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                int blockchainID = await GetBlockchainID(connection, blockchain, network);

                return await RunChildren(source, blockchain, network, blockchainID);
            }
        }
    }
}