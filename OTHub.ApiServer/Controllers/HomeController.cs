
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoinMarketCap;
using CoinMarketCap.Models.Cryptocurrency;
using CoinpaprikaAPI.Entity;
using CoinpaprikaAPI.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MySqlConnector;
using OTHub.APIServer.Helpers;
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

            TickerInfo tickerInfo = await TickerHelper.GetTickerInfo(_cache);

            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                HomeV3Model model = new HomeV3Model();

                model.Blockchains = (await connection.QueryAsync<HomeV3BlockchainModel>(@"SELECT
b.Id BlockchainID,
b.GasTicker,
b.TokenTicker,
b.DisplayName BlockchainName,
b.LogoLocation,
 (
SELECT COUNT(DISTINCT I.NodeId)
FROM OTOffer O
JOIN OTOffer_Holders H ON H.OfferID = O.OfferId 
JOIN otidentity I ON I.Identity = H.Holder
WHERE O.BlockchainID = b.Id and O.IsFinalized = 1 AND NOW() <= DATE_ADD(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE)) ActiveNodes,
(
SELECT COUNT(*)
FROM otoffer
WHERE otoffer.IsFinalized = 1 AND otoffer.BlockchainID = b.Id
) TotalJobs,
(
SELECT COALESCE(SUM(CASE WHEN IsFinalized = 1 AND NOW() <= DATE_ADD(FinalizedTimeStamp, INTERVAL +HoldingTimeInMinutes MINUTE) THEN 1 ELSE 0 END), 0)
FROM otoffer WHERE blockchainid = b.id) AS ActiveJobs,
(select COALESCE(sum(Stake), 0) from otidentity WHERE blockchainid = b.id AND version = (select max(ii.version) from otidentity ii)) StakedTokens,
(SELECT COUNT(*) FROM OTOffer WHERE blockchainid = b.id and IsFinalized = 1 AND CreatedTimeStamp >= DATE_Add(NOW(), INTERVAL -1 DAY)) AS Jobs24H,
(SELECT AVG(otoffer.TokenAmountPerHolder) FROM otoffer WHERE blockchainid = b.id and IsFinalized = 1 AND CreatedTimeStamp >= DATE_Add(NOW(), INTERVAL -1 DAY)) AS JobsReward24H,
(SELECT AVG(otoffer.HoldingTimeInMinutes) FROM OTOffer WHERE blockchainid = b.id and IsFinalized = 1 AND CreatedTimeStamp >= DATE_Add(NOW(), INTERVAL -1 DAY)) AS JobsDuration24H,
(SELECT AVG(otoffer.DataSetSizeInBytes) FROM OTOffer WHERE blockchainid = b.id and IsFinalized = 1 AND CreatedTimeStamp >= DATE_Add(NOW(), INTERVAL -1 DAY)) AS JobsSize24H
FROM blockchains b
order by b.id desc")).ToArray();


                foreach (HomeV3BlockchainModel blockchain in model.Blockchains)
                {
                    blockchain.Fees = (await connection.QueryFirstOrDefaultAsync<HomeFeesModel>(@"SELECT 
bc.ShowCostInUSD,
CAST(AVG((CAST(oc.GasUsed AS DECIMAL(20,4)) * (CAST(oc.GasPrice AS DECIMAL(20,6)) / 1000000000000000000)) * (CASE WHEN bc.ShowCostInUSD AND bc.IsGasStableCoin = 0 THEN ocTicker.Price ELSE 1 END)) AS DECIMAL(20,6)) JobCreationCost, 
CAST(AVG((CAST(of.GasUsed AS DECIMAL(20,4)) * (CAST(of.GasPrice AS DECIMAL(20,6))  / 1000000000000000000)) * (CASE WHEN bc.ShowCostInUSD AND bc.IsGasStableCoin = 0 THEN ofTicker.Price ELSE 1 END)) AS DECIMAL(20,6)) JobFinalisedCost
FROM blockchains bc
LEFT JOIN otcontract_holding_offercreated oc ON bc.ID = oc.BlockchainID AND oc.Timestamp >= DATE_Add(NOW(), INTERVAL -1 DAY)
LEFT JOIN otcontract_holding_offerfinalized of ON of.OfferID = oc.OfferID AND of.BlockchainID = oc.BlockchainID AND of.Timestamp >= DATE_Add(NOW(), INTERVAL -1 DAY)
LEFT JOIN ticker_eth_to_usd ocTicker ON bc.IsGasStableCoin = 0 AND ocTicker.Timestamp = (
SELECT MAX(TIMESTAMP)
FROM ticker_eth_to_usd
WHERE TIMESTAMP <= oc.Timestamp)
LEFT JOIN ticker_eth_to_usd ofTicker ON bc.IsGasStableCoin = 0 AND ofTicker.Timestamp = (
SELECT MAX(TIMESTAMP)
FROM ticker_eth_to_usd
WHERE TIMESTAMP <= of.Timestamp)
WHERE bc.ID = @blockchainID", new
                    {
                        blockchainID = blockchain.BlockchainID
                    }));

                    decimal? payoutFee = (await connection.ExecuteScalarAsync<decimal?>(@"SELECT 
CAST(AVG((CAST(po.GasUsed AS DECIMAL(20,4)) * (CAST(po.GasPrice as decimal(20,6)) / 1000000000000000000)) * (CASE WHEN bc.ShowCostInUSD AND bc.IsGasStableCoin = 0 THEN ocTicker.Price ELSE 1 END)) AS DECIMAL(20, 8)) PayoutCost
FROM blockchains bc
LEFT JOIN otcontract_holding_paidout po ON po.BlockchainID = bc.ID AND po.Timestamp >= DATE_Add(NOW(), INTERVAL -1 DAY)
LEFT JOIN ticker_eth_to_usd ocTicker ON bc.IsGasStableCoin = 0 AND ocTicker.Timestamp = (
SELECT MAX(TIMESTAMP)
FROM ticker_eth_to_usd
WHERE TIMESTAMP <= po.Timestamp)
WHERE bc.ID = @blockchainID", new
                    {
                        blockchainID = blockchain.BlockchainID
                    }));

                    blockchain.Fees.PayoutCost = payoutFee;

                    if (blockchain.BlockchainName == "xDai")
                    {
                        blockchain.HoursTillFirstJob = await connection.ExecuteScalarAsync<int?>(@"
WITH CTE AS (
SELECT 
I.Identity,
I.NodeID,
(
SELECT o.FinalizedTimestamp
FROM otoffer_holders h 
JOIN otoffer o ON o.OfferID = h.OfferID
WHERE h.Holder = i.Identity 
ORDER BY o.FinalizedTimestamp
LIMIT 1
) FirstOfferDate,
bb.Timestamp CreatedDate
FROM otidentity i
JOIN otcontract_profile_identitycreated ic ON ic.NewIdentity = i.Identity AND ic.BlockchainID = i.BlockchainID
JOIN ethblock bb ON bb.BlockchainID = ic.BlockchainID AND bb.BlockNumber = ic.BlockNumber
WHERE i.BlockchainID = @id AND i.VERSION > 0 AND (
SELECT o.FinalizedTimestamp
FROM otoffer_holders h 
JOIN otoffer o ON o.OfferID = h.OfferID
WHERE h.Holder = i.Identity 
ORDER BY o.FinalizedTimestamp
LIMIT 1
) >= DATE_Add(NOW(), INTERVAL -1 DAY)
ORDER BY FirstOfferDate DESC
)

SELECT AVG(TIMESTAMPDIFF(HOUR, CreatedDate, FirstOfferDate)) TimeTillFirstJob FROM CTE", new
                        {
                            id = blockchain.BlockchainID
                        });
                    }
                }

                model.PriceUsd = tickerInfo.PriceUsd;
                model.PercentChange24H = tickerInfo.PercentChange24H;
                model.CirculatingSupply = tickerInfo.CirculatingSupply;
                model.MarketCapUsd = tickerInfo.MarketCapUsd;
                model.Volume24HUsd = tickerInfo.Volume24HUsd;
                model.PriceBtc = tickerInfo.PriceBtc;

                model.All = new HomeV3BlockchainModel
                {
                    BlockchainID = 0,
                    BlockchainName = "All Blockchains",
                    ActiveJobs = model.Blockchains.Sum(b => b.ActiveJobs),
                    ActiveNodes = model.Blockchains.Sum(b => b.ActiveNodes),
                    Jobs24H = model.Blockchains.Sum(b => b.Jobs24H),
                    JobsDuration24H = (long?)model.Blockchains.Select(b => b.JobsDuration24H).DefaultIfEmpty(null).Average(),
                    JobsReward24H = (decimal?) model.Blockchains.Where(b => b.JobsReward24H.HasValue).Average(b => b.JobsReward24H),
                    JobsSize24H = (long?) model.Blockchains.Where(b => b.JobsSize24H.HasValue).Average(b => b.JobsSize24H),
                    StakedTokens = model.Blockchains.Sum(b => b.StakedTokens),
                    TotalJobs = model.Blockchains.Sum(b => b.TotalJobs),
                    TokenTicker = model.Blockchains.Select(b => b.TokenTicker).Aggregate((a,b) => a + " | " + b)
                };

                _cache.Set("HomeV3", model, TimeSpan.FromMinutes(1));


                return model;
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
ROUND(COUNT(o.OfferID) / (@totalToday) * 100) AS Percentage
FROM blockchains bc
LEFT JOIN otoffer o ON bc.ID = o.BlockchainID AND o.IsFinalized = 1 AND o.FinalizedTimestamp >= DATE_Add(NOW(), INTERVAL -1 DAY)
GROUP BY bc.Id
ORDER BY Percentage")).ToArray();

                _cache.Set("24HJobBlockchainDistribution", data, TimeSpan.FromMinutes(5));

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
            if (_cache.TryGetValue("JobsChartDataV3", out var model) && model is JobsChartDataV2Model[] chartModel)
                return chartModel;

            //var response = new JobsChartDataV2();

            await using (var connection =
               new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var data = (await connection.QueryAsync<JobsChartDataV2Model>(@"SELECT * FROM jobhistorybyday")).ToList();

                if (data.LastOrDefault()?.Date.Date < DateTime.Now.Date)
                {
                    JobsChartDataV2Model today = await connection.QueryFirstOrDefaultAsync<JobsChartDataV2Model>(@"
SELECT 
x.Date,
COUNT(O.OfferId) NewJobs,
(
	SELECT COUNT(OI.OfferId) FROM OTOffer OI 
	WHERE 
	OI.IsFinalized = 1
	AND 
	DATE(DATE_Add(OI.FinalizedTimeStamp, INTERVAL + OI.HoldingTimeInMinutes MINUTE)) = x.Date
	)
	as CompletedJobs
FROM (
SELECT DATE(NOW()) Date
) x 
LEFT JOIN OTOffer O on O.IsFinalized = 1 AND x.Date = DATE(O.FinalizedTimestamp)
GROUP BY x.Date");

                    data.Add(today);
                }

                var output = data.ToArray();

                _cache.Set("JobsChartDataV3", output, TimeSpan.FromMinutes(1));

                return output;

                //response.Data = new int[][]
                //{
                //    data.Select(d => d.CompletedJobs).ToArray(),
                //    data.Select(d => d.NewJobs).ToArray()
                //};

                //response.Labels = data.Select(d => d.Label).ToArray();


         

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