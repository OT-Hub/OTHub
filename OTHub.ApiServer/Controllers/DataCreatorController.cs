using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Newtonsoft.Json;
using OTHub.APIServer.Sql;
using OTHub.APIServer.Sql.Models.Jobs;
using OTHub.APIServer.Sql.Models.Nodes;
using OTHub.APIServer.Sql.Models.Nodes.DataCreator;
using OTHub.Settings;
using ServiceStack.Text;
using Swashbuckle.AspNetCore.Annotations;

namespace OTHub.APIServer.Controllers
{
    [Route("api/nodes/[controller]")]
    public class DataCreatorController : Controller
    {
        [Route("{nodeId}")]
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
        public async Task<NodeDataCreatorDetailedModel> Get([SwaggerParameter("The ERC 725 identity for the data creator.", Required = true)] string nodeId)
        {
            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var profile = await connection.QueryFirstOrDefaultAsync<NodeDataCreatorDetailedModel>(
                    DataCreatorSql.GetDetailed, new { nodeId = nodeId });

                if (profile != null)
                {
                    profile.Identities = (await connection.QueryAsync<NodeDetailedIdentity>(
                        @"SELECT i.Identity, bc.BlockchainName, bc.NetworkName FROM otidentity i
JOIN blockchains bc ON bc.id = i.blockchainid
WHERE i.NodeId = @NodeId", new
                        {
                            nodeId = nodeId
                        })).ToArray();
                }

                return profile;

            }
        }

        [Route("{nodeId}/Jobs")]
        [HttpGet]
        public async Task<IActionResult> GetJobs(
            string nodeId,
            [FromQuery]
            int _limit,
            [FromQuery]
            int _page,
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
                case "CreatedTimestamp":
                    orderBy = "ORDER BY CreatedTimestamp";
                    break;
                case "FinalizedTimestamp":
                    orderBy = "ORDER BY FinalizedTimestamp";
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
                var offers = (await connection.QueryAsync<OfferSummaryModel>(
                    DataCreatorSql.GetJobs + $@"
{orderBy}
{limit}", new { nodeId = nodeId, OfferId_like })).ToArray();

                var total = await connection.ExecuteScalarAsync<int>(DataCreatorSql.GetJobsCount, new { nodeId = nodeId, OfferId_like });

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

        [Route("{nodeId}/ProfileTransfers")]
        [HttpGet]
        public async Task<IActionResult> GetProfileTransfers(
            string nodeId,
            [FromQuery] string TransactionHash_like,
            [FromQuery]
            int _limit,
            [FromQuery]
            int _page,
            [FromQuery] string _sort,
            [FromQuery] string _order,
            [FromQuery] bool export,
            [FromQuery] int? exportType)
        {
            _page--;


            if (TransactionHash_like != null && TransactionHash_like.Length > 200)
            {
                TransactionHash_like = null;
            }

            string orderBy = String.Empty;

            switch (_sort)
            {
                case "Amount":
                    orderBy = "ORDER BY Amount";
                    break;
                case "Timestamp":
                    orderBy = "ORDER BY Timestamp";
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
                var transfers = (await connection.QueryAsync<NodeProfileDetailedModel_ProfileTransfer>(
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
                        return File(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(transfers)), "application/json", "profiletransfers.json", false);
                    }
                    else if (exportType == 1)
                    {
                        return File(Encoding.UTF8.GetBytes(CsvSerializer.SerializeToCsv(transfers)), "text/csv", "profiletransfers.csv", false);
                    }
                }

                return new OkObjectResult(transfers);
            }
        }

        [Route("{nodeId}/Litigations")]
        [HttpGet]
        public async Task<IActionResult> GetLitigations(
            string nodeId,
            [FromQuery]
            int _limit,
            [FromQuery]
            int _page,
            [FromQuery] string _sort,
            [FromQuery] string _order,
            [FromQuery] bool export,
            [FromQuery] int? exportType,
            [FromQuery] string OfferId_like,
            [FromQuery] string HolderIdentity_like)
        {
            _page--;

            if (OfferId_like != null && OfferId_like.Length > 200)
            {
                OfferId_like = null;
            }

            if (HolderIdentity_like != null && HolderIdentity_like.Length > 200)
            {
                HolderIdentity_like = null;
            }

            string orderBy = String.Empty;

            switch (_sort)
            {
                case "Timestamp":
                    orderBy = "ORDER BY Timestamp";
                    break;
                case "OfferId":
                    orderBy = "ORDER BY OfferId";
                    break;
                case "RequestedObjectIndex":
                    orderBy = "ORDER BY RequestedObjectIndex";
                    break;
                case "RequestedBlockIndex":
                    orderBy = "ORDER BY RequestedBlockIndex";
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
                var litigations = (await connection.QueryAsync<DataCreatorLitigationSummary>(
                    DataCreatorSql.GetLitigations + $@"
{orderBy}
{limit}", new { nodeId = nodeId, OfferId_like, HolderIdentity_like })).ToArray();

                var total = await connection.ExecuteScalarAsync<int>(DataCreatorSql.GetLitigationsCount, new { nodeId = nodeId, OfferId_like, HolderIdentity_like });

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
    }
}