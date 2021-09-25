using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MySqlConnector;
using OTHub.Settings;

namespace OTHub.APIServer.Controllers
{
    [Route("api/[controller]")]
    public class ReportsController : Controller
    {
        private readonly IMemoryCache _cache;

        public ReportsController(IMemoryCache cache)
        {
            _cache = cache;
        }

        [HttpGet]
        [Route(("StakedTokensByDay"))]
        public async Task<StakedTokensByDayModel[]> StakedTokensByDay()
        {
            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                return (await connection.QueryAsync<StakedTokensByDayModel>(@"SELECT * FROM stakedtokensbyday")).ToArray();
            }
        }

        [HttpGet]
        [Route(("JobHeatmap"))]
        public async Task<JobHeatmapModel[]> JobHeatmap([FromQuery] string blockchain)
        {
            if (string.IsNullOrWhiteSpace(blockchain))
                blockchain = null;

            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var now = DateTime.Now.Date.AddDays(1);

                JobHeatmapModel[] rows = (await connection.QueryAsync<JobHeatmapModel>(
                        @"SELECT DAYNAME(o.CreatedTimestamp) `Day`, HOUR(o.CreatedTimestamp) Hour, COUNT(o.OfferID) Count 
FROM otoffer o
JOIN blockchains b on b.id = o.BlockchainID
WHERE o.IsFinalized = 1 AND o.CreatedTimestamp >= @dateFrom AND o.CreatedTimestamp <= @dateTo AND (@blockchain is null OR b.DisplayName = @blockchain)
GROUP BY DAYNAME(o.CreatedTimestamp), HOUR(o.FinalizedTimestamp)
ORDER BY FIELD(DAYNAME(o.CreatedTimestamp) , 'MONDAY', 'TUESDAY', 'WEDNESDAY', 'THURSDAY', 'FRIDAY', 'SATURDAY', 'SUNDAY'), Hour",
                        new
                        {
                            blockchain,
                            dateFrom = now.AddDays(-15),
                            dateTo = now
                        })
                    ).ToArray();

                List<JobHeatmapModel> items = new List<JobHeatmapModel>();

                string[] days = new[] {"Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"};

                foreach (string day in days)
                {
                    foreach (var i in Enumerable.Range(0, 24))
                    {
                        JobHeatmapModel match = rows.FirstOrDefault(r => r.Day == day && r.Hour == i);

                        if (match == null)
                        {
                            match = new JobHeatmapModel(){Day = day, Hour = i, Count = 0};
                        }

                        items.Add(match);
                    }
                }

                return items.ToArray();
            }
        }

        [HttpGet]
        [Route(("HoldingTimePerMonth"))]
        public async Task<HoldingTimePerMonthReportModel> HoldingTimePerMonth()
        {
            string key = "Reports/HoldingTimePerMonth";

            if (_cache.TryGetValue(key, out var cached) && cached is HoldingTimePerMonthReportModel reportModel)
            {
                return reportModel;
            }

            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                HoldingTimePerMonthDataModel[] rows = (await connection.QueryAsync<HoldingTimePerMonthDataModel>(@"WITH CTE AS (
SELECT YEAR(o.FinalizedTimestamp) 'Year', MONTH(o.FinalizedTimestamp) 'Month', ROUND(o.HoldingTimeInMinutes / 43800) HoldingTimeInMonths, o.OfferID, b.BlockchainName
FROM otoffer o
JOIN blockchains b ON b.ID = o.BlockchainID
WHERE o.IsFinalized = 1
)

SELECT o.`Year`, o.`Month`, o.HoldingTimeInMonths, COUNT(o.OfferID) 'Count', o.BlockchainName
FROM CTE o
GROUP BY o.HoldingTimeInMonths, `Year`, `Month`, o.BlockchainName
ORDER BY `Year`, `Month`, o.HoldingTimeInMonths, o.BlockchainName")).ToArray();

                List<HoldingTimePerMonthModel> list = new List<HoldingTimePerMonthModel>();

                foreach (var group in rows.GroupBy(b => new {b.Year, b.Month, b.HoldingTimeInMonths}))
                {
                    HoldingTimePerMonthModel model = new HoldingTimePerMonthModel
                    {
                        Year = group.Key.Year.Replace(",", ""),
                        Month = group.Key.Month,
                        HoldingTimeInMonths = group.Key.HoldingTimeInMonths
                    };

                    model.BlockchainCounts = group.Select(g => new HoldingTimePerMonthBlockchainModel
                        {BlockchainName = g.BlockchainName, Count = g.Count}).ToArray();

                    list.Add(model);
                }

                HoldingTimePerMonthReportModel report = new HoldingTimePerMonthReportModel();
                report.Data = list.ToArray();

                _cache.Set(key, report, TimeSpan.FromMinutes(3));

                return report;
            }
        }
    }

    public class StakedTokensByDayModel
    {
        public DateTime Date { get; set; }
        public string Deposited { get; set; }
        public string   Withdrawn { get; set; }
        public string Staked { get; set; }
    }

    public class HoldingTimePerMonthDataModel
    {
        public string Year { get; set; }
        public int Month { get; set; }
        public int HoldingTimeInMonths { get; set; }
        public int Count { get; set; }
        public string BlockchainName { get; set; }
    }

    public class HoldingTimePerMonthReportModel
    {
        public HoldingTimePerMonthModel[] Data { get; set; }
        public int[] HoldingTimesAvailable => Data.Select(d => d.HoldingTimeInMonths).Distinct().OrderBy(b => b).ToArray();
    }

    public class HoldingTimePerMonthModel
    {
        public string Year { get; set; }
        public int Month { get; set; }
        public int HoldingTimeInMonths { get; set; }
        public int Count => BlockchainCounts.Sum(c => c.Count);
        public HoldingTimePerMonthBlockchainModel[] BlockchainCounts { get; set; }
    }

    public class HoldingTimePerMonthBlockchainModel
    {
        public string BlockchainName { get; set; }
        public int Count { get; set; }
    }

    public class JobHeatmapModel
    {
        private int _hour;
        public string Day { get; set; }

        internal int Hour
        {
            get => _hour;
            set
            {
                _hour = value;
                HourText = value + ":00";
            }
        }

        public string HourText { get; set; }
        public int Count { get; set; }
    }
}