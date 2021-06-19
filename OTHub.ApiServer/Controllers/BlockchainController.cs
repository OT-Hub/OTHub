using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using OTHub.Settings;
using Swashbuckle.AspNetCore.Annotations;

namespace OTHub.APIServer.Controllers
{
    [Route("api/[controller]")]
    public class BlockchainController : Controller
    {
        [Route("NetworkID")]
        [HttpGet]
        [SwaggerResponse(200, type: typeof(int))]
        public async Task<int?> GetNetworkID([FromQuery] int blockchainID)
        {
            using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                int? networkID = await connection.ExecuteScalarAsync<int?>(@"SELECT NetworkID FROM blockchains WHERE id = @blockchainID",
                    new
                    {
                        blockchainID
                    });

                return networkID;
            }
        }
    }
}