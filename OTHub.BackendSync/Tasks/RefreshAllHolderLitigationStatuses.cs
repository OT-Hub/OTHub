using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using OTHelperNetStandard.Models.Database;
using OTHub.Settings;

namespace OTHelperNetStandard.Tasks
{
    public class RefreshAllHolderLitigationStatuses : TaskRun
    {
        public RefreshAllHolderLitigationStatuses() : base("Refresh All Holder Litigation Statuses")
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