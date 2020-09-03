using System;
using System.Linq;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using OTHub.APIServer.Models;
using OTHub.Settings;
using ServiceStack.Text;
using Swashbuckle.AspNetCore.Annotations;

namespace OTHub.APIServer.Controllers
{
    [Route("api/nodes/[controller]")]
    public class DataCreatorsController : Controller
    {
        [HttpGet]
        [SwaggerOperation(
            Summary = "Get all data creators (no paging)",
            Description = @"This will return a summary of information about each data creator.

If you want to get more information about a specific data creator you should use /api/nodes/DataCreators/{identity} API call"
        )]
        [SwaggerResponse(200, type: typeof(NodeDataCreatorSummaryModel[]))]
        [SwaggerResponse(500, "Internal server error")]
        public IActionResult Get([FromQuery, 
                                                  SwaggerParameter("The filter to use for the ERC version of the identity. The ODN launched with version 0 for. In Decemember 2018 all nodes upgraded their identities (which also generated them new identities) which are version 1. The OT Hub website only shows users version 1 identities.", Required = true)]int ercVersion, 
            [FromQuery, SwaggerParameter("Filter the results to only include identities listed. Multiple identities can be provided by seperating them with &. Up to 50 can be provided maximum.", Required = false)] string[] identity,
            [FromQuery, SwaggerParameter("How many offers you want to return per page", Required = true)] int _limit, 
            [FromQuery, SwaggerParameter("The page number to start from. The first page is 0.", Required = true)] int _page,
            [FromQuery] string Identity_like,
            [FromQuery] string _sort,
            [FromQuery] string _order,
            [FromQuery] bool export,
            [FromQuery] int? exportType)
        {
            if (identity.Length >= 50 || identity.Any(i => i.Length >= 50 || !i.StartsWith("0x") || i.Contains(" ")))
            {
                return new OkObjectResult(new NodeDataCreatorSummaryModel[0]);
            }

            _page--;

            if (Identity_like != null && Identity_like.Length > 200)
            {
                Identity_like = null;
            }

            string orderBy = String.Empty;

            switch (_sort)
            {
                case "OffersTotal":
                    orderBy = "ORDER BY OffersTotal";
                    break;
                case "OffersLast7Days":
                    orderBy = "ORDER BY OffersLast7Days";
                    break;
                case "AvgDataSetSizeKB":
                    orderBy = "ORDER BY AvgDataSetSizeKB";
                    break;
                case "AvgHoldingTimeInMinutes":
                    orderBy = "ORDER BY AvgHoldingTimeInMinutes";
                    break;
                case "StakeReservedTokens":
                    orderBy = "ORDER BY StakeReservedTokens";
                    break;
                case "StakeTokens":
                    orderBy = "ORDER BY StakeTokens";
                    break;
                case "AvgTokenAmountPerHolder":
                    orderBy = "ORDER BY AvgTokenAmountPerHolder";
                    break;
                case "LastJob":
                    orderBy = "ORDER BY LastJob";
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
                NodeDataCreatorSummaryModel[] summary = connection.Query<NodeDataCreatorSummaryModel>(
                    $@"select I.Identity, substring(I.NodeId, 1, 40) as NodeId, Version, 
COALESCE(I.Stake, 0) as StakeTokens, COALESCE(I.StakeReserved, 0) as StakeReservedTokens,
I.Approved,
Count(O.OfferId) OffersTotal,
SUM(CASE WHEN O.CreatedTimestamp >= Date_Add(NOW(), INTERVAL -7 DAY) THEN 1 ELSE 0 END) OffersLast7Days,
ROUND(AVG(O.DataSetSizeInBytes) / 1000) AvgDataSetSizeKB,
ROUND(AVG(O.HoldingTimeInMinutes)) AvgHoldingTimeInMinutes,
ROUND(AVG(O.TokenAmountPerHolder)) AvgTokenAmountPerHolder,
x.Timestamp as CreatedTimestamp,
COALESCE(MAX(O.FinalizedTimestamp), MAX(CreatedTimestamp)) LastJob
from OTIdentity I
JOIN OTOffer O ON O.DCNodeId = I.NodeId
JOIN (SELECT I.Identity, COALESCE(PCB.Timestamp, ICB.Timestamp) as Timestamp FROM OTIdentity I
LEFT JOIN OTContract_Profile_ProfileCreated PC ON PC.Profile = I.Identity
LEFT JOIN EthBlock PCB ON PCB.BlockNumber = PC.BlockNumber
LEFT JOIN OTContract_Profile_IdentityCreated IC ON IC.NewIdentity = I.Identity
LEFT JOIN EthBlock ICB ON ICB.BlockNumber = IC.BlockNumber
WHERE IC.NewIdentity is not null OR PC.Profile is not null) x on x.Identity = I.Identity
WHERE {(identity.Any() ? "I.Identity in @identity AND" : "")} Version = @version
AND (@Identity_like is null OR I.Identity = @Identity_like)
GROUP BY I.Identity
{orderBy}
{limit}", new { version = ercVersion, identity, Identity_like }).ToArray();

                var total = connection.ExecuteScalar<int>($@"select COUNT(DISTINCT I.Identity)
from OTIdentity I
JOIN OTOffer O ON O.DCNodeId = I.NodeId
JOIN (SELECT I.Identity, COALESCE(PCB.Timestamp, ICB.Timestamp) as Timestamp FROM OTIdentity I
LEFT JOIN OTContract_Profile_ProfileCreated PC ON PC.Profile = I.Identity
LEFT JOIN EthBlock PCB ON PCB.BlockNumber = PC.BlockNumber
LEFT JOIN OTContract_Profile_IdentityCreated IC ON IC.NewIdentity = I.Identity
LEFT JOIN EthBlock ICB ON ICB.BlockNumber = IC.BlockNumber
WHERE IC.NewIdentity is not null OR PC.Profile is not null) x on x.Identity = I.Identity
WHERE {(identity.Any() ? "I.Identity in @identity AND" : "")} Version = @version
AND (@Identity_like is null OR I.Identity = @Identity_like)",
new { version = ercVersion, identity, Identity_like });

                HttpContext.Response.Headers["access-control-expose-headers"] = "X-Total-Count";
                HttpContext.Response.Headers["X-Total-Count"] = total.ToString();

                if (export)
                {
                    if (exportType == 0)
                    {
                        return File(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(summary)), "application/json", "datacreators.json", false);
                    }
                    else if (exportType == 1)
                    {
                        return File(Encoding.UTF8.GetBytes(CsvSerializer.SerializeToCsv(summary)), "text/csv", "datacreators.csv", false);
                    }
                }

                return new OkObjectResult(summary);
            }
        }

