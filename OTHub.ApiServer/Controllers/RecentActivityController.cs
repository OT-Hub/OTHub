using System.Linq;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using OTHub.APIServer.Sql;
using OTHub.APIServer.Sql.Models.RecentActivity;
using OTHub.Settings;
using Swashbuckle.AspNetCore.Annotations;

namespace OTHub.APIServer.Controllers
{
    [Route(("api/[controller]"))]
    public class RecentActivityController : Controller
    {
        [HttpGet]
        [SwaggerOperation(
            Summary = "[BETA] Gets jobs won by a collection of data holders in the last 7 days.",
            Description = @"This API call is likely to change in the future so it is not recommended to use it as present.

You can either pass in a single identity or multiple identities for convenience. 

Please note that this will not show a job where you replaced another node as part of litigation. 
This is a API defect but at the time of writing litigation is not on the ODN mainnet."
        )]
        [SwaggerResponse(200, type: typeof(RecentActivityJobModel[]))]
        [SwaggerResponse(500, "Internal server error")]
        public RecentActivityJobModel[] Get([FromQuery, SwaggerParameter("The ERC 725 identity for the node", Required = true)] string[] identity)
        {
            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                RecentActivityJobModel[] summary = connection.Query<RecentActivityJobModel>(
                    RecentActivitySql.GetRecentActivitySql, new
                    {
                        identity
                    }).ToArray();

                return summary;
            }
        }
    }
}
