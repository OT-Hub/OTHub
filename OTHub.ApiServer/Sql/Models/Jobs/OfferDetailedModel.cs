using System;

namespace OTHub.APIServer.Sql.Models.Jobs
{
    public class OfferDetailedModel : OfferSummaryModel
    {
        public String DataSetId { get; set; }
        public UInt64 CreatedBlockNumber { get; set; }
        public string CreatedTransactionHash { get; set; }
        public string DCNodeId { get; set; }
        //public string DCIdentity { get; set; }
        public OfferDetailedHolderModel[] Holders { get; set; }
        public Int32 OffersTotal { get; set; }
        public Int32 OffersLast7Days { get; set; }
        public String PaidoutTokensTotal { get; set; }
        public UInt64? FinalizedBlockNumber { get; set; }
        public String FinalizedTransactionHash { get; set; }
        public UInt64 CreatedGasUsed { get; set; }
        public UInt64 CreatedGasPrice { get; set; }
        public UInt64? FinalizedGasUsed { get; set; }
        public UInt64? FinalizedGasPrice { get; set; }
        public UInt64 LitigationIntervalInMinutes { get; set; }
        public OfferDetailedTimelineEventModel[] TimelineEvents { get; set; }
        public Decimal? EstimatedLambda { get; set; }
    }
}
