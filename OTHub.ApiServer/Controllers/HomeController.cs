using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoinpaprikaAPI.Entity;
using CoinpaprikaAPI.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MySqlConnector;
using OTHub.APIServer.Sql.Models.Home;
using OTHub.Settings;
using Swashbuckle.AspNetCore.Annotations;

namespace OTHub.APIServer.Controllers
{
    [Route("api/[controller]")]
    public class HomeController : Controller
    {
        private readonly IMemoryCache _cache;

        public HomeController(IMemoryCache cache)
        {
            _cache = cache;
        }

        [HttpGet]
        [Route(("HomeV3"))]
        public async Task<HomeV3Model> HomeV3()
        {
            if (_cache.TryGetValue("HomeV3", out object homeModel))
            {
                return (HomeV3Model) homeModel;
            }

            CoinpaprikaAPI.Client client = new CoinpaprikaAPI.Client();

            TickerInfo tickerInfo = null;

            if (!_cache.TryGetValue("HomeV3Ticker", out object tickerModel))
            {
                tickerModel = (await client.GetTickerForIdAsync(@"trac-origintrail")).Value;

                _cache.Set("HomeV3Ticker", tickerModel, TimeSpan.FromMinutes(5));
            }

            tickerInfo = (TickerInfo)tickerModel;

            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var summary = await connection.QuerySingleAsync<HomeV3Model>(@"SELECT
 (
SELECT COUNT(DISTINCT I.NodeId)
FROM OTOffer O
JOIN OTOffer_Holders H ON H.OfferID = O.OfferId
JOIN otidentity I ON I.Identity = H.Holder
WHERE O.IsFinalized = 1 AND NOW() <= DATE_ADD(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE)) ActiveNodes,
(
SELECT COUNT(*)
FROM otoffer
WHERE otoffer.IsFinalized = 1
) TotalJobs,
(
SELECT SUM(CASE WHEN IsFinalized = 1 AND NOW() <= DATE_ADD(FinalizedTimeStamp, INTERVAL +HoldingTimeInMinutes MINUTE) THEN 1 ELSE 0 END)
FROM OTOffer) AS ActiveJobs,
(select sum(Stake) from otidentity where version = (select max(ii.version) from otidentity ii)) StakedTokens,
(SELECT COUNT(*) FROM OTOffer WHERE IsFinalized = 1 AND CreatedTimeStamp >= DATE_Add(NOW(), INTERVAL -1 DAY)) AS Jobs24H,
(SELECT AVG(otoffer.TokenAmountPerHolder) FROM OTOffer WHERE IsFinalized = 1 AND CreatedTimeStamp >= DATE_Add(NOW(), INTERVAL -1 DAY)) AS JobsReward24H,
(SELECT AVG(otoffer.HoldingTimeInMinutes) FROM OTOffer WHERE IsFinalized = 1 AND CreatedTimeStamp >= DATE_Add(NOW(), INTERVAL -1 DAY)) AS JobsDuration24H,
(SELECT AVG(otoffer.DataSetSizeInBytes) FROM OTOffer WHERE IsFinalized = 1 AND CreatedTimeStamp >= DATE_Add(NOW(), INTERVAL -1 DAY)) AS JobsSize24H");



                summary.FeesByBlockchain = (await connection.QueryAsync<HomeFeesModel>(@"SELECT 
bc.BlockchainName, bc.NetworkName,
bc.ShowCostInUSD,
CAST(AVG((CAST(oc.GasUsed AS DECIMAL(20,4)) * (oc.GasPrice / 1000000000000000000)) * (CASE WHEN bc.ShowCostInUSD THEN ocTicker.Price ELSE 1 END)) AS DECIMAL(20,6)) JobCreationCost, 
CAST(AVG((CAST(of.GasUsed AS DECIMAL(20,4)) * (of.GasPrice / 1000000000000000000)) * (CASE WHEN bc.ShowCostInUSD THEN ofTicker.Price ELSE 1 END)) AS DECIMAL(20,6)) JobFinalisedCost
FROM blockchains bc
LEFT JOIN otcontract_holding_offercreated oc ON bc.ID = oc.BlockchainID AND oc.Timestamp >= DATE_Add(NOW(), INTERVAL -1 DAY)
LEFT JOIN otcontract_holding_offerfinalized of ON of.OfferID = oc.OfferID AND of.BlockchainID = oc.BlockchainID AND of.Timestamp >= DATE_Add(NOW(), INTERVAL -1 DAY)
LEFT JOIN ticker_trac ocTicker ON ocTicker.Timestamp = (
SELECT MAX(TIMESTAMP)
FROM ticker_trac
WHERE TIMESTAMP <= oc.Timestamp)
LEFT JOIN ticker_trac ofTicker ON ofTicker.Timestamp = (
SELECT MAX(TIMESTAMP)
FROM ticker_trac
WHERE TIMESTAMP <= of.Timestamp)
GROUP BY bc.id")).ToArray();

                var payoutsCosts = (await connection.QueryAsync<HomeFeesModel>(@"SELECT 
bc.DisplayName BlockchainName,
bc.ShowCostInUSD,
CAST(AVG((CAST(po.GasUsed AS DECIMAL(20,4)) * (po.GasPrice / 1000000000000000000)) * (CASE WHEN bc.ShowCostInUSD THEN ocTicker.Price ELSE 1 END)) AS DECIMAL(20, 8)) PayoutCost
FROM blockchains bc
LEFT JOIN otcontract_holding_paidout po ON po.BlockchainID = bc.ID AND po.Timestamp >= DATE_Add(NOW(), INTERVAL -1 DAY)
LEFT JOIN ticker_trac ocTicker ON ocTicker.Timestamp = (
SELECT MAX(TIMESTAMP)
FROM ticker_trac
WHERE TIMESTAMP <= po.Timestamp)
GROUP BY bc.id")).ToArray();

                //TODO move these 2 SQL queries back into one at some point
                foreach (var homeFeesModel in payoutsCosts)
                {
                    var model = summary.FeesByBlockchain.FirstOrDefault(b =>
                        b.BlockchainName == homeFeesModel.BlockchainName);
                    if (model != null)
                    {
                        model.PayoutCost = homeFeesModel.PayoutCost;
                    }
                }

                summary.StakedByBlockchain = (await connection.QueryAsync<HomeStakedModel>(@"SELECT bc.DisplayName BlockchainName, SUM(i.Stake) StakedTokens
FROM otidentity i
JOIN blockchains bc ON bc.ID = i.BlockchainID
WHERE i.version = (
SELECT MAX(ii.version)
FROM otidentity ii)
GROUP BY bc.ID")).ToArray();

                summary.TotalJobsByBlockchain = (await connection.QueryAsync<HomeJobsModel>(@"SELECT b.DisplayName BlockchainName, COUNT(O.OfferID) Jobs
FROM otoffer o
JOIN blockchains b ON b.id = o.BlockchainID
WHERE o.IsFinalized = 1
GROUP BY b.ID")).ToArray();

                summary.Jobs24HByBlockchain = (await connection.QueryAsync<HomeJobsModel>(@"SELECT b.DisplayName BlockchainName, COUNT(O.OfferID) Jobs
FROM blockchains b
LEFT JOIN otoffer o ON b.id = o.BlockchainID AND o.IsFinalized = 1 AND o.CreatedTimeStamp >= DATE_Add(NOW(), INTERVAL -1 DAY)
GROUP BY b.ID
ORDER BY b.ID")).ToArray();

                summary.PriceUsd = tickerInfo.PriceUsd;
                summary.PercentChange24H = tickerInfo.PercentChange24H;
                summary.CirculatingSupply = tickerInfo.CirculatingSupply;

                _cache.Set("HomeV3", summary, TimeSpan.FromMinutes(5));


                return summary;
            }
        }

        [HttpGet]
        [Route("24HJobBlockchainDistribution")]
        public async Task<HomeJobBlockchainDistributionModel[]> GetHome24HJobBlockchainDistributionModel()
        {
            if (_cache.TryGetValue("24HJobBlockchainDistribution", out object model))
            {
                return (HomeJobBlockchainDistributionModel[])model;
            }

            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var data = (await connection.QueryAsync<HomeJobBlockchainDistributionModel>(
                    @"SET @totalToday = (SELECT COUNT(*) AS total FROM otoffer oo WHERE oo.IsFinalized = 1 AND oo.FinalizedTimestamp >= DATE_Add(NOW(), INTERVAL -1 DAY));

SELECT bc.DisplayName, 
bc.Color, 
COUNT(o.OfferID) Jobs,
ROUND(COUNT(*) / (@totalToday) * 100) AS Percentage
FROM blockchains bc
LEFT JOIN otoffer o ON bc.ID = o.BlockchainID AND o.IsFinalized = 1 AND o.FinalizedTimestamp >= DATE_Add(NOW(), INTERVAL -1 DAY)
GROUP BY bc.Id
ORDER BY Percentage")).ToArray();

                _cache.Set("24HJobBlockchainDistribution", data, TimeSpan.FromHours(6));

                return data;
            }
        }
      

        [HttpGet]
        [Route("JobsChartDataSummaryV2")]
        public async Task<JobChartDataV2SummaryModel> JobsChartDataSummaryV2()
        {
            if (_cache.TryGetValue("JobsChartDataSummaryV2", out var model) && model is JobChartDataV2SummaryModel chartModel)
                return chartModel;

            await using (var connection =
            new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var summary = await connection.QuerySingleAsync<JobChartDataV2SummaryModel>(@"SELECT
    (SELECT COUNT(*) FROM OTOffer WHERE IsFinalized = 1 AND CreatedTimeStamp >= DATE_Add(NOW(), INTERVAL -1 DAY)) as OffersLast24Hours,
    (SELECT COUNT(*) FROM OTOffer WHERE IsFinalized = 1 AND CreatedTimeStamp >= DATE_Add(NOW(), INTERVAL - 7 DAY)) as OffersLast7Days,
        (SELECT COUNT(*) FROM OTOffer WHERE IsFinalized = 1 AND CreatedTimeStamp >= DATE_Add(NOW(), INTERVAL - 1 MONTH)) as OffersLastMonth,
	(SELECT SUM(CASE WHEN IsFinalized = 1 AND NOW() <= DATE_Add(FinalizedTimeStamp, INTERVAL +HoldingTimeInMinutes MINUTE) THEN 1 ELSE 0 END)
	 FROM OTOffer) as OffersActive");

                _cache.Set("JobsChartDataSummaryV2", summary, TimeSpan.FromMinutes(30));

                return summary;
            }
        }

        [HttpGet]
        [Route("HomeNodesInfoV2")]
        public async Task<HomeNodesInfo> HomeNodesInfoV2()
        {
            if (_cache.TryGetValue("HomeNodesInfoV2", out var model) && model is HomeNodesInfo chartModel)
                return chartModel;

            await using (var connection =
    new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                //	(SELECT count(distinct nodeId) as OnlineNodesCount FROM otnode_history WHERE TimeStamp >= DATE_Add(NOW(), INTERVAL -1 DAY) AND Success = 1) as OnlineNodesCount,
                //  ((SELECT count(distinct nodeId) FROM OTContract_Approval_NodeApproved) -(SELECT count(distinct nodeId) FROM OTContract_Approval_NodeRemoved)) as ApprovedNodesCount,

                //    (SELECT count(distinct h.nodeId) as OnlineNodesCount FROM otnode_history h join otnode_ipinfov2 i on i.nodeid = h.nodeid WHERE h.TimeStamp >= DATE_Add(NOW(), INTERVAL -3 HOUR) AND h.Success = 1) OnlineNodesCount,
                var homeInfo = await connection.QuerySingleAsync<HomeNodesInfo>($@"SELECT 
    0 as OnlineNodesCount,
    (SELECT count(distinct H.Holder) FROM OTOffer_Holders H JOIN OTOffer O on O.OfferID = H.OfferID WHERE O.IsFinalized = 1 AND O.FinalizedTimeStamp >= DATE_Add(NOW(), INTERVAL -7 DAY)) NodesWithJobsThisWeek,
    (SELECT count(distinct H.Holder) FROM OTOffer_Holders H JOIN OTOffer O on O.OfferID = H.OfferID WHERE O.IsFinalized = 1 AND O.FinalizedTimeStamp >= DATE_Add(NOW(), INTERVAL -1 MONTH)) NodesWithJobsThisMonth,
    (SELECT COUNT(distinct H.Holder) FROM OTOffer O JOIN OTOffer_Holders H ON H.OfferID = O.OfferId WHERE O.IsFinalized = 1 AND NOW() <= DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE)) NodesWithActiveJobs");

                _cache.Set("HomeNodesInfoV2", homeInfo, TimeSpan.FromMinutes(10));

                return homeInfo;
            }
        }

        [HttpGet]
        [Route("JobsChartDataV3")]
        public async Task<JobsChartDataV2Model[]> JobsChartDataV3()
        {
            //if (_cache.TryGetValue("JobsChartDataV3", out var model) && model is JobsChartDataV2 chartModel)
            //    return chartModel;

            //var response = new JobsChartDataV2();

            await using (var connection =
               new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var data = (await connection.QueryAsync<JobsChartDataV2Model>(@"SELECT * FROM jobhistorybyday")).ToArray();

                return data;

                //response.Data = new int[][]
                //{
                //    data.Select(d => d.CompletedJobs).ToArray(),
                //    data.Select(d => d.NewJobs).ToArray()
                //};

                //response.Labels = data.Select(d => d.Label).ToArray();


                //_cache.Set("JobsChartDataV3", response, TimeSpan.FromMinutes(10));

                //return response;
            }
        }

        [HttpGet]
        [Route("NodesChartDataV2")]
        public NodesChartDataV2 NodesChartDataV2()
        {
            if (_cache.TryGetValue("NodesChartDataV2", out var model) && model is NodesChartDataV2 chartModel)
                return chartModel;

            var response = new NodesChartDataV2();

            //(SELECT COUNT(DISTINCT h.NodeId) FROM otnode_history h WHERE x.Date = DATE(h.Timestamp) AND h.Success = 1) OnlineNodes,

            using (var connection =
               new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var data = connection.Query<HomeNodesChartDataModel>(@"SELECT 
x.Date,
DAYNAME(x.Date) Label,
(SELECT COUNT(distinct H.Holder) FROM OTOffer O JOIN OTOffer_Holders H ON H.OfferID = O.OfferId WHERE O.IsFinalized = 1 AND x.Date <= DATE(DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE))) NodesWithActiveJobs
FROM (
SELECT CURDATE() Date
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 1 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 2 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 3 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 4 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 5 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 6 DAY)
) x 
GROUP BY x.Date").ToArray();

                response.Week = new int[][]
                {
                    data.Select(d => d.NodesWithActiveJobs).ToArray()
                };

                response.WeekLabels = data.Select(d => d.Label).ToArray();

                _cache.Set("NodesChartDataV2", response, TimeSpan.FromMinutes(30));

                return response;
            }
        }
    }
}