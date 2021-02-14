using System.Threading.Tasks;
using MySqlConnector;
using OTHub.BackendSync.Logging;
using OTHub.Settings;

namespace OTHub.BackendSync.Ethereum.Tasks.BlockchainMaintenance
{
    public class BlockchainMaintenanceTask : TaskRunBlockchain
    {
        public BlockchainMaintenanceTask() : base("Blockchain Maintenance")
        {
            Add(new GetLatestContractsTask());
            Add(new RefreshAllHolderLitigationStatusesTask());
            Add(new MarkOldContractsAsArchived());
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