using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Newtonsoft.Json;
using OTHub.APIServer.Sql;
using OTHub.APIServer.Sql.Models;
using OTHub.APIServer.Sql.Models.Nodes;
using OTHub.APIServer.Sql.Models.Nodes.DataHolders;
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
        public async Task<IActionResult> Get(
            [FromQuery, SwaggerParameter("How many offers you want to return per page", Required = true)]
            int _limit,
            [FromQuery, SwaggerParameter("The page number to start from. The first page is 0.", Required = true)]
            int _page,
            [FromQuery] string NodeId_like,
            [FromQuery] string _sort,
            [FromQuery] string _order,
            [FromQuery] bool export,
            [FromQuery] int? exportType,
            [FromQuery] bool restrictToMyNodes)
        {
            _page--;


            if (NodeId_like != null && NodeId_like.Length > 200)
            {
                NodeId_like = null;
            }

            string userID = restrictToMyNodes ? User?.Identity?.Name : null;

            var result = await DataHoldersSql.Get(userID, _limit,
                _page, NodeId_like, _sort, _order);

            HttpContext.Response.Headers["access-control-expose-headers"] = "X-Total-Count";
            HttpContext.Response.Headers["X-Total-Count"] = result.total.ToString();

            if (export)
            {
                if (exportType == 0)
                {
                    return File(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result.results)), "application/json",
                        "dataholders.json", false);
                }

                if (exportType == 1)
                {
                    return File(Encoding.UTF8.GetBytes(CsvSerializer.SerializeToCsv(result.results)), "text/csv",
                        "dataholders.csv", false);
                }
            }

            return new OkObjectResult(result.results);
        }

        [Route("GetNodeIDForIdentity")]
        [HttpGet]
        public async Task<string> GetNodeIDForIdentity([FromQuery] string identity)
        {
            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var data = await connection.ExecuteScalarAsync<string>("SELECT NodeID FROM OTIdentity WHERE Identity = @identity ORDER BY NodeID DESC LIMIT 1", new
                {
                    identity = identity
                });

                return data;
            }
        }



        [Route("GetNodeIDForIdentity")]
        [HttpGet]
        public async Task<string> GetNodeIDForIdentity([FromQuery] string identity)
        {
            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var data = await connection.ExecuteScalarAsync<string>("SELECT NodeID FROM OTIdentity WHERE Identity = @identity ORDER BY NodeID DESC LIMIT 1", new
                {
                    identity = identity
                });

                return data;
            }
        }


        [Route("GetManagementWalletForIdentity")]
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the management wallet address for a specific identity"
        )]
        [SwaggerResponse(200, type: typeof(String))]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<String> GetManagementWalletForIdentity([FromQuery, SwaggerParameter("The ERC 725 identity for the node", Required = true)] string identity)
        {
            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                return await connection.ExecuteScalarAsync<string>(DataHoldersSql.GetManagementWalletForIdentitySql, new {identity = identity});
            }
        }

        [Route("GetRecentPayoutGasPrices")]
        [HttpGet]
        [SwaggerOperation(
            Summary = "Get recent gas prices used by other data holders when requested payouts in the last 7 days"
        )]
        [SwaggerResponse(200, type: typeof(RecentPayoutGasPrice[]))]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<RecentPayoutGasPrice[]> GetRecentPayoutGasPrices()
        {
            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                return (await connection.QueryAsync<RecentPayoutGasPrice>(DataHoldersSql.GetRecentPayoutGasPricesSql)).ToArray();
            }
        }
    }
}