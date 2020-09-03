﻿using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using OTHub.APIServer.Models;
using OTHub.Settings;
using ServiceStack.Text;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace OTHub.APIServer.Controllers
{
    [Route("api/nodes/[controller]")]
    public class DataHolderController : Controller
    {

        [Route("{identity}/GetJobs")]
        [HttpGet]
        public IActionResult GetJobs(string identity,
            [FromQuery, SwaggerParameter("How many offers you want to return per page", Required = true)] int _limit, 
            [FromQuery, SwaggerParameter("The page number to start from. The first page is 0.", Required = true)] int _page,
            [FromQuery] string _sort,
            [FromQuery] string _order,
            [FromQuery] bool export,
            [FromQuery] int? exportType)
        {
            _page--;

            string orderBy = String.Empty;

            switch (_sort)
            {
                case "FinalizedTimestamp":
                    orderBy = "ORDER BY FinalizedTimestamp";
                    break;
                case "HoldingTimeInMinutes":
                    orderBy = "ORDER BY HoldingTimeInMinutes";
                    break;
                case "Paidout":
                    orderBy = "ORDER BY Paidout";
                    break;
                case "TokenAmountPerHolder":
                    orderBy = "ORDER BY TokenAmountPerHolder";
                    break;
                case "EndTimestamp":
                    orderBy = "ORDER BY EndTimestamp";
                    break;
                case "Status":
                    orderBy = "ORDER BY Status";
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
                var offers = connection.Query<NodeProfileDetailedModel_OfferSummary>(
                    $@"SELECT h.OfferId, 
                h.IsOriginalHolder,
                o.FinalizedTimestamp, 
                o.HoldingTimeInMinutes, 
                o.TokenAmountPerHolder,
                DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) as EndTimestamp,
                (CASE WHEN IsFinalized = 1 
                	THEN (CASE WHEN NOW() <= DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) THEN
                	(CASE 
                		WHEN h.LitigationStatus = '4' THEN 'Data Holder Replaced' 
                		WHEN h.LitigationStatus = '3' THEN 'Data Holder is Being Replaced' 
                		WHEN h.LitigationStatus = '2' THEN 'Active (Litigation Answered)' 
                		WHEN h.LitigationStatus = '1' THEN 'Active (Litigation Initiated)' 
                		WHEN h.LitigationStatus = '0' and lc.DHWasPenalized = 1 THEN 'Litigation Failed' 
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
                END) as Status,
                (CASE WHEN COALESCE(SUM(po.Amount), 0) = O.TokenAmountPerHolder then true else false end) as Paidout,
                (CASE WHEN po.ID is null THEN 
                	(CASE WHEN (h.LitigationStatus is null OR h.LitigationStatus = 0 OR h.LitigationStatus = 1 OR h.LitigationStatus = 2)
                		 THEN true else false END) 
                ELSE false END) as CanPayout
                FROM otoffer_holders h
                join otoffer o on o.offerid = h.offerid
                left join otcontract_holding_paidout po on po.OfferID = h.OfferID and po.Holder = h.Holder
                left join otcontract_litigation_litigationcompleted lc on lc.OfferId = h.OfferId and lc.HolderIdentity = h.Holder and lc.BlockNumber = h.LitigationStatusBlockNumber and h.LitigationStatus = 0
                WHERE h.holder = @identity
                GROUP BY h.OfferID, h.Holder
{orderBy}
{limit}", new { identity = identity }).ToArray();

                var total = connection.ExecuteScalar<int>($@"SELECT COUNT(DISTINCT h.OfferId)
                FROM otoffer_holders h
                join otoffer o on o.offerid = h.offerid
                left join otcontract_holding_paidout po on po.OfferID = h.OfferID and po.Holder = h.Holder
                left join otcontract_litigation_litigationcompleted lc on lc.OfferId = h.OfferId and lc.HolderIdentity = h.Holder and lc.BlockNumber = h.LitigationStatusBlockNumber and h.LitigationStatus = 0
                WHERE h.holder = @identity",
new { identity = identity });

                HttpContext.Response.Headers["access-control-expose-headers"] = "X-Total-Count";
                HttpContext.Response.Headers["X-Total-Count"] = total.ToString();

                if (export)
                {
                    if (exportType == 0)
                    {
                        return File(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(offers)), "application/json", "jobs.json", false);
                    }
                    else if (exportType == 1)
                    {
                        return File(Encoding.UTF8.GetBytes(CsvSerializer.SerializeToCsv(offers)), "text/csv", "jobs.csv", false);
                    }
                }

                return new OkObjectResult(offers);
            }
        }

        [Route("{identity}")]
        [HttpGet]
        [SwaggerOperation(
           Summary = "Get detailed information about a data holder (this can optionally include node uptime information too)",
           Description = @"This will return most information known about the data holder including all historical data.

Data Included:
- Staked Tokens
- Management Wallet
- Deposits & Withdrawals
- Offers Won
- Payouts
- Litigations against this Data Holder
- Uptime information (not returned in the response unless specified in the parameters)"
       )]
        [SwaggerResponse(200, type: typeof(NodeDataCreatorDetailedModel))]
        [SwaggerResponse(500, "Internal server error")]
        public NodeDataHolderDetailedModel Get([SwaggerParameter("The ERC 725 identity for the node", Required = true)] string identity, [FromQuery, SwaggerParameter("A boolean flag to indicate if you want to include uptime/health information about this node in the response.", Required = false)] bool includeNodeUptime)
        {
            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var profile = connection.QueryFirstOrDefault<NodeDataHolderDetailedModel>(
                    @"select I.Identity, substring(I.NodeId, 1, 40) as NodeId, Version, COALESCE(I.Stake, 0) as StakeTokens, COALESCE(I.StakeReserved, 0) as StakeReservedTokens, 
COALESCE(I.Paidout, 0) as PaidTokens, COALESCE(I.TotalOffers, 0) as TotalWonOffers, COALESCE(I.OffersLast7Days, 0) WonOffersLast7Days, I.Approved,
(select IT.OldIdentity from OTContract_Profile_IdentityTransferred IT WHERE IT.NewIdentity = @identity) as OldIdentity,
(select IT.NewIdentity from OTContract_Profile_IdentityTransferred IT WHERE IT.OldIdentity = @identity) as NewIdentity,
I.ManagementWallet,
COALESCE(ic.TransactionHash, pc.TransactionHash) CreateTransactionHash,
COALESCE(ic.GasPrice, pc.GasPrice) CreateGasPrice,
COALESCE(ic.GasUsed, pc.GasUsed) CreateGasUsed,
(SELECT COUNT(O.OfferID) FROM OTOffer O WHERE O.DCNodeId = I.NodeId) as DCOfferCount
from OTIdentity I
left JOIN otcontract_profile_identitycreated ic on ic.NewIdentity = I.Identity
left JOIN otcontract_profile_profilecreated pc on pc.Profile = I.Identity
WHERE I.Identity = @identity", new { identity = identity });

                if (profile != null)
                {
//                    profile.Offers = connection.Query<NodeProfileDetailedModel_OfferSummary>(
//                        @"SELECT h.OfferId, 
//h.IsOriginalHolder,
//o.FinalizedTimestamp, 
//o.HoldingTimeInMinutes, 
//o.TokenAmountPerHolder,
//DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) as EndTimestamp,
//(CASE WHEN IsFinalized = 1 
//	THEN (CASE WHEN NOW() <= DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) THEN
//	(CASE 
//		WHEN h.LitigationStatus = '4' THEN 'Data Holder Replaced' 
//		WHEN h.LitigationStatus = '3' THEN 'Data Holder is Being Replaced' 
//		WHEN h.LitigationStatus = '2' THEN 'Active (Litigation Answered)' 
//		WHEN h.LitigationStatus = '1' THEN 'Active (Litigation Initiated)' 
//		WHEN h.LitigationStatus = '0' and lc.DHWasPenalized = 1 THEN 'Litigation Failed' 
//		WHEN h.LitigationStatus = '0' and (lc.TransactionHash is null OR lc.DHWasPenalized = 0) THEN 'Active (Litigation Passed)' 
//		ELSE 'Active' END)
//	 ELSE
//	(CASE 
//		WHEN h.LitigationStatus = '4' THEN 'Data Holder Replaced' 
//		WHEN h.LitigationStatus = '3' THEN 'Data Holder is Being Replaced' 
//		WHEN h.LitigationStatus = '2' THEN 'Completed (Litigation Answered)' 
//		WHEN h.LitigationStatus = '1' THEN 'Completed (Litigation Initiated)' 
//		WHEN h.LitigationStatus = '0' and lc.DHWasPenalized = 1 THEN 'Litigation Failed' 
//		WHEN h.LitigationStatus = '0' and (lc.TransactionHash is null OR lc.DHWasPenalized = 0) THEN 'Completed (Litigation Passed)' 
//		ELSE 'Completed' END)
//	  END)
//	ELSE ''
//END) as Status,
//(CASE WHEN COALESCE(SUM(po.Amount), 0) = O.TokenAmountPerHolder then true else false end) as Paidout,
//(CASE WHEN po.ID is null THEN 
//	(CASE WHEN (h.LitigationStatus is null OR h.LitigationStatus = 0 OR h.LitigationStatus = 1 OR h.LitigationStatus = 2)
//		 THEN true else false END) 
//ELSE false END) as CanPayout
//FROM otoffer_holders h
//join otoffer o on o.offerid = h.offerid
//left join otcontract_holding_paidout po on po.OfferID = h.OfferID and po.Holder = h.Holder
//left join otcontract_litigation_litigationcompleted lc on lc.OfferId = h.OfferId and lc.HolderIdentity = h.Holder and lc.BlockNumber = h.LitigationStatusBlockNumber and h.LitigationStatus = 0
//WHERE h.holder = @identity
//GROUP BY h.OfferID, h.Holder", new { identity = identity }).ToArray();

//                    profile.Payouts = connection.Query<NodeProfileDetailedModel_OfferPayout>(
//                        @"SELECT OfferID, Amount, Timestamp, TransactionHash, GasUsed, GasPrice FROM otcontract_holding_paidout
//WHERE holder = @identity", new { identity = identity }).ToArray();

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

//                    profile.Litigations = connection.Query<DataHolderLitigationSummary>(@"SELECT li.TransactionHash, li.Timestamp, li.OfferId, li.requestedBlockIndex RequestedBlockIndex, li.requestedObjectIndex RequestedObjectIndex
//FROM otcontract_litigation_litigationinitiated li
//WHERE li.HolderIdentity = @identity
//ORDER BY li.Timestamp DESC", new { identity = identity }).ToArray();

                    if (includeNodeUptime)
                    {
                        profile.NodeUptime = connection.QueryFirstOrDefault<NodeUptimeHistory>(@"SELECT
MAX(CASE WHEN Success THEN Timestamp ELSE NULL END) LastSuccess,
MAX(Timestamp) LastCheck,
SUM(CASE WHEN Success AND Timestamp >= DATE_Add(NOW(), INTERVAL -1 DAY) THEN 1 ELSE 0 END) as TotalSuccess24Hours,
SUM(CASE WHEN Success = 0 AND Timestamp >= DATE_Add(NOW(), INTERVAL -1 DAY) THEN 1 ELSE 0 END) as TotalFailed24Hours,
SUM(CASE WHEN Success AND Timestamp >= DATE_Add(NOW(), INTERVAL -7 DAY) THEN 1 ELSE 0 END) as TotalSuccess7Days,
SUM(CASE WHEN Success = 0 AND Timestamp >= DATE_Add(NOW(), INTERVAL -7 DAY) THEN 1 ELSE 0 END) as TotalFailed7Days
from OTNode_History NH
JOIN OTIdentity I ON I.NodeID = NH.NodeID
WHERE I.Identity = @identity
GROUP BY I.Identity", new { identity = identity });

                        var chartData = connection.Query<NodeUptimeChartData>(
                            @"SELECT H.Timestamp, H.Success
FROM OTNode_History H
JOIN OTIdentity I ON I.NodeId = H.NodeId
Where I.Identity = @identity
AND H.Timestamp >= DATE_Add(NOW(), INTERVAL -1 DAY)
ORDER BY H.Timestamp", new
                            {
                                identity = identity
                            }).ToArray();

                        if (chartData.Any())
                        {
                            profile.NodeUptime.ChartData = JsonConvert.SerializeObject(chartData.Select(c => new List<string> { c.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"), c.Success == true ? "Online" : "Offline", c.EndTimestamp.ToString("yyyy-MM-dd HH:mm:ss") }).ToList());
                        }
                    }
                }

                return profile;
            }
        }

        [Route("PayoutsInUSDForDataHolder")]
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets all payouts for a data holder including the equivalent prices of the payout in USD based on the time of the payout (price ticker accuracy is every 6 hours)"
        )]
        [SwaggerResponse(200, type: typeof(PayoutUSDModel[]))]
        [SwaggerResponse(500, "Internal server error")]
        public PayoutUSDModel[] GetUSDPayoutsForDataHolder([FromQuery, SwaggerParameter("The ERC 725 identity for the node", Required = true)] string identity)
        {
            using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                return connection.Query<PayoutUSDModel>(@"SELECT po.OfferID, (CAST(`Amount` AS CHAR)+0) TRACAmount, po.Timestamp PayoutTimestamp, ticker.Timestamp TickerTimestamp, ticker.Price TickerUSDPrice, ticker.Price * po.Amount USDAmount 
FROM otcontract_holding_paidout po
JOIN ticker_trac ticker ON ticker.Timestamp = (
SELECT MAX(TIMESTAMP)
FROM ticker_trac
WHERE TIMESTAMP <= po.Timestamp)
WHERE po.Holder = @identity
ORDER BY po.Timestamp DESC", new
                {
                    identity = identity
                }).ToArray();
            }
        }

        [Route("CanTryPayout")]
        [HttpGet]
        [SwaggerOperation(
            Summary = "Ask OT Hub if it thinks this identity and offer is ready to be paid out",
            Description = @"Please note that when updates are released to the ODN affecting payouts (including extra validation checks in the smart contract) this API call may not return correct information until updated.

All the logic in the smart contract for payouts has been recreated in the API so that you can check if the payout for an offer will succeed before evening trying the payout on the blockchain.
OT Hub enforces this API call is successful before letting users use Metamask to initiate the payout."
        )]
        [SwaggerResponse(200, type: typeof(BeforePayoutResult))]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<BeforePayoutResult> CanTryPayout([FromQuery, SwaggerParameter("The ERC 725 identity for the node", Required = true)] string identity, [FromQuery, SwaggerParameter("The ID of the offer", Required = true)] string offerId, [FromQuery] string holdingAddress, [FromQuery] string holdingStorageAddress, [FromQuery] string litigationStorageAddress)
        {
            return await BlockchainHelper.CanTryPayout(identity, offerId, holdingAddress, holdingStorageAddress, litigationStorageAddress);
        }

        [Route("CheckOnline")]
        [HttpGet]
        [SwaggerOperation(
            Summary = "Check if an identity is online (this works for both data holders and data creators)",
            Description = @"Please note that this API call is rate limited in multiple ways to prevent abuse. Details about the client requesting this data is also collected for preventing abuse of the service.
This API call should only be used by individuals checking their own nodes or other services/bots looking at uptime.

OT Hub already performs online checks so you may not need to use this. Have a look at /api/nodes/DataHolders/{identity} which can return uptime information."
        )]
        [SwaggerResponse(200, type: typeof(NodeOnlineResult))]
        [SwaggerResponse(500, "Internal server error")]
        public NodeOnlineResult CheckOnline([FromQuery, SwaggerParameter("The ERC 725 identity for the node", Required = true)] string identity)
        {
            var ip = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || String.IsNullOrWhiteSpace(ip))
            {
                ip = HttpContext.Connection.RemoteIpAddress.ToString();
            }

            if (identity == null || identity.Length != 42 || !identity.ToLower().StartsWith("0x"))
            {
                return new NodeOnlineResult
                {
                    Warning = true,
                    Header = "Warning!",
                    Message = "Invalid Identity provided."
                };
            }

            if (String.IsNullOrWhiteSpace(ip))
            {
                return new NodeOnlineResult
                {
                    Error = true,
                    Header = "Error!",
                    Message = "You are blocked from using the check online feature."
                };
            }

            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                int[] recentIPRequets = connection.Query<Int32>(@"SELECT 1 FROM OTNode_OnlineCheck
WHERE IPAddress = @ip AND Timestamp >= DATE_Add(NOW(), INTERVAL -4 MINUTE)
GROUP BY Identity", new { ip = ip }).ToArray();

                if (recentIPRequets.Length > 5)
                {
                    return new NodeOnlineResult
                    {
                        Warning = true,
                        Header = "Warning!",
                        Message = "You must wait a few minutes before you can check nodes are online again."
                    };
                }
                if (recentIPRequets.Sum() > 15)
                {
                    return new NodeOnlineResult
                    {
                        Warning = true,
                        Header = "Warning!",
                        Message = "You must wait a few minutes before you can check nodes are online again."
                    };
                }

                DateTime? lastRequestDateForIdentity = connection.ExecuteScalar<DateTime?>(@"SELECT Timestamp FROM OTNode_OnlineCheck
WHERE Identity = @identity
ORDER BY Timestamp DESC LIMIT 1", new { identity = identity });

                if (lastRequestDateForIdentity.HasValue)
                {
                    var diff = DateTime.UtcNow - lastRequestDateForIdentity.Value;
                    if (diff.TotalSeconds < 30)
                    {
                        return new NodeOnlineResult
                        {
                            Warning = true,
                            Header = "Warning!",
                            Message = "You must wait another " + (30 - (int)diff.TotalSeconds) + " seconds before you can check this node is online."
                        };
                    }
                }

                var row = connection.QueryFirstOrDefault(@"SELECT IP.Hostname, IP.Port FROM OTIdentity I
JOIN OTNode_IPInfo IP on IP.NodeID = I.NodeID
WHERE I.Identity = @identity", new { identity = identity });

                if (row == null)
                {
                    return new NodeOnlineResult
                    {
                        Warning = true,
                        Header = "Warning!",
                        Message = "OT Hub has not found this node yet. Please check back later!"
                    };
                }

                connection.Execute(
                    @"INSERT INTO otnode_onlinecheck (IPAddress, Identity, Timestamp) VALUES (@ip, @identity, @timestamp)",
                    new
                    {
                        ip = ip,
                        identity = identity,
                        timestamp = DateTime.UtcNow
                    });

                string hostname = row.Hostname;
                int port = row.Port;
                bool success = false;

                try
                {
                    string url = $"https://{hostname}:{port}/";

                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.Timeout = 20000;
                    request.AllowAutoRedirect = false;
                    request.ServerCertificateValidationCallback = delegate (object sender, X509Certificate certificate,
                        X509Chain chain, SslPolicyErrors errors)
                    {

                        success = true;

                        return true;
                    };

                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                }
                catch (Exception ex)
                {

                }

                if (success)
                {
                    return new NodeOnlineResult
                    {
                        Success = true,
                        Header = "Success!",
                        Message = "Your node responded successfully to the online check."
                    };
                }

                return new NodeOnlineResult
                {
                    Error = true,
                    Message = "Your node did not respond to the online check. If you have recently changed your node IP address this may take time to propagate to OT Hub.",
                    Header = "Error!"
                };
            }
        }
    }
}