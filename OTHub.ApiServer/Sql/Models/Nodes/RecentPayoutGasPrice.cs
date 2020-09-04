using System;

namespace OTHub.APIServer.Sql.Models.Nodes
{
    public class RecentPayoutGasPrice
    {
        public Decimal GasPrice { get; set; }
        public Decimal GasUsed { get; set; }
        public Int32 TotalCount { get; set; }
    }

}
