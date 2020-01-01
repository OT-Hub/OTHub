using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MySql.Data.MySqlClient;
using OTHub.APIServer.Models;
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

//        [HttpGet]
//        [Route("NodesChartData")]
//        [Obsolete]
//        public HomeNodesChartData GetNodesChartData()
//        {
//            if (_cache.TryGetValue("OTHub_HomeNodesChartModel", out var model) && model is HomeNodesChartData chartModel)
//                return chartModel;

//            chartModel = new HomeNodesChartData();

//            using (var connection =
//                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
//            {
//                HomeNodesChartDataModel[] chartData = connection.Query<HomeNodesChartDataModel>(@"SELECT 
//x.Date,
//count(distinct oth.NodeId) as OnlineNodes,
//(SELECT COUNT(distinct I.NodeId) FROM OTIdentity I JOIN OTOffer O ON I.NodeId = O.DCNodeId WHERE O.CreatedTimestamp <= x.Date AND I.Version = 1) DataCreatorNodes,
//(SELECT COUNT(distinct I.NodeId) FROM OTIdentity I JOIN OTContract_Approval_NodeApproved NA ON I.NodeId = NA.NodeID WHERE NA.Timestamp <= x.Date AND I.Version = 1) ApprovedNodes
//FROM (
//SELECT CURDATE() Date
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 41 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 42 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 43 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 44 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 45 DAY)
//UNION 
//SELECT DATE_Add(CURDATE(), INTERVAL - 46 DAY)
//) x 
//LEFT JOIN OTNode_History oth on oth.Success = 1 AND Date(oth.Timestamp) = x.Date AND (x.Date != CURDATE() OR oth.TimeStamp >= DATE_Add(NOW(), INTERVAL -1 HOUR))
//GROUP BY x.Date").ToArray();

//                List<string> labels = new List<string>();
//                List<int> onlineNodes = new List<int>();
//                List<int> dataCreatorNodes = new List<int>();
//                List<int> approvedNodes = new List<int>();
//                foreach (var row in chartData.OrderBy(d => d.Date))
//                {
//                    labels.Add(row.Date.ToString("MMM dd yyyy"));
//                    onlineNodes.Add(row.OnlineNodes);
//                    dataCreatorNodes.Add(row.DataCreatorNodes);
//                    approvedNodes.Add(row.ApprovedNodes);
//                }

//                chartModel.Labels = labels.ToArray();
//                chartModel.OnlineNodes = onlineNodes.ToArray();
//                chartModel.DataCreatorNodes = dataCreatorNodes.ToArray();
//                chartModel.ApprovedNodes = approvedNodes.ToArray();
//            }

//            _cache.Set("OTHub_HomeNodesChartModel", chartModel, TimeSpan.FromMinutes(3));

