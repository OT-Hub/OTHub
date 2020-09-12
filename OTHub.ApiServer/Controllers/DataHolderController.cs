using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
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
using OTHub.APIServer.Ethereum;
using OTHub.APIServer.Sql.Models.Nodes;
using OTHub.APIServer.Sql.Models.Nodes.DataCreator;
using OTHub.APIServer.Sql.Models.Nodes.DataHolder;
using OTHub.APIServer.Sql;
using System.Diagnostics;

namespace OTHub.APIServer.Controllers
{
    [Route("api/nodes/[controller]")]
    public class DataHolderController : Controller
    {

        [Route("{identity}/jobs")]
        [HttpGet]
        public IActionResult GetJobs(string identity,
            [FromQuery, SwaggerParameter("How many offers you want to return per page", Required = true)] int _limit, 
            [FromQuery, SwaggerParameter("The page number to start from. The first page is 0.", Required = true)] int _page,
            [FromQuery] string _sort,
            [FromQuery] string _order,
            [FromQuery] bool export,
            [FromQuery] int? exportType,
            [FromQuery] string OfferId_like)
        {
            _page--;

            if (OfferId_like != null && OfferId_like.Length > 200)
            {
                OfferId_like = null;
            }

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
                    DataHolderSql.GetJobs + $@"
{orderBy}
{limit}", new { identity = identity, OfferId_like }).ToArray();

                var total = connection.ExecuteScalar<int>(DataHolderSql.GetJobsCount, new { identity = identity, OfferId_like });

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

        [Route("{identity}/payouts")]
        [HttpGet]
        public IActionResult GetPayouts(string identity,
            [FromQuery] int _limit,
            [FromQuery] int _page,
            [FromQuery] string _sort,
            [FromQuery] string _order,
            [FromQuery] bool export,
            [FromQuery] int? exportType,
            [FromQuery] string OfferId_like,
            [FromQuery] string TransactionHash_like)
        {
            _page--;

            if (OfferId_like != null && OfferId_like.Length > 200)
            {
                OfferId_like = null;
            }

            if (TransactionHash_like != null && TransactionHash_like.Length > 200)
            {
                TransactionHash_like = null;
            }

            string orderBy = String.Empty;

            switch (_sort)
            {
                case "Timestamp":
                    orderBy = "ORDER BY Timestamp";
                    break;
                case "Amount":
                    orderBy = "ORDER BY Amount";
                    break;
                case "GasUsed":
                    orderBy = "ORDER BY GasUsed";
                    break;
                case "GasPrice":
                    orderBy = "ORDER BY GasPrice";
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
                NodeProfileDetailedModel_OfferPayout[] payouts = connection.Query<NodeProfileDetailedModel_OfferPayout>(
                    DataHolderSql.GetPayouts + $@"
{orderBy}
{limit}", new { identity = identity, OfferId_like, TransactionHash_like }).ToArray();

                var total = connection.ExecuteScalar<int>(DataHolderSql.GetPayoutsCount, new { identity = identity, OfferId_like, TransactionHash_like });

                HttpContext.Response.Headers["access-control-expose-headers"] = "X-Total-Count";
                HttpContext.Response.Headers["X-Total-Count"] = total.ToString();

                if (export)
                {
                    if (exportType == 0)
                    {
                        return File(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payouts)), "application/json", "payouts.json", false);
                    }
                    else if (exportType == 1)
                    {
                        return File(Encoding.UTF8.GetBytes(CsvSerializer.SerializeToCsv(payouts)), "text/csv", "payouts.csv", false);
                    }
                }

                return new OkObjectResult(payouts);
            }
        }

        [Route("{identity}/profiletransfers")]
        [HttpGet]
        public IActionResult GetProfileTransfers(string identity,
    [FromQuery] int _limit,
    [FromQuery] int _page,
    [FromQuery] string _sort,
    [FromQuery] string _order,
    [FromQuery] bool export,
    [FromQuery] int? exportType,
    [FromQuery] string TransactionHash_like)
        {
            _page--;

            if (TransactionHash_like != null && TransactionHash_like.Length > 200)
            {
                TransactionHash_like = null;
            }

            string orderBy = String.Empty;

            switch (_sort)
            {
                case "Timestamp":
                    orderBy = "ORDER BY Timestamp";
                    break;
                case "GasUsed":
                    orderBy = "ORDER BY GasUsed";
                    break;
                case "GasPrice":
                    orderBy = "ORDER BY GasPrice";
                    break;
                case "Amount":
                    orderBy = "ORDER BY Amount";
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
                NodeProfileDetailedModel_ProfileTransfer[] transfers = connection.Query<NodeProfileDetailedModel_ProfileTransfer>(
                    DataHolderSql.GetProfileTransfers + $@"
{orderBy}
{limit}", new { identity = identity, TransactionHash_like }).ToArray();

                var total = connection.ExecuteScalar<int>(DataHolderSql.GetProfileTransfersCount, new { identity = identity, TransactionHash_like });

                HttpContext.Response.Headers["access-control-expose-headers"] = "X-Total-Count";
                HttpContext.Response.Headers["X-Total-Count"] = total.ToString();

                if (export)
                {
                    if (exportType == 0)
                    {
                        return File(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(transfers)), "application/json", "transfers.json", false);
                    }
                    else if (exportType == 1)
                    {
                        return File(Encoding.UTF8.GetBytes(CsvSerializer.SerializeToCsv(transfers)), "text/csv", "transfers.csv", false);
                    }
                }

                return new OkObjectResult(transfers);
            }
        }

