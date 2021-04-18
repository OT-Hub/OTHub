using System;

namespace OTHub.APIServer.Sql.Models.Nodes.DataHolders
{
    public class NodeDataHolderSummaryModel
    {
        //public String Identity { get; set; }
        public String NodeId { get; set; }
        public string DisplayName { get; set; }
        public Int32 Version { get; set; }
        public String StakeTokens { get; set; }
        public String StakeReservedTokens { get; set; }
        public String PaidTokens { get; set; }
        public Int32 ActiveOffers { get; set; }
        public Int32 TotalWonOffers { get; set; }
        public Int32 WonOffersLast7Days { get; set; }
        public Boolean Approved { get; set; }

        public DateTime? LastSeenOnline { get; set; }
        public DateTime? LastSeenOffline { get; set; }


        //public string BlockchainName { get; set; }
        //public string NetworkName { get; set; }
    }
}
