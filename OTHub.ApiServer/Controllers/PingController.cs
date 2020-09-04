using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using OTHub.Settings;
using Swashbuckle.AspNetCore.Annotations;

namespace OTHub.APIServer.Controllers
{
    [Route("api/[controller]")]
    public class PingController : Controller
    {
        [HttpGet]
        [SwaggerOperation(
            Summary = "Check that the OT Hub API is available."
        )]
        [SwaggerResponse(200)]
        [SwaggerResponse(500, "Internal server error")]
        public async Task Get()
        {
            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                await connection.OpenAsync();
            }
        }
    }
}