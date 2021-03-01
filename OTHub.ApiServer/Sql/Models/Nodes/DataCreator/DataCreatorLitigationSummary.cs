using System;

namespace OTHub.APIServer.Sql.Models.Nodes.DataCreator
{
    public class DataCreatorLitigationSummary
    {
        public String TransactionHash { get; set; }
        public DateTime Timestamp { get; set; }
        public String OfferId { get; set; }
        public UInt64 RequestedObjectIndex { get; set; }
        public UInt64 RequestedBlockIndex { get; set; }
        public String NodeId { get; set; }
    }
}