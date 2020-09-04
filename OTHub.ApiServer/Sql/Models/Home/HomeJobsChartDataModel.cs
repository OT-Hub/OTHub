using System;

namespace OTHub.APIServer.Sql.Models.Home
{
    public class HomeJobsChartDataModel
    {
        public DateTime Date { get; set; }
        public Int32 NewJobs { get; set; }
        public Int32 ActiveJobs { get; set; }
    }

}