//            return chartModel;
//        }

        [HttpGet]
        [Route("JobsChartData")]
        [SwaggerOperation(
            Summary = "Get total of jobs over the last 28 days including active jobs. ",
            Description = @"The response contains 3 collections:
- Labels: Each day has it's own label
- NewJobs: The amount of new jobs for that specific day
- ActiveJobs: The amount of active jobs for that specific day

The collections are indexed in the same order. So the first item in each collection can be paired together, then the second item can be paired together etc."
        )]
        [SwaggerResponse(200, type: typeof(HomeJobsChartData))]
        [SwaggerResponse(500, "Internal server error")]
        public HomeJobsChartData GetJobsChartData()
        {
            if (_cache.TryGetValue("OTHub_HomeJobsChartModel", out var model) && model is HomeJobsChartData chartModel)
                return chartModel;

            chartModel = new HomeJobsChartData();

            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                HomeJobsChartDataModel[] chartData = connection.Query<HomeJobsChartDataModel>(@"SELECT 
x.Date,
COUNT(O.OfferId) NewJobs,
(
	SELECT COUNT(OI.OfferId) FROM OTOffer OI 
	WHERE 
	OI.IsFinalized = 1
	AND 
	DATE(OI.FinalizedTimeStamp) <= x.Date
	AND
	   (CASE WHEN x.Date= CURDATE() THEN NOW() ELSE  DATE_Add(x.Date, INTERVAL + 1 DAY) END)
		<=
		DATE_Add(OI.FinalizedTimeStamp, INTERVAL + OI.HoldingTimeInMinutes MINUTE)
	)
	as ActiveJobs
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
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 7 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 8 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 9 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 10 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 11 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 12 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 13 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 14 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 15 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 16 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 17 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 18 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 19 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 20 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 21 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 22 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 23 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 24 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 25 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 26 DAY)
UNION 
SELECT DATE_Add(CURDATE(), INTERVAL - 27 DAY)
) x 
LEFT JOIN OTOffer O on O.IsFinalized = 1 AND x.Date = DATE(O.FinalizedTimestamp)
GROUP BY x.Date").ToArray();

                List<string> labels = new List<string>();
                List<int> activeJobs = new List<int>();
                List<int> newJobs = new List<int>();

                foreach (var row in chartData.OrderBy(d => d.Date))
                {
                    labels.Add(row.Date.ToString("MMM dd yyyy"));
                    activeJobs.Add(row.ActiveJobs);
                    newJobs.Add(row.NewJobs);
                }

                chartModel.Labels = labels.ToArray();
                chartModel.ActiveJobs = activeJobs.ToArray();
                chartModel.NewJobs = newJobs.ToArray();
            }

            _cache.Set("OTHub_HomeJobsChartModel", chartModel, TimeSpan.FromMinutes(4));

            return chartModel;
        }

        [HttpGet()]
        [SwaggerOperation(
            Summary = "Retrieves statistics about the ODN which you can see on the OT Hub dashboard.",
            Description = @"You can use this API call to retrieve the following:
- Online Node count
- Approved Node count
- Nodes with Jobs count
- Offers in last x time period
- Payouts total
- Litigation stats

Please note market price information is not currently available within the response."
        )]
        [SwaggerResponse(200, type: typeof(HomeModel))]
        [SwaggerResponse(500, "Internal server error")]
        public HomeModel Get()
        {
            if (_cache.TryGetValue("OTHub_HomeModel", out var model) && model is HomeModel homeModel)
                return homeModel;

            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                //	(SELECT count(distinct nodeId) as OnlineNodesCount FROM otnode_history WHERE TimeStamp >= DATE_Add(NOW(), INTERVAL -1 DAY) AND Success = 1) as OnlineNodesCount,
                //  ((SELECT count(distinct nodeId) FROM OTContract_Approval_NodeApproved) -(SELECT count(distinct nodeId) FROM OTContract_Approval_NodeRemoved)) as ApprovedNodesCount,

               var homeInfo = connection.QuerySingle<HomeNodesInfo>($@"SELECT 
    (SELECT count(distinct h.nodeId) as OnlineNodesCount FROM otnode_history h join otnode_ipinfo i on i.nodeid = h.nodeid WHERE h.TimeStamp >= DATE_Add(NOW(), INTERVAL -1 HOUR) AND h.Success = 1 and i.NetworkId like '{OTHubSettings.Instance.Blockchain.Network}V4.0') OnlineNodesCount,
    (select count(distinct n.nodeId) FROM OTNode_IPInfo n JOIN OTIdentity I ON I.NodeId = n.NodeId WHERE I.Approved = 1) ApprovedNodesCount,
	(select sum(Stake) from otidentity where version = (select max(ii.version) from otidentity ii)) StakedTokensTotal,
    (SELECT count(distinct H.Holder) FROM OTOffer_Holders H JOIN OTOffer O on O.OfferID = H.OfferID WHERE O.IsFinalized = 1 AND O.FinalizedTimeStamp >= DATE_Add(NOW(), INTERVAL -7 DAY)) NodesWithJobsThisWeek,
    (SELECT count(distinct H.Holder) FROM OTOffer_Holders H JOIN OTOffer O on O.OfferID = H.OfferID WHERE O.IsFinalized = 1 AND O.FinalizedTimeStamp >= DATE_Add(NOW(), INTERVAL -1 MONTH)) NodesWithJobsThisMonth,
    (SELECT COUNT(distinct H.Holder) FROM OTOffer O JOIN OTOffer_Holders H ON H.OfferID = O.OfferId WHERE O.IsFinalized = 1 AND NOW() <= DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE)) NodesWithActiveJobs,
	(select sum(StakeReserved) from otidentity where version = (select max(ii.version) from otidentity ii)) LockedTokensTotal");

                var lastApprovals = connection.QueryFirstOrDefault(
                    @"SELECT Timestamp, COUNT(*) Amount FROM otcontract_approval_nodeapproved
GROUP BY Timestamp
ORDER BY Timestamp DESC
LIMIT 1");

                homeInfo.LastApprovalTimestamp = (DateTime?)lastApprovals?.Timestamp;
                homeInfo.LastApprovalAmount = (Int32?) lastApprovals?.Amount;
          
                var offersInfo = connection.QuerySingle<HomeOffersInfo>(@"SELECT
	(SELECT COUNT(*) FROM OTOffer WHERE IsFinalized = 1) as OffersTotal,
    (SELECT COUNT(*) FROM OTOffer WHERE IsFinalized = 1 AND CreatedTimeStamp >= DATE_Add(NOW(), INTERVAL -1 DAY)) as OffersLast24Hours,
    (SELECT COUNT(*) FROM OTOffer WHERE IsFinalized = 1 AND CreatedTimeStamp >= DATE_Add(NOW(), INTERVAL - 7 DAY)) as OffersLast7Days,
	(SELECT SUM(CASE WHEN IsFinalized = 1 AND NOW() <= DATE_Add(FinalizedTimeStamp, INTERVAL +HoldingTimeInMinutes MINUTE) THEN 1 ELSE 0 END)
	 FROM OTOffer) as OffersActive");

                //	(SELECT COALESCE(SUM(Amount), 0) FROM OTContract_Holding_Paidout WHERE TimeStamp >= DATE_Add(NOW(), INTERVAL -1 DAY)) as PayoutsLast24Hours,
                //(SELECT COALESCE(SUM(Amount), 0) FROM OTContract_Holding_Paidout WHERE TimeStamp >= DATE_Add(NOW(), INTERVAL - 7 DAY)) as PayoutsLast7Days
                var payoutsInfo = connection.QuerySingle<HomePayoutsInfo>(@"SELECT
	(SELECT COALESCE(SUM(Amount), 0) FROM OTContract_Holding_Paidout) as PayoutsTotal");

                var litigationsInfo = connection.QuerySingle<HomeLitigationsInfo>(@"SELECT (SELECT COUNT(*) FROM otcontract_litigation_litigationinitiated) LitigationsTotal, 
(SELECT COUNT(*) FROM otcontract_litigation_litigationinitiated WHERE TimeStamp >= DATE_Add(NOW(), INTERVAL - 7 DAY)) Litigations7Days,
 (SELECT COUNT(*) FROM otcontract_litigation_litigationcompleted WHERE TimeStamp >= DATE_Add(NOW(), INTERVAL - 7 DAY) AND DHWasPenalized = 1) Litigations7DaysPenalized, 
 (SELECT COUNT(*) FROM otcontract_litigation_litigationcompleted WHERE TimeStamp >= DATE_Add(NOW(), INTERVAL - 7 DAY) AND DHWasPenalized = 0) Litigations7DaysNotPenalized,
 (SELECT COUNT(*) FROM otcontract_litigation_litigationinitiated WHERE TimeStamp >= DATE_Add(NOW(), INTERVAL - 1 MONTH)) Litigations1Month,
 (SELECT COUNT(*) FROM otcontract_litigation_litigationcompleted WHERE TimeStamp >= DATE_Add(NOW(), INTERVAL - 1 MONTH) AND DHWasPenalized = 1) Litigations1MonthPenalized, 
 (SELECT COUNT(*) FROM otcontract_litigation_litigationcompleted WHERE TimeStamp >= DATE_Add(NOW(), INTERVAL - 1 MONTH) AND DHWasPenalized = 0) Litigations1MonthNotPenalized,
  (SELECT COUNT(*) FROM OTOffer_Holders H join ethblock b on b.BlockNumber = H.LitigationStatusBlockNumber WHERE H.LitigationStatus in (1,2) AND b.TimeStamp >= DATE_Add(NOW(), INTERVAL - 1 HOUR)) LitigationsActiveLastHour ");

                homeModel = new HomeModel
                {
                    MarketInfo = new HomeMarketInfo
                    {
                        Change24Hours = 0,
                        MarketCap = 0,
                        USDValue = 0
                    },
                    NodesInfo = homeInfo,
                    OffersInfo = offersInfo,
                    PayoutsInfo = payoutsInfo,
                    LitigationsInfo = litigationsInfo
                };

                _cache.Set("OTHub_HomeModel", homeModel, TimeSpan.FromMinutes(1));

                return homeModel;
            }
        }
    }
}