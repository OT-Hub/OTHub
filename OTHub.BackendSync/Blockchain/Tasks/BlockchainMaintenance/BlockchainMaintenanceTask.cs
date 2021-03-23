using System;
using System.Threading.Tasks;
using MySqlConnector;
using OTHub.BackendSync.Blockchain.Tasks.Misc.Children;
using OTHub.BackendSync.Logging;
using OTHub.Settings;

namespace OTHub.BackendSync.Blockchain.Tasks.BlockchainMaintenance
{
    public class BlockchainMaintenanceTask : TaskRunBlockchain
    {
        public BlockchainMaintenanceTask() : base("Blockchain Maintenance")
        {
            Add(new GetLatestContractsTask());
            Add(new RefreshAllHolderLitigationStatusesTask());
            Add(new MarkOldContractsAsArchived());
        }

        public override TimeSpan GetExecutingInterval(BlockchainType type)
        {
            return TimeSpan.FromHours(3);
        }

        public override async Task Execute(Source source, BlockchainType blockchain, BlockchainNetwork network)
        {
            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                int blockchainID = GetBlockchainID(connection, blockchain, network);

                await RunChildren(source, blockchain, network, blockchainID);
            }
        }
    }
}