using System;

namespace OTHub.APIServer.Sql.Models.Nodes.DataHolder
{
    public class DataHolderLitigationSummary
    {
        public String TransactionHash { get; set; }
        public DateTime Timestamp { get; set; }
        public String OfferId { get; set; }
        public UInt64 RequestedObjectIndex { get; set; }
        public UInt64 RequestedBlockIndex { get; set; }
    }
}