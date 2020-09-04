using System;

namespace OTHub.APIServer.Sql.Models.Home
{
    public class HomeNodesInfo
    {
        public Int32 OnlineNodesCount { get; set; }
        public Int32 ApprovedNodesCount { get; set; }
        public Int32 NodesWithActiveJobs { get; set; }
        public Int32 NodesWithJobsThisWeek { get; set; }
        public Int32 NodesWithJobsThisMonth { get; set; }
        public Decimal StakedTokensTotal { get; set; }
        public Decimal LockedTokensTotal { get; set; }
        public DateTime? LastApprovalTimestamp { get; set; }
        public int? LastApprovalAmount { get; set; }
    }
}
