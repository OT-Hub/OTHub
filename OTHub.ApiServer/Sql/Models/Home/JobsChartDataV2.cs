using System;

namespace OTHub.APIServer.Sql.Models.Home
{
    public class JobsChartDataV2Model
    {
        public String Label { get; set; }
        public DateTime Date { get; set; }
        public int NewJobs { get; set; }
        public int CompletedJobs { get; set; }
    }
    public class JobsChartDataV2
    {
        public int[][] Week { get; set; }
        public int[][] Month { get; set; }
        public int[][] Year { get; set; }
        public string[] WeekLabels { get; set; }
        public string[] MonthLabels { get; set; }
        public string[] YearLabels { get; set; }
    }

    public class NodesChartDataV2
    {
        public int[][] Week { get; set; }
        public string[] WeekLabels { get; set; }
    }

    public class JobChartDataV2SummaryModel
    {
        public int OffersLast24Hours { get; set; }
        public int OffersLast7Days { get; set; }
        public int OffersLastMonth { get; set; }
        public int OffersActive { get; set; }
    }
}