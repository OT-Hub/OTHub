using System;

namespace OTHub.APIServer.Sql.Models.Nodes.DataCreators
{
    public class NodeDataCreatorSummaryModel
    {
        public String Identity { get; set; }
        public String NodeId { get; set; }
        public Int32 Version { get; set; }
        public String StakeTokens { get; set; }
        public String StakeReservedTokens { get; set; }
        public Boolean Approved { get; set; }
        public Int32 OffersTotal { get; set; }
        public Int32 OffersLast7Days { get; set; }
        public Int32 AvgDataSetSizeKB { get; set; }
        public Int32 AvgHoldingTimeInMinutes { get; set; }
        public Int32 AvgTokenAmountPerHolder { get; set; }
        public DateTime CreatedTimestamp { get; set; }
        public DateTime? LastJob { get; set; }
    }
}
