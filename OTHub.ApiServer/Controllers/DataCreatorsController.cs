using System;
using System.Linq;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Newtonsoft.Json;
using OTHub.APIServer.Sql;
using OTHub.APIServer.Sql.Models.Nodes.DataCreators;
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
                limit = $"LIMIT {_page * _limit},{_limit}";
            }

            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                NodeDataCreatorSummaryModel[] summary = connection.Query<NodeDataCreatorSummaryModel>(
                    DataCreatorsSql.GetDataCreatorsSql(identity) +
                    $@"
{orderBy}
{limit}", new { version = ercVersion, identity, Identity_like }, commandTimeout: 120).ToArray();

                var total = connection.ExecuteScalar<int>(DataCreatorsSql.GetDataCreatorsCountSql(identity),
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
    }
}