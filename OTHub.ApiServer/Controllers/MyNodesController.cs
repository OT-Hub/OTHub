﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CoinpaprikaAPI.Entity;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MySqlConnector;
using Nethereum.RPC;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using OTHub.APIServer.Helpers;
using OTHub.APIServer.Sql.Models.Nodes;
using OTHub.Settings;
using Swashbuckle.AspNetCore.Annotations;

namespace OTHub.APIServer.Controllers
{
    [Route("api/[controller]")]
    public class MyNodesController : Controller
    {
        private readonly IMemoryCache _cache;

        public MyNodesController(IMemoryCache cache)
        {
            _cache = cache;
        }

        [HttpPost]
        [Authorize]
        [Route("UpdateMyNodesPriceCalculationMode")]
        [SwaggerOperation(Description = "Requires authentication to use.")]
        public async Task UpdateMyNodesPriceCalculationMode([FromQuery]int mode)
        {
            if (mode != 0 && mode != 1)
            {
                return;
            }

            await using (MySqlConnection connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                await connection.ExecuteAsync("UPDATE Users SET USDPriceCalculationMode = @mode WHERE ID = @userID", new
                {
                    userID = User?.Identity.Name, mode
                });
            }

            _cache.Remove("MyNodes-GetRecentJobs-" + User.Identity.Name);
            _cache.Remove("MyNodes-JobsPerMonth-" + User.Identity.Name);
        }

        [HttpGet]
        [Authorize]
        [Route("MyNodesPriceCalculationMode")]
        [SwaggerOperation(Description = "Requires authentication to use.")]
        public async Task<int> GetMyNodesPriceCalculationMode()
        {
            await using (MySqlConnection connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                return await connection.ExecuteScalarAsync<int>(@"SELECT USDPriceCalculationMode FROM Users where ID = @userID", new
                {
                    userID = User?.Identity?.Name
                }, commandTimeout: 120);
            }
        }

