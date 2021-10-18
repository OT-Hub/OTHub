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
            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                int? networkID = await connection.ExecuteScalarAsync<int?>(@"SELECT NetworkID FROM blockchains WHERE id = @blockchainID",
                    new
                    {
                        blockchainID
                    });

                return networkID;
            }
        }

        [Route("GetBlockchains")]
        [HttpGet]
        public async Task<BlockchainModel[]> GetBlockchains()
        {
            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                return (await connection.QueryAsync<BlockchainModel>(@"SELECT ID, BlockchainName FROM blockchains")).ToArray();
            }
        }
    }

    public class BlockchainModel
    {
        public int ID { get; set; }
        public string BlockchainName { get; set; }
    }
}