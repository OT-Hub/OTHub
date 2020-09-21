using System;
using System.Linq;
using System.Text;
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
        public IActionResult Get([FromQuery,
                                  SwaggerParameter(
                                      "The filter to use for the ERC version of the identity. The ODN launched with version 0 for. In Decemember 2018 all nodes upgraded their identities (which also generated them new identities) which are version 1. The OT Hub website only shows users version 1 identities.",
                                      Required = true)]
            int ercVersion,
            [FromQuery,
             SwaggerParameter(
                 "Filter the results to only include identities listed. Multiple identities can be provided by seperating them with &. Up to 50 can be provided maximum.",
                 Required = false)]
            string[] identity,
            [FromQuery,
             SwaggerParameter(
                 "Filter the results to only include identities with the specified management wallet address. Multiple management wallet addresses can be provided by seperating them with &. Up to 50 can be provided maximum.",
                 Required = false)]
            string[] managementWallet,
            [FromQuery, SwaggerParameter("How many offers you want to return per page", Required = true)]
            int _limit,
            [FromQuery, SwaggerParameter("The page number to start from. The first page is 0.", Required = true)]
            int _page,
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

            if (managementWallet.Length >= 50 ||
                managementWallet.Any(i => i.Length >= 50 || !i.StartsWith("0x") || i.Contains(" ")))
            {
                return new OkObjectResult(new NodeDataHolderSummaryModel[0]);
            }

            if (Identity_like != null && Identity_like.Length > 200)
            {
                Identity_like = null;
            }

            NodeDataHolderSummaryModel[] rows = DataHoldersSql.Get(ercVersion, identity, managementWallet, _limit,
                _page, Identity_like, _sort, _order,
                out int total);

            HttpContext.Response.Headers["access-control-expose-headers"] = "X-Total-Count";
            HttpContext.Response.Headers["X-Total-Count"] = total.ToString();

            if (export)
            {
                if (exportType == 0)
                {
                    return File(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(rows)), "application/json",
                        "dataholders.json", false);
                }

                if (exportType == 1)
                {
                    return File(Encoding.UTF8.GetBytes(CsvSerializer.SerializeToCsv(rows)), "text/csv",
                        "dataholders.csv", false);
                }
            }

            return new OkObjectResult(rows);
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
                return connection.ExecuteScalar<string>(DataHoldersSql.GetManagementWalletForIdentitySql, new {identity = identity});
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
                return connection.Query<RecentPayoutGasPrice>(DataHoldersSql.GetRecentPayoutGasPricesSql).ToArray();
            }
        }
    }
}