using System;

namespace OTHub.APIServer.Sql.Models.Jobs
{
    public class OfferDetailedTimelineEventModel
    {
        public DateTime Timestamp { get; set; }
        public string Name { get; set; }
        public string RelatedTo { get; set; }
        public string TransactionHash { get; set; }
    }
}
