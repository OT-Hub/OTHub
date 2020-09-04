using System;

namespace OTHub.APIServer.Sql.Models.Nodes
{
    public class NodeProfileDetailedModel_OfferSummary
    {
        public String OfferId { get; set; }
        public DateTime FinalizedTimestamp { get; set; }
        public UInt64 HoldingTimeInMinutes { get; set; }
        public Boolean Paidout { get; set; }
        public Boolean CanPayout { get; set; }
        public string TokenAmountPerHolder { get; set; }
        public DateTime EndTimestamp { get; set; }
        public String Status { get; set; }
        public bool IsOriginalHolder { get; set; }
    }
}