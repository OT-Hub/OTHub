using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
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

        [Route("{nodeId}/jobs")]
        [HttpGet]
        public async Task<IActionResult> GetJobs(string nodeId,
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
                limit = $"LIMIT {_page * _limit},{_limit}";
            }

            await using (var connection =
             new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var offers = connection.Query<NodeProfileDetailedModel_OfferSummary>(
                    DataHolderSql.GetJobs + $@"
{orderBy}
{limit}", new { nodeId = nodeId, OfferId_like }).ToArray();

                var total = await connection.ExecuteScalarAsync<int>(DataHolderSql.GetJobsCount, new { nodeId = nodeId, OfferId_like });

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
        public async Task<IActionResult> GetPayouts(string nodeId,
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
                limit = $"LIMIT {_page * _limit},{_limit}";
            }

            await using (var connection =
             new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                NodeProfileDetailedModel_OfferPayout[] payouts = (await connection.QueryAsync<NodeProfileDetailedModel_OfferPayout>(
                    DataHolderSql.GetPayouts + $@"
{orderBy}
{limit}", new { nodeId = nodeId, OfferId_like, TransactionHash_like })).ToArray();

                var total = await connection.ExecuteScalarAsync<int>(DataHolderSql.GetPayoutsCount, new { nodeId = nodeId, OfferId_like, TransactionHash_like });

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

        [Route("{nodeId}/profiletransfers")]
        [HttpGet]
        public async Task<IActionResult> GetProfileTransfers(string nodeId,
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
                limit = $"LIMIT {_page * _limit},{_limit}";
            }

            await using (var connection =
             new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                NodeProfileDetailedModel_ProfileTransfer[] transfers = (await connection.QueryAsync<NodeProfileDetailedModel_ProfileTransfer>(
                    DataHolderSql.GetProfileTransfers + $@"
{orderBy}
{limit}", new { nodeId = nodeId, TransactionHash_like })).ToArray();

                var total = await connection.ExecuteScalarAsync<int>(DataHolderSql.GetProfileTransfersCount, new { nodeId = nodeId, TransactionHash_like });

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

        [Route("{nodeId}/litigations")]
        [HttpGet]
        public async Task<IActionResult> GetLitigations(string nodeId,
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
                limit = $"LIMIT {_page * _limit},{_limit}";
            }

            await using (var connection =
             new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                DataHolderLitigationSummary[] litigations = (await connection.QueryAsync<DataHolderLitigationSummary>(
                    DataHolderSql.GetLitigations + $@"
{orderBy}
{limit}", new { nodeId = nodeId, OfferId_like })).ToArray();

                var total = await connection.ExecuteScalarAsync<int>(DataHolderSql.GetLitigationsCount, new { nodeId = nodeId, OfferId_like });

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

        [Route("{nodeId}")]
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
        public async Task<NodeDataHolderDetailedModel> Get([SwaggerParameter("The ERC 725 identity for the node", Required = true)] string nodeId)
        {
            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var profile = await connection.QueryFirstOrDefaultAsync<NodeDataHolderDetailedModel>(DataHolderSql.GetDetailed, new { nodeId = nodeId });

                if (profile != null)
                {
                    profile.Identities = (await connection.QueryAsync<NodeDetailedIdentity>(
                        @"SELECT i.Identity, bc.DisplayName BlockchainName, i.Stake, i.StakeReserved FROM otidentity i
JOIN blockchains bc ON bc.id = i.blockchainid
WHERE i.NodeId = @NodeId", new
                        {
                            nodeId = nodeId
                        })).ToArray();
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
        public async Task<IActionResult> GetUSDPayoutsForDataHolder([FromQuery, SwaggerParameter("The ERC 725 identity for the node", Required = true)] string identity,
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


            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var rows = (await connection.QueryAsync<PayoutUSDModel>(DataHolderSql.GetUSDPayoutsForDataHolder + Environment.NewLine + orderBy, new
                {
                    identity = identity,
                    OfferId_like
                })).ToArray();

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
    }
}
