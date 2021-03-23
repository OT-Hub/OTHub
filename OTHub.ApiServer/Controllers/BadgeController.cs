using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MySqlConnector;
using OTHub.APIServer.Sql;
using OTHub.APIServer.Sql.Models.Nodes;
using OTHub.APIServer.Sql.Models.Nodes.DataCreator;
using OTHub.Settings;
using Swashbuckle.AspNetCore.Annotations;

namespace OTHub.APIServer.Controllers
{
    [Route("api/[controller]")]
    public class BadgeController : Controller
    {
        private readonly IMemoryCache _cache;

        public BadgeController(IMemoryCache cache)
        {
            _cache = cache;
        }

        [Route("")]
        [HttpGet]
        [SwaggerResponse(200, type: typeof(BadgeModel[]))]
        public async Task<BadgeModel> Get()
        {
            const string key = "MenuBadges";

            if (_cache.TryGetValue(key, out var val))
            {
                return (BadgeModel) val;
            }

            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var badges = await connection.QueryFirstOrDefaultAsync<BadgeModel>(@"SELECT 
(SELECT COUNT(*) FROM otoffer WHERE IsFinalized = 1) AS TotalJobs,
(SELECT COUNT(DISTINCT NodeId) FROM otidentity WHERE VERSION = 1) AS DataHolders,
(SELECT COUNT(DISTINCT DcNodeID) FROM otcontract_holding_offercreated) AS DataCreators");

                _cache.Set(key, badges, TimeSpan.FromMinutes(2));

                return badges;

            }
        }
    }

    public class BadgeModel
    {
        public int TotalJobs { get; set; }
        public int DataHolders { get; set; }
        public int DataCreators { get; set; }
    }
}