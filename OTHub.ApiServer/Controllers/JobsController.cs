using System;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MySqlConnector;
using Newtonsoft.Json;
using OTHub.APIServer.Sql;
using OTHub.APIServer.Sql.Models;
using OTHub.APIServer.Sql.Models.Jobs;
using OTHub.Settings;
using ServiceStack.Text;
using Swashbuckle.AspNetCore.Annotations;

namespace OTHub.APIServer.Controllers
{
    [Route("api/[controller]")]
    public class JobsController : Controller
    {
        private readonly IMemoryCache _cache;

        public JobsController(IMemoryCache cache)
        {
            _cache = cache;
        }

        [HttpGet]
        [Route("jobcreatedcountinperiod")]
        public async Task<IActionResult> GetJobCreatedCountInTimePeriod([FromQuery] string timePeriod, [FromQuery] int time, [FromQuery] int? blockchainID)
        {
            string cacheKey = $@"JobsController-GetJobCreatedCountInTimePeriod-{blockchainID}-{timePeriod}-{time}";
            if (_cache.TryGetValue(cacheKey, out object value))
            {
                if (value is long intVal)
                {
                    return Ok(intVal);
                }
            }

            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                DateTime date = DateTime.UtcNow;

                time = Math.Abs(time) * -1;

                switch (timePeriod)
                {
                    case "seconds":
                        date = date.AddSeconds(time);
                        break;
                    case "minutes":
                        date = date.AddMinutes(time);
                        break;
                    case "hours":
                        date = date.AddHours(time);
                        break;
                    case "days":
                        date = date.AddDays(time);
                        break;
                    case "months":
                        date = date.AddMonths(time);
                        break;
                    case "years":
                        date = date.AddYears(time);
                        break;
                    default:
                        return BadRequest("Invalid timePeriod parameter. Valid options: minutes, hours, days, months, years");
                }


                var count = await connection.ExecuteScalarAsync<long>(@"SELECT COUNT(o.OfferID)
FROM otcontract_holding_offercreated o
WHERE (@blockchainID is null OR o.BlockchainID = @blockchainID) AND o.Timestamp >= @laterThanDate", new
                {
                    blockchainID = blockchainID,
                    laterThanDate = date
                });

                using (ICacheEntry cacheEntry = _cache.CreateEntry(cacheKey))
                {
                    cacheEntry.Priority = CacheItemPriority.Low;
                    cacheEntry.Value = count;
                    cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                }

                return Ok(count);
            }
        }

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

            var result = await JobsSql.GetWithPaging(_limit, _page, OfferId_like, _sort, _order);

            HttpContext.Response.Headers["access-control-expose-headers"] = "X-Total-Count";
            HttpContext.Response.Headers["X-Total-Count"] = result.total.ToString();

            if (export)
            {
                if (exportType == 0)
                {
                    return File(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result.results)), "application/json",
                        "jobs.json", false);
                }
                else if (exportType == 1)
                {
                    return File(Encoding.UTF8.GetBytes(CsvSerializer.SerializeToCsv(result.results)), "text/csv", "jobs.csv",
                        false);
                }
            }

            return new OkObjectResult(result.results);
        }

        [HttpGet]
        [Route("last24h")]
        [SwaggerOperation(
    Summary = "Gets all offers (last 24h)",
    Description = @"
This will return a summary of information about each offer.

If you want to get more information about a specific offer you should use /api/jobs/detail/{offerID} API call"
)]
        [SwaggerResponse(200, type: typeof(OfferSummaryModel[]))]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<IActionResult> GetLast24H()
        {

            var result = await JobsSql.GetLast24H();


            return new OkObjectResult(result);
        }
    }
}