using System.Threading.Tasks;
using MySqlConnector;
using OTHub.BackendSync.Database.Models;
using OTHub.BackendSync.Logging;
using OTHub.Settings;

namespace OTHub.BackendSync.Ethereum.Tasks
{
    public class RefreshAllHolderLitigationStatusesTask : TaskRun
    {
        public RefreshAllHolderLitigationStatusesTask() : base("Refresh All Holder Litigation Statuses")
        {
        }

        public override async Task Execute(Source source, Blockchain blockchain, Network network)
        {
            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                int blockchainID = GetBlockchainID(connection, blockchain, network);

                await OTOfferHolder.UpdateLitigationForAllOffers(connection, blockchainID);
            }
        }
    }
}