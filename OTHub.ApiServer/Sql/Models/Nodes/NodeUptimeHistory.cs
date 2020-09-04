using System;

namespace OTHub.APIServer.Sql.Models.Nodes
{
    public class NodeUptimeHistory
    {
        public DateTime? LastSuccess { get; set; }
        public DateTime? LastCheck { get; set; }
        public int TotalSuccess24Hours { get; set; }
        public int TotalFailed24Hours { get; set; }
        public int TotalSuccess7Days { get; set; }
        public int TotalFailed7Days { get; set; }
        public string ChartData { get; set; }
    }
}