using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public async Task<IActionResult> Get(
            [FromQuery, SwaggerParameter("How many offers you want to return per page", Required = true)] int _limit, 
            [FromQuery, SwaggerParameter("The page number to start from. The first page is 0.", Required = true)] int _page,
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

            string userID = User?.Identity?.Name;
            bool filterByMyNodes = restrictToMyNodes;

            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                NodeDataCreatorSummaryModel[] summary = (await connection.QueryAsync<NodeDataCreatorSummaryModel>(
                    DataCreatorsSql.GetDataCreatorsSql(userID, filterByMyNodes) +
                    $@"
{orderBy}
{limit}", new { userID = userID, NodeId_like }, commandTimeout: 120)).ToArray();

                var total = await connection.ExecuteScalarAsync<int>(DataCreatorsSql.GetDataCreatorsCountSql(userID, filterByMyNodes),
new { userID = userID, NodeId_like });

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