using System;
using System.Linq;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OTHub.APIServer.Models;
using OTHub.Settings;
using ServiceStack;
using ServiceStack.Text;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using static System.Net.WebRequestMethods;

namespace OTHub.APIServer.Controllers
{
    [Route("api/[controller]")]
    public class JobsController : Controller
    {
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets all offers (no paging)",
            Description = @"Please note that this API call has been replaced by the api/jobs/paging API call

This will return a summary of information about each offer.

If you want to get more information about a specific offer you should use /api/jobs/detail/{offerID} API call"
        )]
        [SwaggerResponse(200, type: typeof(OfferSummaryModel[]))]
        [SwaggerResponse(500, "Internal server error")]
        [Obsolete]
        public OfferSummaryModel[] Get()
        {
            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                OfferSummaryModel[] summary = connection.Query<OfferSummaryModel>(
                    @"SELECT I.Identity DCIdentity, O.OfferId, O.CreatedTimestamp as Timestamp, O.DataSetSizeInBytes, O.TokenAmountPerHolder, O.HoldingTimeInMinutes, O.IsFinalized,
(CASE WHEN O.IsFinalized = 1 
	THEN (CASE WHEN NOW() <= DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) THEN 'Active' ELSE 'Completed' END)
	ELSE (CASE WHEN O.CreatedTimeStamp <= DATE_Add(NOW(), INTERVAL -30 MINUTE)
		THEN 'Not Started'
		ELSE 'Not Started'
	END)
END) as Status,
(CASE WHEN O.IsFinalized = 1  THEN DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) ELSE NULL END) as EndTimestamp
FROM OTOffer O
LEFT JOIN OTIdentity I ON I.NodeID = O.DCNodeID
GROUP BY O.OfferID").ToArray();

                return summary;
            }
        }

        [HttpGet]
        [Route("paging")]
        [SwaggerOperation(
            Summary = "Gets all offers (paging)",
            Description = @"
This will return a summary of information about each offer.

If you want to get more information about a specific offer you should use /api/jobs/detail/{offerID} API call"
        )]
        [SwaggerResponse(200, type: typeof(OfferSummaryModel[]))]
        [SwaggerResponse(500, "Internal server error")]
        public IActionResult GetWithPaging([FromQuery, SwaggerParameter("How many offers you want to return per page", Required = true)] int _limit, [FromQuery, SwaggerParameter("The page number to start from. The first page is 0.", Required = true)] int _page, [FromQuery] string OfferId_like,
            [FromQuery] string _sort,
            [FromQuery] string _order,
            [FromQuery] bool export,
            [FromQuery] int? exportType)
        {
            _page--;

            if (OfferId_like != null && OfferId_like.Length > 200)
            {
                OfferId_like = null;
            }

            string orderBy = String.Empty;

            switch (_sort)
            {
                case "Timestamp":
                    orderBy = "ORDER BY Timestamp";
                    break;
                case "DataSetSizeInBytes":
                    orderBy = "ORDER BY DataSetSizeInBytes";
                    break;
                case "HoldingTimeInMinutes":
                    orderBy = "ORDER BY HoldingTimeInMinutes";
                    break;
                case "TokenAmountPerHolder":
                    orderBy = "ORDER BY TokenAmountPerHolder";
                    break;
            }

            if (!String.IsNullOrWhiteSpace(orderBy))
            {
                switch (_order)
                {
                    case "ASC":
                        orderBy += " ASC";
                        break;
                    case "DESC":
                        orderBy += " DESC";
                        break;
                }
            }

            string limit = string.Empty;

            if (_page >= 0 && _limit >= 0)
            {
                limit = $"LIMIT {_page},{_limit}";
            } 

            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                OfferSummaryModel[] summary = connection.Query<OfferSummaryModel>(
                    $@"SELECT I.Identity DCIdentity, O.OfferId, O.CreatedTimestamp as Timestamp, O.DataSetSizeInBytes, O.TokenAmountPerHolder, O.HoldingTimeInMinutes, O.IsFinalized,
(CASE WHEN O.IsFinalized = 1 
	THEN (CASE WHEN NOW() <= DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) THEN 'Active' ELSE 'Completed' END)
	ELSE (CASE WHEN O.CreatedTimeStamp <= DATE_Add(NOW(), INTERVAL -30 MINUTE)
		THEN 'Not Started'
		ELSE 'Not Started'
	END)
END) as Status,
(CASE WHEN O.IsFinalized = 1  THEN DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) ELSE NULL END) as EndTimestamp
FROM OTOffer O
LEFT JOIN OTIdentity I ON I.NodeID = O.DCNodeID
WHERE COALESCE(@OfferId_like, '') = '' OR O.OfferId = @OfferId_like
GROUP BY O.OfferID
{orderBy}
{limit}", new
                    {
                        OfferId_like
                    }).ToArray();

                var total = connection.ExecuteScalar<int>(@"SELECT COUNT(DISTINCT O.OfferID)
FROM OTOffer O
LEFT JOIN OTIdentity I ON I.NodeID = O.DCNodeID
WHERE COALESCE(@OfferId_like, '') = '' OR O.OfferId = @OfferId_like", new
                {
                    OfferId_like
                });

                HttpContext.Response.Headers["access-control-expose-headers"] = "X-Total-Count";
                HttpContext.Response.Headers["X-Total-Count"] = total.ToString();

                if (export)
                {
                    if (exportType == 0)
                    {
                        return File(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(summary)), "application/json", "jobs.json", false);
                    }
                    else if (exportType == 1)
                    {
                        return File(Encoding.UTF8.GetBytes(CsvSerializer.SerializeToCsv(summary)), "text/csv", "jobs.csv", false);
                    }
                }

                return new OkObjectResult(summary);

                //return new OfferSummaryWithPaging
                //{
                //    data = summary,
                //    draw = summary.Length,
                //    recordsFiltered = total,
                //    recordsTotal = total
                //};
            }
        }



        [HttpGet]
        [Route("detail/{offerID}")]
        [SwaggerOperation(
            Summary = "Get detailed information about a offer",
            Description = @"This will return most information known about the offer.

Data Included:
- Dataset Information
- Timeline
- Data Holders")]
        [SwaggerResponse(200, type: typeof(NodeDataHolderDetailedModel))]
        [SwaggerResponse(500, "Internal server error")]
        [SwaggerRequestExample(typeof(string), typeof(OfferExample), jsonConverter: typeof(StringEnumConverter))]
        public OfferDetailedModel Detail([SwaggerParameter("The ID of the offer", Required = true)]string offerID)
        {
            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                OfferDetailedModel model = connection.QueryFirstOrDefault<OfferDetailedModel>(
                    @"SELECT O.OfferId, O.EstimatedLambda, O.CreatedTimestamp as Timestamp, O.FinalizedTimestamp, O.LitigationIntervalInMinutes, O.DataSetId, O.DataSetSizeInBytes, O.TokenAmountPerHolder, O.HoldingTimeInMinutes, O.IsFinalized,
(CASE WHEN IsFinalized = 1 
	THEN (CASE WHEN NOW() <= DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) THEN 'Active' ELSE 'Completed' END)
	ELSE (CASE WHEN O.CreatedTimeStamp <= DATE_Add(NOW(), INTERVAL -30 MINUTE)
		THEN 'Not Started'
		ELSE 'Not Started'
	END)
END) as Status,
(CASE WHEN O.IsFinalized = 1  THEN DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) ELSE NULL END) as EndTimestamp
, O.CreatedBlockNumber, O.CreatedTransactionHash, O.DcNodeId, DCI.Identity DCIdentity,
(SELECT COUNT(*) FROM OTOffer IO WHERE IO.DCNodeID = O.DCNodeID AND IO.IsFinalized = 1) as OffersTotal,
(SELECT COUNT(*) FROM OTOffer IO WHERE IO.DCNodeID = O.DCNodeID AND IO.IsFinalized = 1 AND IO.CreatedTimeStamp >= DATE_Add(NOW(), INTERVAL -7 DAY)) as OffersLast7Days,
(SELECT COALESCE(SUM(Amount), 0) FROM OTContract_Holding_Paidout IP
JOIN OTOffer IO ON IO.OfferID = IP.OfferID
WHERE IO.DCNodeId = O.DCNodeId) as PaidoutTokensTotal,
O.FinalizedBlockNumber,
O.FinalizedTransactionHash,
OC.GasUsed CreatedGasUsed,
OF.GasUsed FinalizedGasUsed,
OC.GasPrice CreatedGasPrice,
OF.GasPrice FinalizedGasPrice
 FROM OTOffer O
 JOIN OTContract_Holding_OfferCreated OC ON OC.OfferID = O.OfferID
 LEFT JOIN OTContract_Holding_OfferFinalized OF ON OF.OfferID = O.OfferID
 LEFT JOIN OTIdentity DCI ON DCI.NodeId = O.DCNodeId
WHERE O.OfferId = @offerID", new { offerID = offerID });
                if (model != null)
                {
                    model.Holders = connection.Query<OfferDetailedHolderModel>(
                        @"SELECT H.Holder as Identity, CASE WHEN H.LitigationStatus = 0 AND (lc.TransactionHash is null OR lc.DHWasPenalized = 0) THEN NULL ELSE H.LitigationStatus END LitigationStatus,
(CASE WHEN IsFinalized = 1 
	THEN (CASE WHEN NOW() <= DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) THEN
	(CASE 
		WHEN h.LitigationStatus = '4' THEN 'Data Holder Replaced' 
		WHEN h.LitigationStatus = '3' THEN 'Data Holder is Being Replaced' 
		WHEN h.LitigationStatus = '2' THEN 'Active (Litigation Answered)' 
		WHEN h.LitigationStatus = '1' THEN 'Active (Litigation Initiated)' 
		WHEN h.LitigationStatus = '0' and lc.DHWasPenalized = 1 THEN 'Active (Litigation Failed)' 
		WHEN h.LitigationStatus = '0' and (lc.TransactionHash is null OR lc.DHWasPenalized = 0) THEN 'Active (Litigation Passed)' 
		ELSE 'Active' END)
	 ELSE
	(CASE 
		WHEN h.LitigationStatus = '4' THEN 'Data Holder Replaced' 
		WHEN h.LitigationStatus = '3' THEN 'Data Holder is Being Replaced' 
		WHEN h.LitigationStatus = '2' THEN 'Completed (Litigation Answered)' 
		WHEN h.LitigationStatus = '1' THEN 'Completed (Litigation Initiated)' 
		WHEN h.LitigationStatus = '0' and lc.DHWasPenalized = 1 THEN 'Litigation Failed' 
		WHEN h.LitigationStatus = '0' and (lc.TransactionHash is null OR lc.DHWasPenalized = 0) THEN 'Completed (Litigation Passed)' 
		ELSE 'Completed' END)
	  END)
	ELSE ''
END) as LitigationStatusText
 FROM OTOffer_Holders H
 JOIN OTOffer O ON O.OfferID = H.OfferID
left join otcontract_litigation_litigationcompleted lc on lc.OfferId = h.OfferId and lc.HolderIdentity = h.Holder and lc.BlockNumber = h.LitigationStatusBlockNumber and h.LitigationStatus = 0
Where H.OfferId = @offerID
ORDER BY H.LitigationStatus", new
                        {
                            offerID = offerID
                        }).ToArray();

                    model.Timeline = connection.Query<OfferDetailedTimelineModel>($@"select Timestamp, 'Offer Created' as Name, null as 'RelatedTo', TransactionHash  from otcontract_holding_offercreated
where OfferId = @offerID
union all
select Timestamp, 'Offer Finalized', null, TransactionHash from otcontract_holding_offerfinalized
where OfferId = @offerID
union all
select Timestamp, 'Data Holder Chosen' as Name, Holder as 'RelatedTo', TransactionHash  from otoffer_holders h
join otcontract_holding_offerfinalized of on of.OfferID = h.OfferId
where h.OfferId = @offerID AND h.IsOriginalHolder = 1
union all
select Timestamp, 'Litigation Initiated', HolderIdentity, TransactionHash from otcontract_litigation_litigationinitiated
where OfferId = @offerID
union all
select Timestamp, 'Litigation Timed out', HolderIdentity, TransactionHash from otcontract_litigation_litigationtimedout
where OfferId = @offerID
union all
select Timestamp, 'Litigation Answered', HolderIdentity, TransactionHash from otcontract_litigation_litigationanswered
where OfferId = @offerID
union all
select Timestamp, CASE WHEN DHWasPenalized = 1 THEN 'Litigation Failed' ELSE 'Litigation Passed' END, HolderIdentity, TransactionHash from otcontract_litigation_litigationcompleted
where OfferId = @offerID
union all
select Timestamp, 'Data Holder Replaced', HolderIdentity, TransactionHash from otcontract_litigation_replacementstarted
where OfferId = @offerID
union all
select Timestamp, 'Data Holder Chosen', ChosenHolder, TransactionHash from otcontract_replacement_replacementcompleted
where OfferId = @offerID
union all
select Timestamp, CONCAT('Offer Paidout for ', (CAST(TRUNCATE(`Amount`, 3) AS CHAR)+0), ' {(OTHubSettings.Instance.Blockchain.Network == BlockchainNetwork.Testnet ? "ATRAC" : "TRAC")}'), Holder, TransactionHash from otcontract_holding_paidout
where OfferId = @offerID
union all
select DATE_Add(of.Timestamp, INTERVAL + oc.HoldingTimeInMinutes MINUTE), 'Offer Completed', null, null from otcontract_holding_offerfinalized of
join otcontract_holding_offercreated oc on oc.OfferId = of.OfferId
where of.OfferId = @offerID 
and NOW() >= DATE_Add(of.Timestamp, INTERVAL + oc.HoldingTimeInMinutes MINUTE)", new {offerID = offerID }).OrderBy(t => t.Timestamp).ToArray();
                }

                return model;
            }
        }
    }
}