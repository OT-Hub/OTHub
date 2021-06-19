using System;

namespace OTHub.APIServer.Sql.Models.Nodes
{
    public class NodeProfileDetailedModel_OfferSummary
    {
        public string Identity { get; set; }
        public string NodeId { get; set; }
        public String OfferId { get; set; }
        public DateTime FinalizedTimestamp { get; set; }
        public UInt64 HoldingTimeInMinutes { get; set; }
        public Boolean Paidout { get; set; }
        public string PaidoutAmount { get; set; }
        public Boolean CanPayout { get; set; }
        public string TokenAmountPerHolder { get; set; }
        public DateTime EndTimestamp { get; set; }
        public String Status { get; set; }
        public bool IsOriginalHolder { get; set; }
        public bool IsMyNode { get; set; }
        public int BlockchainID { get; set; }
    }
}