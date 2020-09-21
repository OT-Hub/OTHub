using System.Linq;
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
        public SystemStatus Get()
        {
            SystemStatus status = new SystemStatus();

            using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                status.Items = connection.Query<SystemStatusItem>(SystemSql.GetSql).ToArray();
            }

            return status;
        }
    }
}