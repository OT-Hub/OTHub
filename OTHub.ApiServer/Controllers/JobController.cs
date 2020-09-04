using System.Linq;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
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
        public OfferDetailedModel Detail([SwaggerParameter("The ID of the offer", Required = true)] string offerID)
        {
            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                OfferDetailedModel model = connection.QueryFirstOrDefault<OfferDetailedModel>(
                    JobSql.GetJobDetailed, new { offerID = offerID });
                if (model != null)
                {
                    model.Holders = connection.Query<OfferDetailedHolderModel>(
                        JobSql.GetJobHolders, new
                        {
                            offerID = offerID
                        }).ToArray();

                    model.Timeline = connection.Query<OfferDetailedTimelineModel>(JobSql.GetJobTimeline(), new { offerID = offerID }).OrderBy(t => t.Timestamp).ToArray();
                }

                return model;
            }
        }
    }
}