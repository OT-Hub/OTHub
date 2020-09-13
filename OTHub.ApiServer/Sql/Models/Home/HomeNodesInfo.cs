using System;

namespace OTHub.APIServer.Sql.Models.Home
{
    public class HomeNodesInfo
    {
        public Int32 OnlineNodesCount { get; set; }
        public Int32 NodesWithActiveJobs { get; set; }
        public Int32 NodesWithJobsThisWeek { get; set; }
        public Int32 NodesWithJobsThisMonth { get; set; }
    }
}
