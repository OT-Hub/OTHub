using System;

namespace OTHub.APIServer.Sql.Models.Jobs
{
    public class OfferDetailedHolderModel
    {
        public String NodeId { get; set; }
        public Int32? LitigationStatus { get; set; }
        public String LitigationStatusText { get; set; }
        public DateTime JobStarted { get; set; }
        public DateTime JobCompleted { get; set; }
        public string DisplayName { get; set; }
    }
}
