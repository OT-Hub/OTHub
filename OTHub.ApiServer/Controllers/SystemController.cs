using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using OTHub.APIServer.Models;
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
                status.Items = connection.Query<SystemStatusItem>(@"select Name, LastSuccessDateTime, LastTriedDateTime, Success from systemstatus ORDER BY Name").ToArray();
            }

            return status;
        }
    }

    public class SystemStatus
    {
        public SystemStatusItem[] Items { get; set; }
    }

    public class SystemStatusItem
    {
        public String Name { get; set; }
        public DateTime? LastSuccessDateTime { get; set; }
        public DateTime LastTriedDateTime { get; set; }
        public bool Success { get; set; }
    }
}