        [HttpGet]
        [Authorize]
        [Route("TaxReport")]
        [SwaggerOperation(Description = "Requires authentication to use.")]
        public async Task<TaxReportModel[]> TaxReport([FromQuery]int usdMode, [FromQuery] string nodeID,
            [FromQuery]DateTime startDate, [FromQuery]DateTime endDate,
            [FromQuery] bool includeActiveJobs, [FromQuery] bool includeCompletedJobs)
        {
            //TaxModel model = new TaxModel();

            TaxReportModel[] rows = null;

            var args = new
            {
                userID = User?.Identity?.Name,
                startDate = startDate.Date,
                endDate = endDate.Date,
                nodeID,
                includeActiveJobs,
                includeCompletedJobs
            };


            await using (MySqlConnection connection =
             new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                switch (usdMode)
                {
                    case 0:
                        rows = (await connection.QueryAsync<TaxReportModel>(@"CREATE TEMPORARY TABLE IF NOT EXISTS tmpIdentitiesForQuery AS (
SELECT i.Identity 
FROM otidentity i 
LEFT JOIN mynodes mn ON mn.NodeID = i.NodeId AND mn.UserID = @userID
WHERE (@nodeID IS NOT NULL AND i.NodeId = @nodeID) OR (@nodeID IS NULL AND mn.UserID = @userID)
);

SELECT 
o.OfferID, o.FinalizedTimestamp AS Date, o.TokenAmountPerHolder Amount, ticker.Timestamp TickerTimestamp, ticker.Price TickerUSDPrice, ticker.Price * o.TokenAmountPerHolder USDAmount
FROM otoffer o
JOIN otoffer_holders h ON h.OfferID = o.OfferID AND h.BlockchainID = o.BlockchainID
JOIN tmpIdentitiesForQuery ii ON ii.Identity = h.Holder
JOIN ticker_trac ticker ON ticker.Timestamp = (
SELECT MAX(TIMESTAMP)
FROM ticker_trac
WHERE TIMESTAMP <= o.FinalizedTimestamp)
WHERE o.IsFinalized = 1 
AND o.FinalizedTimestamp >= @startDate 
AND o.FinalizedTimestamp <= @endDate
AND ((@includeActiveJobs = 1 AND DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) > NOW()) 
OR (@includeCompletedJobs = 1 AND DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) < NOW()))", args, commandTimeout: 120)).ToArray();
                        break;
                    case 1:
                        rows = (await connection.QueryAsync<TaxReportModel>(@"CREATE TEMPORARY TABLE IF NOT EXISTS tmpIdentitiesForQuery AS (
SELECT i.Identity 
FROM otidentity i 
LEFT JOIN mynodes mn ON mn.NodeID = i.NodeId AND mn.UserID = @userID
WHERE (@nodeID IS NOT NULL AND i.NodeId = @nodeID) OR (@nodeID IS NULL AND mn.UserID = @userID)
);

SELECT 
o.OfferID, DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) AS Date, o.TokenAmountPerHolder Amount, ticker.Timestamp TickerTimestamp, ticker.Price TickerUSDPrice, ticker.Price * o.TokenAmountPerHolder USDAmount
FROM otoffer o
JOIN otoffer_holders h ON h.OfferID = o.OfferID AND h.BlockchainID = o.BlockchainID
JOIN tmpIdentitiesForQuery ii ON ii.Identity = h.Holder
JOIN ticker_trac ticker ON ticker.Timestamp = (
SELECT MAX(TIMESTAMP)
FROM ticker_trac
WHERE TIMESTAMP <= (CASE WHEN O.IsFinalized = 1  THEN DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) ELSE NULL END))
WHERE o.IsFinalized = 1 
AND DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) >= @startDate
AND DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) <= @endDate
AND ((@includeActiveJobs = 1 AND DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) > NOW()) 
OR (@includeCompletedJobs = 1 AND DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) < NOW()))", args, commandTimeout: 120)).ToArray();
                        break;
                    case 2:
                        rows = (await connection.QueryAsync<TaxReportModel>(@"CREATE TEMPORARY TABLE IF NOT EXISTS tmpIdentitiesForQuery AS (
SELECT i.Identity 
FROM otidentity i 
LEFT JOIN mynodes mn ON mn.NodeID = i.NodeId AND mn.UserID = @userID
WHERE (@nodeID IS NOT NULL AND i.NodeId = @nodeID) OR (@nodeID IS NULL AND mn.UserID = @userID)
);

SELECT 
po.OfferID, po.Timestamp AS Date, po.Amount Amount, ticker.Timestamp TickerTimestamp, ticker.Price TickerUSDPrice, ticker.Price * po.Amount USDAmount
FROM otcontract_holding_paidout po
JOIN otoffer_holders h ON h.OfferID = po.OfferID AND h.Holder = po.Holder AND po.BlockchainID = h.BlockchainID
JOIN otoffer o on o.OfferID = po.OfferID AND o.BlockchainID = po.BlockchainID
JOIN tmpIdentitiesForQuery ii ON ii.Identity = h.Holder
JOIN ticker_trac ticker ON ticker.Timestamp = (
SELECT MAX(TIMESTAMP)
FROM ticker_trac
WHERE TIMESTAMP <= po.Timestamp)
WHERE po.Timestamp >= @startDate 
AND po.Timestamp <= @endDate 
AND o.IsFinalized = 1
AND ((@includeActiveJobs = 1 AND DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) > NOW()) 
OR (@includeCompletedJobs = 1 AND DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) < NOW()))", args, commandTimeout: 120)).ToArray();
                        break;
                }
            }

            //model.Items = rows;

            return rows;
        }

        [HttpGet]
        [Authorize]
        [Route("GetHoldingTimeByMonth")]
        [SwaggerOperation(Description = "Requires authentication to use.")]
        public async Task<GetHoldingTimeByMonthModel[]> GetHoldingTimeByMonth([FromQuery] string nodeID)
        {
            if (string.IsNullOrWhiteSpace(nodeID))
                nodeID = null;

            await using (MySqlConnection connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                GetHoldingTimeByMonthModel[] rows = (await connection.QueryAsync<GetHoldingTimeByMonthModel>(@"WITH CTE AS (
SELECT YEAR(o.FinalizedTimestamp) 'Year', MONTH(o.FinalizedTimestamp) 'Month', ROUND(o.HoldingTimeInMinutes / 43800) HoldingTimeInMonths, o.OfferID, b.BlockchainName
FROM otoffer o
JOIN blockchains b ON b.ID = o.BlockchainID
JOIN otoffer_holders h ON h.OfferID = o.OfferID AND h.BlockchainID = o.BlockchainID
JOIN otidentity i ON i.Identity = h.Holder AND i.BlockchainID = o.BlockchainID
JOIN mynodes mn ON mn.UserID = @userID AND mn.NodeID = i.NodeId
WHERE o.IsFinalized = 1 AND NOW() <= DATE_ADD(o.FinalizedTimeStamp, INTERVAL +o.HoldingTimeInMinutes MINUTE)
AND (@nodeID IS NULL OR mn.NodeID = @nodeID) 
)

SELECT o.HoldingTimeInMonths, COUNT(o.OfferID) 'Count'
FROM CTE o
GROUP BY o.HoldingTimeInMonths
ORDER BY o.HoldingTimeInMonths", new
                {
                    userID = User?.Identity?.Name,
                    nodeID = nodeID
                }, commandTimeout: 120)).ToArray();

                return rows;
            }
        }

        [HttpGet]
        [Authorize]
        [Route("RecentJobs")]
        [SwaggerOperation(Description = "Requires authentication to use.")]
        public async Task<RecentJobsByDay[]> GetRecentJobs([FromQuery] string nodeID)
        {
            if (string.IsNullOrWhiteSpace(nodeID))
                nodeID = null;

            if (_cache.TryGetValue($"MyNodes-GetRecentJobs-{User.Identity.Name}-{nodeID}", out var cached))
            {
                return (RecentJobsByDay[]) cached;
            }

            TickerInfo ticker = await TickerHelper.GetTickerInfo(_cache);

            await using (MySqlConnection connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var jobs = (await connection.QueryAsync<RecentJobs>(@"SELECT mn.NodeID, mn.DisplayName, o.OfferID, o.HoldingTimeInMinutes, o.TokenAmountPerHolder, o.FinalizedTimestamp,
(CASE WHEN u.USDPriceCalculationMode = 0 THEN ticker.Price ELSE @overrideUSDPrice END) * o.TokenAmountPerHolder AS USDAmount,
b.DisplayName Blockchain, b.LogoLocation BlockchainLogo
FROM mynodes mn
JOIN users u ON u.ID = mn.UserID
JOIN otidentity i ON i.NodeId = mn.NodeID
JOIN otoffer_holders h ON h.Holder = i.Identity AND h.BlockchainID = i.BlockchainID
JOIN otoffer o ON o.OfferID = h.OfferID AND o.BlockchainID = i.BlockchainID
JOIN blockchains b ON b.ID = o.BlockchainID
LEFT JOIN ticker_trac ticker ON u.USDPriceCalculationMode = 0 AND ticker.Timestamp = (
SELECT MAX(TIMESTAMP)
FROM ticker_trac
WHERE TIMESTAMP <= o.FinalizedTimestamp)
WHERE o.FinalizedTimestamp >= DATE_Add(DATE(NOW()), INTERVAL -7 DAY) AND mn.UserID = @userID AND (@nodeID IS NULL OR @nodeID = mn.NodeID)
ORDER BY o.FinalizedTimestamp DESC", new
                {
                    userID = User.Identity.Name,
                    overrideUSDPrice = ticker?.PriceUsd ?? 0,
                    nodeID = nodeID
                }, commandTimeout: 120)).ToArray();

                List<RecentJobsByDay> days = new List<RecentJobsByDay>(7);
                DateTime date = DateTime.Now.Date;

                for (int i = 1; i <= 7; i++)
                {
                    RecentJobsByDay day = new RecentJobsByDay();
                    day.Active = i == 1;
                    day.Day = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName(date.DayOfWeek);
                    day.Jobs = jobs.Where(j => j.FinalizedTimestamp.Date == date).ToArray();
                    days.Add(day);
                    date = date.AddDays(-1);
                }

                var data = days.ToArray();




                _cache.Set($"MyNodes-GetRecentJobs-{User.Identity.Name}-{nodeID}", data, TimeSpan.FromSeconds(15));



                return data;
            }
        }

        [HttpGet]
        [Authorize]
        [Route("PossibleJobPayouts")]
        [SwaggerOperation(Description = "Requires authentication to use.")]
        public async Task<PossiblePayoutModel[]> GetPossibleJobPayouts([FromQuery] bool includeActiveJobs, [FromQuery] bool includeCompletedJobs,
            [FromQuery] int blockchainID, [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo)
        {
            await using (MySqlConnection connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {

                string nodeUrl = await connection.ExecuteScalarAsync<string>(@"SELECT BlockchainNodeUrl FROM blockchains WHERE id = @id", new
                {
                    id = blockchainID
                });

                var cl = new Web3(nodeUrl);

                var eth = new EthApiService(cl.Client);

                var latestBlockParam = BlockParameter.CreateLatest();

                var block = await cl.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(latestBlockParam);

                var rows = (await connection.QueryAsync<PossiblePayoutModel>(@"WITH CTEJobs AS (
SELECT 
b.ID BlockchainID, b.DisplayName BlockchainName, o.OfferID, MN.NodeID, i.Identity, o.TokenAmountPerHolder TokenAmount,
DATE_Add(o.FinalizedTimeStamp, INTERVAL + o.HoldingTimeInMinutes MINUTE) as JobEndTimestamp, of.Timestamp FinalizedTimestamp,
o.TokenAmountPerHolder * 1000000000000000000 TokenAmountInWei, o.HoldingTimeInMinutes
FROM
mynodes MN
JOIN otidentity i ON i.NodeId = MN.NodeID
JOIN otoffer_holders h ON h.Holder = i.Identity AND h.BlockchainID = i.BlockchainID
JOIN otoffer o ON o.OfferID = h.OfferID AND o.BlockchainID = h.BlockchainID
JOIN blockchains b ON b.id = o.BlockchainID
JOIN otcontract_holding_offerfinalized of ON of.OfferID = o.OfferID AND of.BlockchainID = o.BlockchainID
WHERE o.IsFinalized = 1 AND MN.UserID = @userID AND (@blockchainID IS NULL OR i.BlockchainID = @blockchainID)
AND ((@includeActiveJobs = 1 AND DATE_Add(o.FinalizedTimeStamp, INTERVAL + o.HoldingTimeInMinutes MINUTE) >= NOW())
OR (@includeCompletedJobs = 1 AND DATE_Add(o.FinalizedTimeStamp, INTERVAL + o.HoldingTimeInMinutes MINUTE) <= NOW()))
AND (@dateFrom IS NULL OR DATE_Add(o.FinalizedTimeStamp, INTERVAL + o.HoldingTimeInMinutes MINUTE) >= @dateFrom)
AND (@dateTo IS NULL OR DATE_Add(o.FinalizedTimeStamp, INTERVAL + o.HoldingTimeInMinutes MINUTE) <= @dateTo)
),

CTEPayouts AS (
SELECT MN.NodeID, po.OfferID, SUM(po.Amount) PaidAmount, COUNT(po.OfferId) PayoutCount, MAX(po.Timestamp) LastPayout
FROM otcontract_holding_paidout po
JOIN otidentity i ON i.Identity = po.Holder AND i.BlockchainID = po.BlockchainID
JOIN mynodes MN ON MN.NodeID = i.NodeId
WHERE MN.UserID = @userID AND (@blockchainID IS NULL OR po.BlockchainID = @blockchainID)
GROUP BY MN.NodeID, po.OfferID
)

SELECT 
J.BlockchainID, J.BlockchainName, J.OfferID, J.NodeID, J.Identity, J.TokenAmount, COALESCE(P.PaidAmount, 0) PaidAmount, 
COALESCE(P.PayoutCount, 0) PayoutCount, P.LastPayout, J.JobEndTimestamp,
((J.TokenAmountInWei * (LEAST(UNIX_TIMESTAMP(J.JobEndTimestamp), @currentBlockUnixTimestamp) - UNIX_TIMESTAMP(GREATEST(COALESCE(P.LastPayout, 0), J.FinalizedTimestamp)))) / (J.HoldingTimeInMinutes * 60)) / 1000000000000000000 EstimatedPayout
FROM CTEJobs J
LEFT JOIN CTEPayouts P ON P.OfferID = J.OfferID
WHERE J.TokenAmount != COALESCE(P.PaidAmount, 0)", new
                {
                    userID = User.Identity.Name,
                    includeActiveJobs = includeActiveJobs,
                    includeCompletedJobs = includeCompletedJobs,
                    blockchainID = blockchainID,
                    dateFrom = dateFrom,
                    dateTo = dateTo,
                    currentBlockUnixTimestamp = (ulong)block.Timestamp.Value
                }, commandTimeout: 120)).ToArray();

                return rows;
            }
        }

        [HttpGet]
        [Authorize]
        [Route("GetNodeStats")]
        [SwaggerOperation(Description = "Requires authentication to use.")]
        public async Task<NodeStats> GetNodeStats([FromQuery]string nodeID)
        {
            TickerInfo ticker = await TickerHelper.GetTickerInfo(_cache);

            await using (MySqlConnection connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                using (var multi = await connection.QueryMultipleAsync(@"
SET @priceMode = (SELECT u.USDPriceCalculationMode FROM users u WHERE u.ID = @userID);

CREATE TEMPORARY TABLE JobsCTELocal (SELECT 
i.NodeId, 
SUM(o.TokenAmountPerHolder) AS TokenAmount,
SUM((CASE WHEN @priceMode = 0 THEN ticker.Price ELSE @overrideUSDPrice END) * o.TokenAmountPerHolder) AS USDAmount,
COUNT(o.OfferID) AS JobCount,
CASE WHEN mn.ID IS NOT NULL THEN 1 ELSE 0 END AS IsMyNode
FROM otoffer o
JOIN otoffer_holders h ON h.OfferID = o.OfferID AND h.BlockchainID = o.BlockchainID
JOIN otidentity i ON i.Identity = h.Holder AND i.BlockchainID = o.BlockchainID
LEFT JOIN mynodes mn ON mn.UserID = @userID AND mn.NodeID = i.NodeId
LEFT JOIN ticker_trac ticker ON @priceMode = 0 AND (i.NodeId = @nodeID OR mn.ID IS NOT null) AND ticker.Timestamp = (
SELECT MAX(TIMESTAMP)
FROM ticker_trac
WHERE TIMESTAMP <= o.FinalizedTimestamp)
WHERE i.Version > 0 AND i.NodeId != '0000000000000000000000000000000000000000'
GROUP BY i.NodeId);


SELECT * FROM (
SELECT l.NodeId
, l.IsMyNode
,SUM(l.TokenAmount) RewardTokenAmount
,SUM(l.USDAmount) RewardUSDAmount
,  ROUND(PERCENT_RANK() OVER (
          ORDER BY SUM(l.TokenAmount)
       ) * 100 ) RewardTokenAmountRank
,SUM(l.JobCount) JobCount
,  ROUND(PERCENT_RANK() OVER (
          ORDER BY SUM(l.JobCount)
       ) * 100)  JobCountRank
FROM JobsCTELocal l
GROUP BY l.NodeId) X
WHERE (@nodeID IS NULL AND X.IsMyNode = 1) OR X.NodeId = @nodeID
;

SELECT * FROM (
SELECT 
i.NodeId
,SUM(i.Stake) StakeTokenAmount
,SUM(i.Stake) * @overrideUSDPrice StakeUSDAmount
,  ROUND(PERCENT_RANK() OVER (
          ORDER BY SUM(i.Stake)
       ) * 100)  StakeTokenAmountRank
,SUM(i.StakeReserved) StakeReservedTokenAmount
,SUM(i.StakeReserved) * @overrideUSDPrice StakeReservedUSDAmount
,  ROUND(PERCENT_RANK() OVER (
          ORDER BY SUM(i.StakeReserved)
       ) * 100) StakeReservedTokenAmountRank
FROM otidentity i
WHERE i.NodeId IN (SELECT j.NodeID FROM JobsCTELocal j) AND i.Version > 0 AND i.NodeId != '0000000000000000000000000000000000000000'
GROUP BY i.NodeId) X
WHERE (@nodeID IS NULL AND X.NodeId IN (SELECT j.NodeID FROM JobsCTELocal j WHERE j.IsMyNode = 1)) OR X.NodeId = @nodeID
", new
                {
                    userID = User.Identity.Name,
                    overrideUSDPrice = ticker?.PriceUsd ?? 0,
                    nodeID
                }, commandTimeout:120))
                {
                    NodeStatsModel1 model1;
                    NodeStatsModel2 model2;

                    if (!String.IsNullOrWhiteSpace(nodeID))
                    {
                        model1 = multi.ReadFirstOrDefault<NodeStatsModel1>();
                        model2 = multi.ReadFirstOrDefault<NodeStatsModel2>();

                        if (model1 == null || model2 == null)
                            return null;
                    }
                    else
                    {
                        var model1rows = multi.Read<NodeStatsModel1>().ToArray();
                        model1 = new NodeStatsModel1
                        {
                            JobCount = model1rows.Sum(r => r.JobCount),
                            RewardTokenAmount = model1rows.Sum(r => r.RewardTokenAmount),
                            RewardUSDAmount = model1rows.Sum(r => r.RewardUSDAmount)
                        };
                        var model2rows = multi.Read<NodeStatsModel2>().ToArray();
                        model2 = new NodeStatsModel2
                        {
                            StakeReservedTokenAmount = model2rows.Sum(r => r.StakeReservedTokenAmount),
                            StakeReservedUSDAmount = model2rows.Sum(r => r.StakeReservedUSDAmount),
                            StakeTokenAmount = model2rows.Sum(r => r.StakeTokenAmount),
                            StakeUSDAmount = model2rows.Sum(r => r.StakeUSDAmount)
                        };
                    }


                    NodeStats stats = new NodeStats
                    {
                        TotalJobs = new NodeStats.NodeStatsNumeric
                        {
                            Value = model1.JobCount,
                            BetterThanActiveNodesPercentage = model1.JobCountRank
                        },
                        TotalLocked =
                            new NodeStats.NodeStatsToken
                            {
                                TokenAmount = model2.StakeReservedTokenAmount,
                                USDAmount = model2.StakeReservedUSDAmount,
                                BetterThanActiveNodesPercentage = model2.StakeReservedTokenAmountRank
                            },
                        TotalRewards =
                            new NodeStats.NodeStatsToken
                            {
                                TokenAmount = model1.RewardTokenAmount, 
                                USDAmount = model1.RewardUSDAmount,
                                BetterThanActiveNodesPercentage = model1.RewardTokenAmountRank
                            },
                        TotalStaked = new NodeStats.NodeStatsToken
                        {
                            TokenAmount = model2.StakeTokenAmount, 
                            USDAmount = model2.StakeUSDAmount,
                            BetterThanActiveNodesPercentage = model2.StakeTokenAmountRank
                        }
                    };
                    return stats;
                }
            }
        }

        [HttpGet]
        [Authorize]
        [Route("JobsPerMonth")]
        [SwaggerOperation(Description = "Requires authentication to use.")]
        public async Task<NodesPerYearMonthResponse> GetJobsPerMonth()
        {
            if (_cache.TryGetValue("MyNodes-JobsPerMonth-" + User.Identity.Name, out var cached))
            {
                return (NodesPerYearMonthResponse)cached;
            }

            TickerInfo ticker = await TickerHelper.GetTickerInfo(_cache);

            NodesPerYearMonthResponse response = new NodesPerYearMonthResponse();

            await using (MySqlConnection connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                JobsPerMonthModel[] data = (await connection.QueryAsync<JobsPerMonthModel>(@"WITH JobsCTE AS (
SELECT 
mn.DisplayName,
i.NodeId, 
YEAR(o.FinalizedTimestamp) AS 'Year', 
MONTH(o.FinalizedTimestamp) AS 'Month',
SUM(o.TokenAmountPerHolder) AS TokenAmount,
COUNT(o.OfferID) AS JobCount,
SUM((CASE WHEN u.USDPriceCalculationMode = 0 THEN ticker.Price ELSE @overrideUSDPrice END) * o.TokenAmountPerHolder) AS USDAmount
FROM otoffer o
JOIN otoffer_holders h ON h.OfferID = o.OfferID AND h.BlockchainID = o.BlockchainID
JOIN otidentity i ON i.Identity = h.Holder AND i.BlockchainID = o.BlockchainID
JOIN mynodes mn ON mn.NodeID = i.NodeId
JOIN users u ON u.ID = mn.UserID
LEFT JOIN ticker_trac ticker ON u.USDPriceCalculationMode = 0 AND ticker.Timestamp = (
SELECT MAX(TIMESTAMP)
FROM ticker_trac
WHERE TIMESTAMP <= o.FinalizedTimestamp)
WHERE mn.UserID = @userID
GROUP BY i.NodeId, YEAR(o.FinalizedTimestamp), MONTH(o.FinalizedTimestamp)
)

SELECT JobsCTE.DisplayName, JobsCTE.NodeId, JobsCTE.Year, JobsCTE.Month, JobsCTE.TokenAmount, JobsCTE.JobCount, JobsCTE.USDAmount
FROM JobsCTE
ORDER BY JobsCTE.DisplayName, JobsCTE.NodeID, JobsCTE.Year, JobsCTE.Month", new
                {
                    userID = User.Identity.Name,
                    overrideUSDPrice = ticker?.PriceUsd ?? 0
                }, commandTimeout: 120)).ToArray();

                IEnumerable<IGrouping<string, JobsPerMonthModel>> groupedByNodes = data.GroupBy(m => m.NodeId);

                foreach (IGrouping<string, JobsPerMonthModel> nodeGroup in groupedByNodes)
                {
                    List<JobsPerYear> years = new List<JobsPerYear>();

                    IEnumerable<IGrouping<int, JobsPerMonthModel>> groupedByYears =
                        nodeGroup.GroupBy(g => g.Year).OrderBy(g => g.Key);

                    JobsPerMonth lastMonth = null;

                    foreach (IGrouping<int, JobsPerMonthModel> yearGroup in groupedByYears)
                    {
                        Dictionary<int, JobsPerMonthModel> months = yearGroup
                            .ToDictionary(k => k.Month, v => v);

                        JobsPerYear year = new JobsPerYear
                        {
                            Year = yearGroup.Key.ToString(),
                            Active = yearGroup.Key == DateTime.Now.Year
                        };

                        for (int i = 1; i <= 12; i++)
                        {
                            JobsPerMonth month;

                            if (months.TryGetValue(i, out JobsPerMonthModel monthData))
                            {
                                month = new JobsPerMonth(monthData);
                            }
                            else
                            {
                                month = new JobsPerMonth
                                {
                                    Month = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(i)
                                };
                            }

                            year.Months.Add(month);

                            if (lastMonth != null)
                            {
                                if (lastMonth.JobCount > month.JobCount || (lastMonth.JobCount == 0 && month.JobCount == 0))
                                {
                                    month.Down = true;
                                }
                            }
                            else
                            {
                                month.Down = true;
                            }

                            lastMonth = month;
                        }

                        years.Add(year);
                    }

                    response.Nodes.Add(new NodeJobsPerYear
                    {
                        NodeId = nodeGroup.Key,
                        DisplayName = nodeGroup.First().DisplayName,
                        Years = years
                    });
                }
            }

            JobsPerYear[] allYears = response.Nodes.SelectMany(n => n.Years).ToArray();

            IEnumerable<IGrouping<string, JobsPerYear>> allGroupedByYear = allYears.GroupBy(a => a.Year);

            response.AllNodes = new NodeJobsPerYear
            {
                DisplayName = "All Nodes",
                Years = allGroupedByYear.Select(y => new JobsPerYear
                {
                    Year = y.Key,
                    Active = y.First().Active,
                    Months = y.SelectMany(d => d.Months)
                        .GroupBy(m => m.Month)
                        .Select(m => new JobsPerMonth
                        {
                            Month = m.Key,
                            JobCount = m.Sum(d => d.JobCount),
                            TokenAmount = m.Sum(d => d.TokenAmount),
                            USDAmount = m.Sum(d => d.USDAmount)
                        })
                        .ToList()
                }).OrderBy(y => y.Year).ToList()
            };

            JobsPerMonth previousMonth = null;

            foreach (JobsPerYear year in response.AllNodes.Years)
            {
                foreach (JobsPerMonth month in year.Months)
                {
                    if (previousMonth == null)
                    {
                        month.Down = true;
                    }
                    else
                    {
                        if (previousMonth.JobCount == 0 && month.JobCount == 0)
                        {
                            month.Down = true;
                        }
                        else if (previousMonth.JobCount > month.JobCount)
                        {
                            month.Down = true;
                        }
                    }

                    previousMonth = month;
                }
            }



            _cache.Set("MyNodes-JobsPerMonth-" + User.Identity.Name, response, TimeSpan.FromSeconds(15));



            return response;
        }

        [HttpPost]
        [Authorize]
        [Route("ImportNodes")]
        [SwaggerOperation(Description = "Requires authentication to use.")]
        public async Task ImportNodes([FromQuery] string identities)
        {
            string[] split = identities.Split(";").Where(t => t.StartsWith("0x") && t.Length <= 100).ToArray();

            IEnumerable<string> nodeIds;

            await using (MySqlConnection connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                nodeIds = await connection.QueryAsync<string>(
                    @"SELECT NodeID FROM OTIdentity WHERE Identity in @identities", new
                    {
                        identities = split
                    });
            }

            foreach (var nodeId in nodeIds.Distinct())
            {
                await AddEditNode(nodeId, null);
            }
        }

        [HttpPost]
        [Authorize]
        [Route("AddEditNode")]
        [SwaggerOperation(Description = "Requires authentication to use.")]
        public async Task AddEditNode([FromQuery] string nodeID, [FromQuery] string name)
        {
            if (name != null && name.Length > 200)
            {
                name = name.Substring(0, 200);
            }

            await using (MySqlConnection connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                bool exists = await connection.ExecuteScalarAsync<bool>(@"SELECT 1 FROM MyNodes WHERE UserID = @userID AND NodeID = @nodeID", new
                {
                    userID = User.Identity.Name, nodeID
                });

                if (!exists)
                {
                    await connection.ExecuteAsync("INSERT INTO MyNodes (UserID, NodeID, DisplayName) VALUES (@userID, @nodeID, @name)", new
                    {
                        userID = User.Identity.Name,
                        nodeID,
                        name
                    });
                }
                else
                {
                    await connection.ExecuteAsync("UPDATE MyNodes SET DisplayName = @name WHERE UserID = @userID AND NodeID = @nodeID", new
                    {
                        userID = User.Identity.Name,
                        nodeID,
                        name
                    });
                }
            }
        }

        [HttpDelete]
        [Authorize]
        [Route("DeleteNode")]
        [SwaggerOperation(Description = "Requires authentication to use.")]
        public async Task DeleteNode([FromQuery]string nodeID)
        {
            await using (MySqlConnection connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                bool exists = await connection.ExecuteScalarAsync<bool>(
                    @"SELECT 1 FROM MyNodes WHERE UserID = @userID AND NodeID = @nodeID", new
                    {
                        userID = User.Identity.Name, nodeID
                    });

                if (exists)
                {
                    await connection.ExecuteAsync("DELETE FROM MyNodes WHERE UserID = @userID AND NodeID = @nodeID", new
                    {
                        userID = User.Identity.Name, nodeID
                    });
                }
            }
        }
    }

    public class JobsPerMonthModel
    { 
        public string DisplayName { get; set; }
        public string NodeId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TokenAmount { get; set; }
        public int JobCount { get; set; }
        public decimal USDAmount { get; set; }
    }

    public class NodesPerYearMonthResponse
    {
        public NodeJobsPerYear AllNodes { get; set; }
        public List<NodeJobsPerYear> Nodes { get; } = new List<NodeJobsPerYear>();
    }


    public class NodeJobsPerYear
    {
        public string DisplayName { get; set; }
        public string NodeId { get; set; }
        public List<JobsPerYear> Years { get; set; }
    }

    public class JobsPerYear
    {
        public bool Active { get; set; }
        public string Year { get; set; }

        public List<JobsPerMonth> Months { get; set; } = new List<JobsPerMonth>();
    }


    public class JobsPerMonth
    {
        public JobsPerMonth()
        {
            
        }
        public JobsPerMonth(JobsPerMonthModel model)
        {
            Month = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(model.Month);
            TokenAmount = model.TokenAmount;
            JobCount = model.JobCount;
            USDAmount = model.USDAmount;
        }

        public string Month { get; set; }
        public decimal TokenAmount { get; set; }
        public int JobCount { get; set; }
        public decimal USDAmount { get; set; }
        public bool Down { get; set; }
    }

    public class RecentJobsByDay
    {
        public string Day { get; set; }
        public RecentJobs[] Jobs { get; set; }
        public bool Active { get; set; }
    }

    public class RecentJobs
    {
        public string NodeId { get; set; }
        public string DisplayName { get; set; }
        public string OfferID { get; set; }
        public int HoldingTimeInMinutes { get; set; }
        public decimal TokenAmountPerHolder { get; set; }
        public DateTime FinalizedTimestamp { get; set; }
        public decimal USDAmount { get; set; }
        public string Blockchain { get; set; }
        public string BlockchainLogo { get; set; }
    }

    internal class NodeStatsModel1
    {
        public int JobCount { get; set; }
        public int JobCountRank { get; set; }
        public decimal RewardTokenAmount { get; set; }
        public int RewardTokenAmountRank { get; set; }
        public decimal RewardUSDAmount { get; set; }
    }

    internal class NodeStatsModel2
    {
        public decimal StakeTokenAmount { get; set; }
        public int StakeTokenAmountRank { get; set; }
        public decimal StakeUSDAmount { get; set; }
        public decimal StakeReservedTokenAmount { get; set; }
        public decimal StakeReservedUSDAmount { get; set; }
        public int StakeReservedTokenAmountRank { get; set; }
    }

    public class NodeStats
    {
        public NodeStatsNumeric TotalJobs { get; set; }
        public NodeStatsToken TotalRewards { get; set; }
        public NodeStatsToken TotalStaked { get; set; }
        public NodeStatsToken TotalLocked { get; set; }

        public class NodeStatsToken
        {
            public decimal TokenAmount { get; set; }
            public decimal USDAmount { get; set; }
            public int? BetterThanActiveNodesPercentage { get; set; }
        }
        public class NodeStatsNumeric
        {
            public int Value { get; set; }

            public int? BetterThanActiveNodesPercentage { get; set; }
        }
    }

    public class GetHoldingTimeByMonthModel
    {
        public int HoldingTimeInMonths { get; set; }
        public int Count { get; set; }
    }

    public class PossiblePayoutModel
    {
        public int BlockchainID { get; set; }
        public string BlockchainName { get; set; }
        public string OfferID { get; set; }
        public string NodeID { get; set; }
        public string Identity { get; set; }
        public decimal TokenAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public DateTime? LastPayout { get; set; }
        public DateTime JobEndTimestamp { get; set; }
        public decimal EstimatedPayout { get; set; }

    }
}