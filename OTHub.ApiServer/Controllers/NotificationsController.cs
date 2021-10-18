using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using OTHub.Settings;

namespace OTHub.APIServer.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : Controller
    {
        [HttpGet]
        public async Task<NotificationModel[]> Get()
        {
            await using (MySqlConnection connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var rows = (await connection.QueryAsync<NotificationModel>(@"SELECT Title, CreatedAt Date, 
Description, RelativeUrl Url, `Read` FROM notifications
WHERE Dismissed = 0 AND UserID = @userID
ORDER BY DATE DESC", new
                {
                    userID = User.Identity.Name
                })).ToArray();

                return rows;
            }
        }

        [HttpPost]
        [Route("MarkAsRead")]
        public async Task MarkAsRead([FromQuery] DateTime upToDate)
        {
            await using (MySqlConnection connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                await connection.ExecuteAsync(@"UPDATE notifications 
SET `Read` = 1
WHERE UserID = @userID AND `Read` = 0 AND CreatedAt <= @date AND Dismissed = 0", new
                {
                    userID = User.Identity.Name,
                    date = upToDate
                });
            }
        }

        [HttpPost]
        [Route("Dismiss")]
        public async Task Dismiss([FromQuery] DateTime upToDate)
        {
            await using (MySqlConnection connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                await connection.ExecuteAsync(@"UPDATE notifications 
SET `Read` = 1, Dismissed = 1
WHERE UserID = @userID AND `Dismissed` = 0 AND CreatedAt <= @date", new
                {
                    userID = User.Identity.Name,
                    date = upToDate
                });
            }
        }
    }

    public class NotificationModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public string Url { get; set; }
        public bool Read { get; set; }
    }
}
