using System;

namespace OTHub.APIServer.Sql.Models.RecentActivity
{
    public class RecentActivityJobModel
    {
        public String Identity { get; set; }
        public String OfferId { get; set; }
        public DateTime Timestamp { get; set; }
        public String TokenAmountPerHolder { get; set; }
        public DateTime EndTimestamp { get; set; }
    }
}
