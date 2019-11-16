using System;
using System.Linq;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Converters;
using OTHub.APIServer.Models;
using OTHub.Settings;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

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
		THEN 'Expired'
		ELSE 'Bidding'
	END)
END) as Status,
(CASE WHEN O.IsFinalized = 1  THEN DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) ELSE NULL END) as EndTimestamp
FROM OTOffer O
JOIN OTIdentity I ON I.NodeID = O.DCNodeID
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
        public OfferSummaryWithPaging GetWithPaging([FromQuery, SwaggerParameter("How many offers you want to return per page", Required = true)] int pageLength, [FromQuery, SwaggerParameter("The page number to start from. The first page is 0.", Required = true)] int start)
        {
            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                OfferSummaryModel[] summary = connection.Query<OfferSummaryModel>(
                    $@"SELECT I.Identity DCIdentity, O.OfferId, O.CreatedTimestamp as Timestamp, O.DataSetSizeInBytes, O.TokenAmountPerHolder, O.HoldingTimeInMinutes, O.IsFinalized,
(CASE WHEN O.IsFinalized = 1 
	THEN (CASE WHEN NOW() <= DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) THEN 'Active' ELSE 'Completed' END)
	ELSE (CASE WHEN O.CreatedTimeStamp <= DATE_Add(NOW(), INTERVAL -30 MINUTE)
		THEN 'Expired'
		ELSE 'Bidding'
	END)
END) as Status,
(CASE WHEN O.IsFinalized = 1  THEN DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) ELSE NULL END) as EndTimestamp
FROM OTOffer O
JOIN OTIdentity I ON I.NodeID = O.DCNodeID
GROUP BY O.OfferID
ORDER BY O.CreatedTimestamp DESC
LIMIT {start},{pageLength}").ToArray();

                var total = connection.ExecuteScalar<int>(@"SELECT COUNT(DISTINCT O.OfferID)
FROM OTOffer O
JOIN OTIdentity I ON I.NodeID = O.DCNodeID");

                return new OfferSummaryWithPaging
                {
                    data = summary,
                    draw = summary.Length,
                    recordsFiltered = total,
                    recordsTotal = total
                };
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
                    @"SELECT O.OfferId, O.CreatedTimestamp as Timestamp, O.FinalizedTimestamp, O.LitigationIntervalInMinutes, O.DataSetId, O.DataSetSizeInBytes, O.TokenAmountPerHolder, O.HoldingTimeInMinutes, O.IsFinalized,
(CASE WHEN IsFinalized = 1 
	THEN (CASE WHEN NOW() <= DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) THEN 'Active' ELSE 'Completed' END)
	ELSE (CASE WHEN O.CreatedTimeStamp <= DATE_Add(NOW(), INTERVAL -30 MINUTE)
		THEN 'Expired'
		ELSE 'Bidding'
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
                        @"SELECT H.Holder as Identity, H.LitigationStatus,
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
		WHEN h.LitigationStatus = '2' THEN 'Completed Job (Litigation Answered)' 
		WHEN h.LitigationStatus = '1' THEN 'Completed Job (Litigation Initiated)' 
		WHEN h.LitigationStatus = '0' and lc.DHWasPenalized = 1 THEN 'Completed Job (Litigation Failed)' 
		WHEN h.LitigationStatus = '0' and (lc.TransactionHash is null OR lc.DHWasPenalized = 0) THEN 'Completed Job (Litigation Passed)' 
		ELSE 'Completed Job' END)
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

                    model.Timeline = connection.Query<OfferDetailedTimelineModel>($@"select Timestamp, 'Offer Created' as Name, null as 'RelatedTo'  from otcontract_holding_offercreated
where OfferId = @offerID
union all
select Timestamp, 'Offer Finalized', null from otcontract_holding_offerfinalized
where OfferId = @offerID
union all
select Timestamp, 'Litigation Initiated', HolderIdentity from otcontract_litigation_litigationinitiated
where OfferId = @offerID
union all
select Timestamp, 'Litigation Timed out', HolderIdentity from otcontract_litigation_litigationtimedout
where OfferId = @offerID
union all
select Timestamp, 'Litigation Answered', HolderIdentity from otcontract_litigation_litigationanswered
where OfferId = @offerID
union all
select Timestamp, CASE WHEN DHWasPenalized = 1 THEN 'Litigation Failed' ELSE 'Litigation Passed' END, HolderIdentity from otcontract_litigation_litigationcompleted
where OfferId = @offerID
union all
select Timestamp, 'Replacement Started', HolderIdentity from otcontract_litigation_replacementstarted
where OfferId = @offerID
union all
select Timestamp, 'Replacement Completed', ChosenHolder from otcontract_replacement_replacementcompleted
where OfferId = @offerID
union all
select Timestamp, CONCAT('Offer Paidout for ', (CAST(`Amount` AS CHAR)+0), ' {(OTHubSettings.Instance.Blockchain.Network == BlockchainNetwork.Testnet ? "ATRAC" : "TRAC")}'), Holder from otcontract_holding_paidout
where OfferId = @offerID
union all
select DATE_Add(of.Timestamp, INTERVAL + oc.HoldingTimeInMinutes MINUTE), 'Offer Completed', null from otcontract_holding_offerfinalized of
join otcontract_holding_offercreated oc on oc.OfferId = of.OfferId
where of.OfferId = @offerID 
and NOW() >= DATE_Add(of.Timestamp, INTERVAL + oc.HoldingTimeInMinutes MINUTE)", new {offerID = offerID }).OrderBy(t => t.Timestamp).ToArray();
                }

                return model;
            }
        }
    }
}