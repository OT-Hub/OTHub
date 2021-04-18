using System.Threading.Tasks;
using Auth0.ManagementApi;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using OTHub.Settings;

namespace OTHub.APIServer.Controllers
{
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        [HttpPost]
        [Authorize]
        [Route("EnsureCreated")]
        public async Task EnsureCreated()
        {
            await using (MySqlConnection connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                bool exists = await connection.ExecuteScalarAsync<bool>(@"SELECT 1 FROM Users WHERE ID = @userID", new
                {
                    userID = User.Identity.Name
                });

                if (!exists)
                {
                    await connection.ExecuteAsync("INSERT INTO Users (ID) VALUES (@userID)", new
                    {
                        userID = User.Identity.Name
                    });
                }
            }
        }
    }
}