        [Route("{identity}/litigations")]
        [HttpGet]
        public IActionResult GetLitigations(string identity,
    [FromQuery] int _limit,
    [FromQuery] int _page,
    [FromQuery] string _sort,
    [FromQuery] string _order,
    [FromQuery] bool export,
    [FromQuery] int? exportType,
    [FromQuery] string OfferId_like)
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
                case "RequestedObjectIndex":
                    orderBy = "ORDER BY RequestedObjectIndex";
                    break;
                case "RequestedBlockIndex":
                    orderBy = "ORDER BY RequestedBlockIndex";
                    break;
                case "OfferId":
                    orderBy = "ORDER BY OfferId";
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
                DataHolderLitigationSummary[] litigations = connection.Query<DataHolderLitigationSummary>(
                    DataHolderSql.GetLitigations + $@"
{orderBy}
{limit}", new { identity = identity, OfferId_like }).ToArray();

                var total = connection.ExecuteScalar<int>(DataHolderSql.GetLitigationsCount, new { identity = identity, OfferId_like });

                HttpContext.Response.Headers["access-control-expose-headers"] = "X-Total-Count";
                HttpContext.Response.Headers["X-Total-Count"] = total.ToString();

                if (export)
                {
                    if (exportType == 0)
                    {
                        return File(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(litigations)), "application/json", "litigations.json", false);
                    }
                    else if (exportType == 1)
                    {
                        return File(Encoding.UTF8.GetBytes(CsvSerializer.SerializeToCsv(litigations)), "text/csv", "litigations.csv", false);
                    }
                }

                return new OkObjectResult(litigations);
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
        public NodeDataHolderDetailedModel Get([SwaggerParameter("The ERC 725 identity for the node", Required = true)] string identity,
            [FromQuery, SwaggerParameter("A boolean flag to indicate if you want to include uptime/health information about this node in the response.", Required = false)]
        bool includeNodeUptime)
        {
            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var profile = connection.QueryFirstOrDefault<NodeDataHolderDetailedModel>(DataHolderSql.GetDetailed, new { identity = identity });

                if (profile != null)
                {
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
AND H.Timestamp >= DATE_Add(NOW(), INTERVAL -3 DAY)
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
        public IActionResult GetUSDPayoutsForDataHolder([FromQuery, SwaggerParameter("The ERC 725 identity for the node", Required = true)] string identity,
                [FromQuery] string _sort,
    [FromQuery] string _order,
    [FromQuery] bool export,
    [FromQuery] int? exportType,
    [FromQuery] string OfferId_like)
        {
            if (OfferId_like != null && OfferId_like.Length > 200)
            {
                OfferId_like = null;
            }

            string orderBy = String.Empty;

            switch (_sort)
            {
                case "TRACAmount":
                    orderBy = "ORDER BY TRACAmount";
                    break;
                case "USDAmount":
                    orderBy = "ORDER BY USDAmount";
                    break;
                case "PayoutTimestamp":
                    orderBy = "ORDER BY PayoutTimestamp";
                    break;
                case "TickerTimestamp":
                    orderBy = "ORDER BY TickerTimestamp";
                    break;
                case "TickerUSDPrice":
                    orderBy = "ORDER BY TickerUSDPrice";
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


            using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var rows = connection.Query<PayoutUSDModel>(DataHolderSql.GetUSDPayoutsForDataHolder + Environment.NewLine + orderBy, new
                {
                    identity = identity,
                    OfferId_like
                }).ToArray();

                if (export)
                {
                    if (exportType == 0)
                    {
                        return File(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(rows)), "application/json", "payoutsinusd.json", false);
                    }
                    else if (exportType == 1)
                    {
                        return File(Encoding.UTF8.GetBytes(CsvSerializer.SerializeToCsv(rows)), "text/csv", "payoutsinusd.csv", false);
                    }
                }

                return new OkObjectResult(rows);
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

                Stopwatch sw = new Stopwatch();
                sw.Start();

                DateTime now = DateTime.Now;

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
                finally
                {
                    sw.Stop();
                }

                var nodeId = connection.ExecuteScalar<String>("Select NodeID FROM OTIdentity WHERE Identity = @identity", new
                {
                    identity
                });

                connection.Execute(
    @"INSERT INTO OTNode_History(NodeId, Timestamp, Success, Duration)
VALUES(@NodeId, @Timestamp, @Success, @Duration)",
    new
    {
        NodeId = nodeId,
        Timestamp = now,
        Duration = sw.ElapsedMilliseconds,
        Success = success
    });


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
