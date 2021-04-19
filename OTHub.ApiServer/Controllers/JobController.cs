using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using OTHub.APIServer.Sql;
using OTHub.APIServer.Sql.Models.Jobs;
using OTHub.APIServer.Sql.Models.Nodes.DataHolder;
using OTHub.Settings;
using Swashbuckle.AspNetCore.Annotations;

namespace OTHub.APIServer.Controllers
{
    [Route("api/[controller]")]
    public class JobController : Controller
    {
        [HttpGet]
        [Route("detail/{offerID}")]
        [SwaggerOperation(
           Summary = "Get detailed information about a offer",
           Description = @"This will return most information known about the offer.

Data Included:
- Dataset Information
- Timeline
- Data Holders")]
        [SwaggerResponse(200, type: typeof(NodeDataHolderDetailedModel))]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<OfferDetailedModel> Detail([SwaggerParameter("The ID of the offer", Required = true)] string offerID)
        {
            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                OfferDetailedModel model = await connection.QueryFirstOrDefaultAsync<OfferDetailedModel>(
                    JobSql.GetJobDetailed, new { offerID = offerID,
                        userID = User?.Identity?.Name
                    });
                if (model != null)
                {
                    model.Holders = (await connection.QueryAsync<OfferDetailedHolderModel>(
                        JobSql.GetJobHolders, new
                        {
                            offerID = offerID,
                            userID = User?.Identity?.Name
                        })).ToArray();

                    model.TimelineEvents = (await connection.QueryAsync<OfferDetailedTimelineEventModel>(JobSql.GetJobTimelineEvents(), new { offerID = offerID })).OrderBy(t => t.Timestamp).ToArray();
                }

                return model;
            }
        }
    }
}