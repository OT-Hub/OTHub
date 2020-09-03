using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using OTHub.APIServer.Models;
using OTHub.Settings;
using ServiceStack.Text;
using Swashbuckle.AspNetCore.Annotations;

namespace OTHub.APIServer.Controllers
{
    [Route("api/nodes/[controller]")]
    public class DataHoldersController : Controller
    {
        [HttpGet]
        [SwaggerOperation(
            Summary = "Get all data holders (no paging)",
            Description = @"This will return a summary of information about each data holder.

If you want to get more information about a specific data holder you should use /api/nodes/DataHolders/{identity} API call"
        )]
        [SwaggerResponse(200, type: typeof(NodeDataHolderSummaryModel[]))]
        [SwaggerResponse(500, "Internal server error")]
        public IActionResult Get([FromQuery,
                                                 SwaggerParameter("The filter to use for the ERC version of the identity. The ODN launched with version 0 for. In Decemember 2018 all nodes upgraded their identities (which also generated them new identities) which are version 1. The OT Hub website only shows users version 1 identities.", Required = true)]int ercVersion,
            [FromQuery, SwaggerParameter("Filter the results to only include identities listed. Multiple identities can be provided by seperating them with &. Up to 50 can be provided maximum.", Required = false)] string[] identity, 
            [FromQuery, SwaggerParameter("Filter the results to only include identities with the specified management wallet address. Multiple management wallet addresses can be provided by seperating them with &. Up to 50 can be provided maximum.", Required = false)] string[] managementWallet,
            [FromQuery, SwaggerParameter("How many offers you want to return per page", Required = true)] int _limit, [FromQuery, SwaggerParameter("The page number to start from. The first page is 0.", Required = true)] int _page,
            [FromQuery] string Identity_like,
            [FromQuery] string _sort,
            [FromQuery] string _order,
            [FromQuery] bool export,
            [FromQuery] int? exportType)
        {
            _page--;

            if (identity.Length >= 50 || identity.Any(i => i.Length >= 50 || !i.StartsWith("0x") || i.Contains(" ")))
            {
                return new OkObjectResult(new NodeDataHolderSummaryModel[0]);
            }

            if (managementWallet.Length >= 50 || managementWallet.Any(i => i.Length >= 50 || !i.StartsWith("0x") || i.Contains(" ")))
            {
                return new OkObjectResult(new NodeDataHolderSummaryModel[0]);
            }

            if (Identity_like != null && Identity_like.Length > 200)
            {
                Identity_like = null;
            }

            string orderBy = String.Empty;

            switch (_sort)
            {
                case "WonOffersLast7Days":
                    orderBy = "ORDER BY WonOffersLast7Days";
                    break;
                case "TotalWonOffers":
                    orderBy = "ORDER BY TotalWonOffers";
                    break;
                case "ActiveOffers":
                    orderBy = "ORDER BY ActiveOffers";
                    break;
                case "PaidTokens":
                    orderBy = "ORDER BY PaidTokens";
                    break;
                case "StakeReservedTokens":
                    orderBy = "ORDER BY StakeReservedTokens";
                    break;
                case "StakeTokens":
                    orderBy = "ORDER BY StakeTokens";
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

            //History is expensive which can be improved on in the future. I think I fixed the performance but better to be safe
            bool includeHistory = true;

            if (identity.Any())
            {
                includeHistory = true;
            }

            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                NodeDataHolderSummaryModel[] summary = connection.Query<NodeDataHolderSummaryModel>(
                    $@"select I.Identity,
SUM(CASE WHEN O.IsFinalized = 1 
	THEN (CASE WHEN NOW() <= DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) THEN 1 ELSE 0 END)
	ELSE 0
END) as ActiveOffers,
 substring(I.NodeId, 1, 40) as NodeId, I.Version, COALESCE(I.Stake, 0) as StakeTokens,
COALESCE(I.StakeReserved, 0) as StakeReservedTokens, COALESCE(I.Paidout, 0) as PaidTokens, COALESCE(I.TotalOffers, 0) as TotalWonOffers, 
COALESCE(I.OffersLast7Days, 0) WonOffersLast7Days, I.Approved
{(includeHistory ? ",MAX(CASE WHEN H.Success = 1 THEN H.Timestamp ELSE NULL END) LastSeenOnline," : "")}
{(includeHistory ? "MAX(CASE WHEN H.Success = 0 THEN H.Timestamp ELSE NULL END) LastSeenOffline" : "")}
from OTIdentity I
LEFT JOIN OTOffer_Holders OH ON OH.Holder = I.Identity
LEFT JOIN OTOffer O ON O.OfferID = OH.OfferID
{(includeHistory ? "LEFT JOIN (SELECT NodeID, Success, MAX(TIMESTAMP) Timestamp FROM otnode_history GROUP BY NodeID, Success) H ON H.NodeID = I.NodeID" : "")}
WHERE (@Identity_like IS NULL OR I.Identity = @Identity_like) AND {(identity.Any() ? "I.Identity in @identity AND" : "")} {(managementWallet.Any() ? "I.ManagementWallet in @managementWallet AND" : "")} I.Version = @version
GROUP BY I.Identity
{orderBy}
{limit}", new { version = ercVersion, identity, managementWallet, Identity_like }).ToArray();

                var total = connection.ExecuteScalar<int>($@"select COUNT(DISTINCT I.Identity)
from OTIdentity I
LEFT JOIN OTOffer_Holders OH ON OH.Holder = I.Identity
LEFT JOIN OTOffer O ON O.OfferID = OH.OfferID
WHERE (@Identity_like IS NULL OR I.Identity = @Identity_like) AND {(identity.Any() ? "I.Identity in @identity AND" : "")} {(managementWallet.Any() ? "I.ManagementWallet in @managementWallet AND" : "")} I.Version = @version", 
new { version = ercVersion, identity, managementWallet, Identity_like });

                HttpContext.Response.Headers["access-control-expose-headers"] = "X-Total-Count";
                HttpContext.Response.Headers["X-Total-Count"] = total.ToString();

                if (export)
                {
                    if (exportType == 0)
                    {
                        return File(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(summary)), "application/json", "dataholders.json", false);
                    }
                    else if (exportType == 1)
                    {
                        return File(Encoding.UTF8.GetBytes(CsvSerializer.SerializeToCsv(summary)), "text/csv", "dataholders.csv", false);
                    }
                }

                return new OkObjectResult(summary);
            }
        }

