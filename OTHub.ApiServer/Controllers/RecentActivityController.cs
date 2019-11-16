using System.Linq;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using OTHub.APIServer.Models;
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
                    @"SELECT OH.Holder Identity, O.OfferId, O.CreatedTimestamp as Timestamp, O.TokenAmountPerHolder, 
(CASE WHEN O.IsFinalized = 1  THEN DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) ELSE NULL END) as EndTimestamp
FROM OTOffer_Holders OH
JOIN OTOffer O ON O.OfferID = OH.OfferID
JOIN OTIdentity I ON I.NodeID = O.DCNodeID
WHERE OH.Holder in @identity AND OH.IsOriginalHolder = 1 AND O.CreatedTimestamp >= DATE_Add(NOW(), INTERVAL -7 DAY)
ORDER BY O.CreatedTimestamp DESC", new
                    {
                        identity
                    }).ToArray();

                return summary;
            }
        }
    }
}
