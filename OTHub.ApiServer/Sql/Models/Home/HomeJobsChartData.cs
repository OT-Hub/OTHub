using System;

namespace OTHub.APIServer.Sql.Models.Home
{
    public class HomeJobsChartData
    {
        public String[] Labels { get; set; }
        public Int32[] NewJobs { get; set; }
        public Int32[] ActiveJobs { get; set; }
    }
}
