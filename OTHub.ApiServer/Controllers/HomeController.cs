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
                var summary = connection.QuerySingle<HomeV3Model>(@"SELECT
 (
SELECT COUNT(DISTINCT H.Holder)
FROM OTOffer O
JOIN OTOffer_Holders H ON H.OfferID = O.OfferId
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



                summary.FeesByBlockchain = connection.Query<HomeFeesModel>(@"SELECT 
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
GROUP BY bc.id").ToArray();

                var payoutsCosts = connection.Query<HomeFeesModel>(@"SELECT 
bc.BlockchainName, bc.NetworkName,
bc.ShowCostInUSD,
CAST(AVG((CAST(po.GasUsed AS DECIMAL(20,4)) * (po.GasPrice / 1000000000000000000)) * (CASE WHEN bc.ShowCostInUSD THEN ocTicker.Price ELSE 1 END)) AS DECIMAL(20, 8)) PayoutCost
FROM blockchains bc
LEFT JOIN otcontract_holding_paidout po ON po.BlockchainID = bc.ID AND po.Timestamp >= DATE_Add(NOW(), INTERVAL -1 DAY)
LEFT JOIN ticker_trac ocTicker ON ocTicker.Timestamp = (
SELECT MAX(TIMESTAMP)
FROM ticker_trac
WHERE TIMESTAMP <= po.Timestamp)
GROUP BY bc.id").ToArray();

                //TODO move these 2 SQL queries back into one at some point
                foreach (var homeFeesModel in payoutsCosts)
                {
                    var model = summary.FeesByBlockchain.FirstOrDefault(b =>
                        b.NetworkName == homeFeesModel.NetworkName && b.BlockchainName == homeFeesModel.BlockchainName);
                    if (model != null)
                    {
                        model.PayoutCost = homeFeesModel.PayoutCost;
                    }
                }

                summary.StakedByBlockchain = connection.Query<HomeStakedModel>(@"SELECT bc.BlockchainName, bc.NetworkName, SUM(i.Stake) StakedTokens
FROM otidentity i
JOIN blockchains bc ON bc.ID = i.BlockchainID
WHERE i.version = (
SELECT MAX(ii.version)
FROM otidentity ii)
GROUP BY bc.ID").ToArray();

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
        public JobChartDataV2SummaryModel JobsChartDataSummaryV2()
        {
            if (_cache.TryGetValue("JobsChartDataSummaryV2", out var model) && model is JobChartDataV2SummaryModel chartModel)
                return chartModel;

            using (var connection =
            new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var summary = connection.QuerySingle<JobChartDataV2SummaryModel>(@"SELECT
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
        public HomeNodesInfo HomeNodesInfoV2()
        {
            if (_cache.TryGetValue("HomeNodesInfoV2", out var model) && model is HomeNodesInfo chartModel)
                return chartModel;

            using (var connection =
    new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                //	(SELECT count(distinct nodeId) as OnlineNodesCount FROM otnode_history WHERE TimeStamp >= DATE_Add(NOW(), INTERVAL -1 DAY) AND Success = 1) as OnlineNodesCount,
                //  ((SELECT count(distinct nodeId) FROM OTContract_Approval_NodeApproved) -(SELECT count(distinct nodeId) FROM OTContract_Approval_NodeRemoved)) as ApprovedNodesCount,

                //    (SELECT count(distinct h.nodeId) as OnlineNodesCount FROM otnode_history h join otnode_ipinfov2 i on i.nodeid = h.nodeid WHERE h.TimeStamp >= DATE_Add(NOW(), INTERVAL -3 HOUR) AND h.Success = 1) OnlineNodesCount,
                var homeInfo = connection.QuerySingle<HomeNodesInfo>($@"SELECT 
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
        public JobsChartDataV2Model[] JobsChartDataV3()
        {
            //if (_cache.TryGetValue("JobsChartDataV3", out var model) && model is JobsChartDataV2 chartModel)
            //    return chartModel;

            //var response = new JobsChartDataV2();

            using (var connection =
               new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var data = connection.Query<JobsChartDataV2Model>(@"SELECT * FROM jobhistorybyday").ToArray();

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

//        [HttpGet]
//        [Route("JobsChartData")]
//        [SwaggerOperation(
//            Summary = "Get total of jobs over the last 28 days including active jobs. ",
//            Description = @"The response contains 3 collections:
//- Labels: Each day has it's own label
//- NewJobs: The amount of new jobs for that specific day
//- ActiveJobs: The amount of active jobs for that specific day

//The collections are indexed in the same order. So the first item in each collection can be paired together, then the second item can be paired together etc."
//        )]
//        [SwaggerResponse(200, type: typeof(HomeJobsChartData))]
//        [SwaggerResponse(500, "Internal server error")]
//        public HomeJobsChartData GetJobsChartData()
//        {
//            if (_cache.TryGetValue("OTHub_HomeJobsChartModel", out var model) && model is HomeJobsChartData chartModel)
//                return chartModel;

//            chartModel = new HomeJobsChartData();

//            using (var connection =
//                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
//            {
//                HomeJobsChartDataModel[] chartData = connection.Query<HomeJobsChartDataModel>(@"SELECT 
//x.Date,
//COUNT(O.OfferId) NewJobs,
//(
//	SELECT COUNT(OI.OfferId) FROM OTOffer OI 
//	WHERE 
//	OI.IsFinalized = 1
//	AND 
//	DATE(OI.FinalizedTimeStamp) <= x.Date
//	AND
//	   (CASE WHEN x.Date= CURDATE() THEN NOW() ELSE  DATE_Add(x.Date, INTERVAL + 1 DAY) END)
//		<=
//		DATE_Add(OI.FinalizedTimeStamp, INTERVAL + OI.HoldingTimeInMinutes MINUTE)
//	)
//	as ActiveJobs
//FROM (
//SELECT CURDATE() Date
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 1 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 2 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 3 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 4 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 5 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 6 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 7 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 8 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 9 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 10 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 11 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 12 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 13 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 14 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 15 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 16 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 17 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 18 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 19 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 20 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 21 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 22 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 23 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 24 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 25 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 26 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 27 DAY)
//) x 
//LEFT JOIN OTOffer O on O.IsFinalized = 1 AND x.Date = DATE(O.FinalizedTimestamp)
//GROUP BY x.Date").ToArray();

//                List<string> labels = new List<string>();
//                List<int> activeJobs = new List<int>();
//                List<int> newJobs = new List<int>();

//                foreach (var row in chartData.OrderBy(d => d.Date))
//                {
//                    labels.Add(row.Date.ToString("MMM dd yyyy"));
//                    activeJobs.Add(row.ActiveJobs);
//                    newJobs.Add(row.NewJobs);
//                }

//                chartModel.Labels = labels.ToArray();
//                chartModel.ActiveJobs = activeJobs.ToArray();
//                chartModel.NewJobs = newJobs.ToArray();
//            }

//            _cache.Set("OTHub_HomeJobsChartModel", chartModel, TimeSpan.FromMinutes(4));

//            return chartModel;
//        }

//        [HttpGet()]
//        [SwaggerOperation(
//            Summary = "Retrieves statistics about the ODN which you can see on the OT Hub dashboard.",
//            Description = @"You can use this API call to retrieve the following:
//- Online Node count
//- Approved Node count
//- Nodes with Jobs count
//- Offers in last x time period
//- Payouts total
//- Litigation stats

//Please note market price information is not currently available within the response."
//        )]
//        [SwaggerResponse(200, type: typeof(HomeModel))]
//        [SwaggerResponse(500, "Internal server error")]
//        public HomeModel Get()
//        {
//            if (_cache.TryGetValue("OTHub_HomeModel", out var model) && model is HomeModel homeModel)
//                return homeModel;

//            using (var connection =
//                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
//            {
//                //	(SELECT count(distinct nodeId) as OnlineNodesCount FROM otnode_history WHERE TimeStamp >= DATE_Add(NOW(), INTERVAL -1 DAY) AND Success = 1) as OnlineNodesCount,
//                //  ((SELECT count(distinct nodeId) FROM OTContract_Approval_NodeApproved) -(SELECT count(distinct nodeId) FROM OTContract_Approval_NodeRemoved)) as ApprovedNodesCount,

//               var homeInfo = connection.QuerySingle<HomeNodesInfo>($@"SELECT 
//    (SELECT count(distinct h.nodeId) as OnlineNodesCount FROM otnode_history h join otnode_ipinfo i on i.nodeid = h.nodeid WHERE h.TimeStamp >= DATE_Add(NOW(), INTERVAL -1 HOUR) AND h.Success = 1 and i.NetworkId like '{OTHubSettings.Instance.Blockchain.Network}V4.0') OnlineNodesCount,
//    (select count(distinct n.nodeId) FROM OTNode_IPInfo n JOIN OTIdentity I ON I.NodeId = n.NodeId WHERE I.Approved = 1) ApprovedNodesCount,
//	(select sum(Stake) from otidentity where version = (select max(ii.version) from otidentity ii)) StakedTokensTotal,
//    (SELECT count(distinct H.Holder) FROM OTOffer_Holders H JOIN OTOffer O on O.OfferID = H.OfferID WHERE O.IsFinalized = 1 AND O.FinalizedTimeStamp >= DATE_Add(NOW(), INTERVAL -7 DAY)) NodesWithJobsThisWeek,
//    (SELECT count(distinct H.Holder) FROM OTOffer_Holders H JOIN OTOffer O on O.OfferID = H.OfferID WHERE O.IsFinalized = 1 AND O.FinalizedTimeStamp >= DATE_Add(NOW(), INTERVAL -1 MONTH)) NodesWithJobsThisMonth,
//    (SELECT COUNT(distinct H.Holder) FROM OTOffer O JOIN OTOffer_Holders H ON H.OfferID = O.OfferId WHERE O.IsFinalized = 1 AND NOW() <= DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE)) NodesWithActiveJobs,
//	(select sum(StakeReserved) from otidentity where version = (select max(ii.version) from otidentity ii)) LockedTokensTotal");

////                var lastApprovals = connection.QueryFirstOrDefault(
////                    @"SELECT Timestamp, COUNT(*) Amount FROM otcontract_approval_nodeapproved
////GROUP BY Timestamp
////ORDER BY Timestamp DESC
////LIMIT 1");

//                //homeInfo.LastApprovalTimestamp = (DateTime?)lastApprovals?.Timestamp;
//                //homeInfo.LastApprovalAmount = (Int32?) lastApprovals?.Amount;
          
//                var offersInfo = connection.QuerySingle<HomeOffersInfo>(@"SELECT
//	(SELECT COUNT(*) FROM OTOffer WHERE IsFinalized = 1) as OffersTotal,
//    (SELECT COUNT(*) FROM OTOffer WHERE IsFinalized = 1 AND CreatedTimeStamp >= DATE_Add(NOW(), INTERVAL -1 DAY)) as OffersLast24Hours,
//    (SELECT COUNT(*) FROM OTOffer WHERE IsFinalized = 1 AND CreatedTimeStamp >= DATE_Add(NOW(), INTERVAL - 7 DAY)) as OffersLast7Days,
//	(SELECT SUM(CASE WHEN IsFinalized = 1 AND NOW() <= DATE_Add(FinalizedTimeStamp, INTERVAL +HoldingTimeInMinutes MINUTE) THEN 1 ELSE 0 END)
//	 FROM OTOffer) as OffersActive");

//                //	(SELECT COALESCE(SUM(Amount), 0) FROM OTContract_Holding_Paidout WHERE TimeStamp >= DATE_Add(NOW(), INTERVAL -1 DAY)) as PayoutsLast24Hours,
//                //(SELECT COALESCE(SUM(Amount), 0) FROM OTContract_Holding_Paidout WHERE TimeStamp >= DATE_Add(NOW(), INTERVAL - 7 DAY)) as PayoutsLast7Days
//                var payoutsInfo = connection.QuerySingle<HomePayoutsInfo>(@"SELECT
//	(SELECT COALESCE(SUM(Amount), 0) FROM OTContract_Holding_Paidout) as PayoutsTotal");

//                var litigationsInfo = connection.QuerySingle<HomeLitigationsInfo>(@"SELECT (SELECT COUNT(*) FROM otcontract_litigation_litigationinitiated) LitigationsTotal, 
//(SELECT COUNT(*) FROM otcontract_litigation_litigationinitiated WHERE TimeStamp >= DATE_Add(NOW(), INTERVAL - 7 DAY)) Litigations7Days,
// (SELECT COUNT(*) FROM otcontract_litigation_litigationcompleted WHERE TimeStamp >= DATE_Add(NOW(), INTERVAL - 7 DAY) AND DHWasPenalized = 1) Litigations7DaysPenalized, 
// (SELECT COUNT(*) FROM otcontract_litigation_litigationcompleted WHERE TimeStamp >= DATE_Add(NOW(), INTERVAL - 7 DAY) AND DHWasPenalized = 0) Litigations7DaysNotPenalized,
// (SELECT COUNT(*) FROM otcontract_litigation_litigationinitiated WHERE TimeStamp >= DATE_Add(NOW(), INTERVAL - 1 MONTH)) Litigations1Month,
// (SELECT COUNT(*) FROM otcontract_litigation_litigationcompleted WHERE TimeStamp >= DATE_Add(NOW(), INTERVAL - 1 MONTH) AND DHWasPenalized = 1) Litigations1MonthPenalized, 
// (SELECT COUNT(*) FROM otcontract_litigation_litigationcompleted WHERE TimeStamp >= DATE_Add(NOW(), INTERVAL - 1 MONTH) AND DHWasPenalized = 0) Litigations1MonthNotPenalized,
//  (SELECT COUNT(*) FROM OTOffer_Holders H join ethblock b on b.BlockNumber = H.LitigationStatusBlockNumber WHERE H.LitigationStatus in (1,2) AND b.TimeStamp >= DATE_Add(NOW(), INTERVAL - 1 HOUR)) LitigationsActiveLastHour ");

//                homeModel = new HomeModel
//                {
//                    MarketInfo = new HomeMarketInfo
//                    {
//                        Change24Hours = 0,
//                        MarketCap = 0,
//                        USDValue = 0
//                    },
//                    NodesInfo = homeInfo,
//                    OffersInfo = offersInfo,
//                    PayoutsInfo = payoutsInfo,
//                    LitigationsInfo = litigationsInfo
//                };

//                _cache.Set("OTHub_HomeModel", homeModel, TimeSpan.FromMinutes(1));

//                return homeModel;
//            }
//        }
    }
}