using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OTHub.APIServer.Sql;
using OTHub.APIServer.Sql.Models;
using OTHub.APIServer.Sql.Models.Jobs;
using ServiceStack.Text;
using Swashbuckle.AspNetCore.Annotations;

namespace OTHub.APIServer.Controllers
{
    [Route("api/[controller]")]
    public class JobsController : Controller
    {
        [HttpGet]
        [Route("paging")]
        [SwaggerOperation(
            Summary = "Gets all offers (paging)",
            Description = @"
This will return a summary of information about each offer.

If you want to get more information about a specific offer you should use /api/jobs/detail/{offerID} API call"
        )]
        [SwaggerResponse(200, type: typeof(OfferSummaryModel[]))]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<IActionResult>GetWithPaging(
            [FromQuery, SwaggerParameter("How many offers you want to return per page", Required = true)]
            int _limit,
            [FromQuery, SwaggerParameter("The page number to start from. The first page is 0.", Required = true)]
            int _page, [FromQuery] string OfferId_like,
            [FromQuery] string _sort,
            [FromQuery] string _order,
            [FromQuery] bool export,
            [FromQuery] int? exportType)
        {
            _page--;

            if (OfferId_like != null && OfferId_like.Length > 200)
            {
                OfferId_like = null;
            }

            OfferSummaryModel[] rows = JobsSql.GetWithPaging(_limit, _page, OfferId_like, _sort, _order, out int total);

            HttpContext.Response.Headers["access-control-expose-headers"] = "X-Total-Count";
            HttpContext.Response.Headers["X-Total-Count"] = total.ToString();

            if (export)
            {
                if (exportType == 0)
                {
                    return File(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(rows)), "application/json",
                        "jobs.json", false);
                }
                else if (exportType == 1)
                {
                    return File(Encoding.UTF8.GetBytes(CsvSerializer.SerializeToCsv(rows)), "text/csv", "jobs.csv",
                        false);
                }
            }

            return new OkObjectResult(rows);
        }
    }
}