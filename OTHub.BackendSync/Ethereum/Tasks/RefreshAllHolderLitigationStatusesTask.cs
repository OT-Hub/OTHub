using System.Threading.Tasks;
using MySql.Data.MySqlClient;
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

        public override async Task Execute(Source source)
        {
            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                OTOfferHolder.UpdateLitigationForAllOffers(connection);
            }
        }
    }
}