        [Route("{identity}")]
        [HttpGet]
        [SwaggerOperation(
            Summary = "Get detailed information about a data creator",
            Description = @"This will return most information known about the data creator including all historical data.

Data Included:
- Staked Tokens
- Management Wallet
- Deposits & Withdrawals
- Offers Created
- Litigations Started against Data Holders"
        )]
        [SwaggerResponse(200, type: typeof(NodeDataCreatorDetailedModel))]
        [SwaggerResponse(500, "Internal server error")]
        public NodeDataCreatorDetailedModel Get([SwaggerParameter("The ERC 725 identity for the data creator.", Required = true)]string identity)
        {
            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var profile = connection.QueryFirstOrDefault<NodeDataCreatorDetailedModel>(
                    @"select I.Identity, substring(I.NodeId, 1, 40) as NodeId, Version, COALESCE(I.Stake, 0) as StakeTokens, COALESCE(I.StakeReserved, 0) as StakeReservedTokens, 
 I.Approved,
(select IT.OldIdentity from OTContract_Profile_IdentityTransferred IT WHERE IT.NewIdentity = @identity) as OldIdentity,
(select IT.NewIdentity from OTContract_Profile_IdentityTransferred IT WHERE IT.OldIdentity = @identity) as NewIdentity,
I.ManagementWallet,
COALESCE(ic.TransactionHash, pc.TransactionHash) CreateTransactionHash,
COALESCE(ic.GasPrice, pc.GasPrice) CreateGasPrice,
COALESCE(ic.GasUsed, pc.GasUsed) CreateGasUsed
from OTIdentity I
left JOIN otcontract_profile_identitycreated ic on ic.NewIdentity = I.Identity
left JOIN otcontract_profile_profilecreated pc on pc.Profile = I.Identity
JOIN otoffer O ON O.DCNodeId = I.NodeId
WHERE I.Identity = @identity
GROUP BY I.Identity", new { identity = identity });

                if (profile != null)
                {
//                    profile.Offers = connection.Query<OfferSummaryModel>(
//                        @"SELECT o.OfferId, o.CreatedTimestamp as Timestamp, o.DataSetSizeInBytes, o.TokenAmountPerHolder, o.HoldingTimeInMinutes, o.IsFinalized,
//(CASE WHEN o.IsFinalized = 1 
//	THEN (CASE WHEN NOW() <= DATE_Add(o.FinalizedTimeStamp, INTERVAL +o.HoldingTimeInMinutes MINUTE) THEN 'Active' ELSE 'Completed' END)
//	ELSE (CASE WHEN o.CreatedTimeStamp <= DATE_Add(NOW(), INTERVAL -30 MINUTE)
//		THEN 'Not Started'
//		ELSE 'Not Started'
//	END)
//END) as Status,
//(CASE WHEN o.IsFinalized = 1  THEN DATE_Add(o.FinalizedTimeStamp, INTERVAL +o.HoldingTimeInMinutes MINUTE) ELSE NULL END) as EndTimestamp
//FROM OTOffer o
//join otidentity i on i.NodeId = o.DCNodeId
//join otcontract_holding_offercreated oc on oc.OfferID = o.OfferID
//left join otcontract_holding_offerfinalized of on of.OfferID = o.OfferID
//WHERE i.Identity = @identity", new {identity = identity}).ToArray();


//                    profile.ProfileTransfers = connection.Query<NodeProfileDetailedModel_ProfileTransfer>(
//                        @"SELECT TransactionHash, AmountDeposited as Amount, b.Timestamp, t.GasPrice, t.GasUsed FROM otcontract_profile_tokensdeposited t
//JOIN ethblock b on b.BlockNumber = t.BlockNumber
//where t.Profile = @identity
//union
//SELECT TransactionHash, AmountWithdrawn * - 1 as Amount, b.Timestamp, t.GasPrice, t.GasUsed FROM otcontract_profile_tokenswithdrawn t
//JOIN ethblock b on b.BlockNumber = t.BlockNumber
//where t.Profile = @identity
//union 
//select pc.TransactionHash, pc.InitialBalance as Amount, b.Timestamp, pc.GasPrice, pc.GasUsed  from otcontract_profile_profilecreated pc
//join ethblock b on b.BlockNumber = pc.BlockNumber
//WHERE pc.Profile = @identity", new { identity = identity }).ToArray();

//                    profile.Litigations = connection.Query<DataCreatorLitigationSummary>(@"SELECT li.TransactionHash, li.Timestamp, li.OfferId, li.HolderIdentity, li.requestedBlockIndex RequestedBlockIndex, li.requestedObjectIndex RequestedObjectIndex
//FROM otcontract_litigation_litigationinitiated li
//JOIN OTOffer O ON O.OfferId = li.OfferId
//JOIN OTIdentity I ON I.NodeId = O.DCNodeId
//WHERE I.Identity = @identity
//ORDER BY li.Timestamp DESC", new { identity = identity }).ToArray();
                }

                return profile;

            }
        }
    }
}