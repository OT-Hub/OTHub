using System;

namespace OTHub.APIServer.Sql.Models.Jobs
{
    public class OfferDetailedHolderModel
    {
        public String Identity { get; set; }
        public Int32? LitigationStatus { get; set; }
        public String LitigationStatusText { get; set; }
    }
}
