using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using OTHub.Settings;

namespace OTHub.APIServer.Controllers
{
    [Route("api/[controller]")]
    public class ReportsController : Controller
    {
        [HttpGet]
        [Route(("StakedTokensByDay"))]
        public async Task<StakedTokensByDayModel[]> StakedTokensByDay()
        {
            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                return (await connection.QueryAsync<StakedTokensByDayModel>(@"SELECT * FROM stakedtokensbyday")).ToArray();
            }
        }
    }

    public class StakedTokensByDayModel
    {
        public DateTime Date { get; set; }
        public string Deposited { get; set; }
        public string   Withdrawn { get; set; }
        public string Staked { get; set; }
    }
}