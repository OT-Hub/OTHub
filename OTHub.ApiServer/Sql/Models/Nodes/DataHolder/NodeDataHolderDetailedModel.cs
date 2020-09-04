using System;

namespace OTHub.APIServer.Sql.Models.Nodes.DataHolder
{
    public class NodeDataHolderDetailedModel : NodeDetailedModel
    {
        public String PaidTokens { get; set; }
        public Int32 TotalWonOffers { get; set; }
        public Int32 WonOffersLast7Days { get; set; }

        //public NodeProfileDetailedModel_OfferSummary[] Offers { get; set; }
        //public NodeProfileDetailedModel_OfferPayout[] Payouts { get; set; }
        //public DataHolderLitigationSummary[] Litigations { get; set; }
    }
}