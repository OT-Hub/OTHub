using System;

namespace OTHub.APIServer.Sql.Models.Nodes
{
    public class NodeUptimeChartData
    {
        public DateTime Timestamp { get; set; }
        public Boolean Success { get; set; }

        public DateTime EndTimestamp
        {
            get
            {
                return Timestamp.AddMinutes(2);
            }
        }
    }
}