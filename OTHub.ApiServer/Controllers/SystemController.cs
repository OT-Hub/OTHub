using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using OTHub.APIServer.Sql;
using OTHub.APIServer.Sql.Models;
using OTHub.APIServer.Sql.Models.System;
using OTHub.Settings;
using Swashbuckle.AspNetCore.Annotations;

namespace OTHub.APIServer.Controllers
{
    [Route("api/[controller]")]
    public class SystemController : Controller
    {
        [HttpGet]
        [SwaggerResponse(200, type: typeof(SystemStatus))]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<SystemStatus> Get()
        {
            SystemStatus status = new SystemStatus();

            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                SystemStatusItem[] items = (await connection.QueryAsync<SystemStatusItem>(SystemSql.GetSql)).ToArray();

                SystemStatusGroup[] groups = items.GroupBy(i => i.BlockchainName + " " + i.NetworkName)
                    .Select(g => new SystemStatusGroup() {Name = g.Key, Items = g.ToArray()})
                    .ToArray();

                status.Groups = groups;
            }

            return status;
        }
    }
}