        [Route("GetManagementWalletForIdentity")]
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the management wallet address for a specific identity"
        )]
        [SwaggerResponse(200, type: typeof(String))]
        [SwaggerResponse(500, "Internal server error")]
        public String GetManagementWalletForIdentity([FromQuery, SwaggerParameter("The ERC 725 identity for the node", Required = true)] string identity)
        {
            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                return connection.ExecuteScalar<string>(@"select managementwallet from otidentity
where identity = @identity", new {identity = identity});
            }
        }

        [Route("GetRecentPayoutGasPrices")]
        [HttpGet]
        [SwaggerOperation(
            Summary = "Get recent gas prices used by other data holders when requested payouts in the last 7 days"
        )]
        [SwaggerResponse(200, type: typeof(RecentPayoutGasPrice[]))]
        [SwaggerResponse(500, "Internal server error")]
        public RecentPayoutGasPrice[] GetRecentPayoutGasPrices()
        {
            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                return connection.Query<RecentPayoutGasPrice>(@"
SELECT GasPrice, AVG(GasUsed) GasUsed, COUNT(*) TotalCount FROM otcontract_holding_paidout
WHERE Timestamp >= DATE_Add(NOW(), INTERVAL -7 DAY)
GROUP BY GasPrice
ORDER BY GasPrice").ToArray();
            }
        }
    }
}