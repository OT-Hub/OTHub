using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using OTHub.APIServer.Helpers;
using OTHub.Settings;
using Swashbuckle.AspNetCore.Annotations;
using Telegram.Bot.Extensions.LoginWidget;

namespace OTHub.APIServer.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class TelegramController : Controller
    {
        private readonly TelegramBot _bot;

        public TelegramController(TelegramBot bot)
        {
            _bot = bot;
        }

        [HttpPost]
        [Route("LinkAccount")]
        [SwaggerOperation(Description = "Requires authentication to use.")]
        public async Task LinkAccount([FromBody] TelegramAccount account)
        {
            Authorization result = _bot.LinkAccount(account);

            if (result == Authorization.Valid)
            {
                string userID = User.Identity.Name;

                await using (MySqlConnection connection =
                    new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                {
                    await connection.ExecuteAsync("UPDATE Users SET TelegramUserID = @telegramUserID WHERE ID = @userID", new
                    {
                        userID = userID,
                        telegramUserID = account.id
                    });

                    int count = await connection.ExecuteScalarAsync<int>(
                        @"SELECT COUNT(*) FROM telegramsettings WHERE userid = @userID",
                        new
                        {
                            userID = userID
                        });

                    if (count == 0)
                    {
                        await connection.ExecuteAsync(
                            @"INSERT INTO telegramsettings (UserID, NotificationsEnabled, JobWonEnabled, HasReceivedMessageFromUser)
VALUE (@userID, 1, 1, 0)", new
                            {
                                userID = userID
                            });
                    }
                }
            }
        }

        [HttpPost]
        [Route("UpdateNotificationsEnabled")]
        [SwaggerOperation(Description = "Requires authentication to use.")]
        public async Task UpdateNotificationsEnabled([FromQuery]bool value)
        {
            string userID = User.Identity.Name;

            await using (MySqlConnection connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                await connection.ExecuteAsync(@"
UPDATE telegramsettings
SET NotificationsEnabled = @value
WHERE UserID = @userID", new
                {
                    userID = userID,
                    value = value
                });
            }
        }

        [HttpPost]
        [Route("UpdateJobWonEnabled")]
        [SwaggerOperation(Description = "Requires authentication to use.")]
        public async Task UpdateJobWonEnabled([FromQuery] bool value)
        {
            string userID = User.Identity.Name;

            await using (MySqlConnection connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                await connection.ExecuteAsync(@"
UPDATE telegramsettings
SET JobWonEnabled = @value
WHERE UserID = @userID", new
                {
                    userID = userID,
                    value = value
                });
            }
        }



        [HttpGet]
        [Route("GetSettings")]
        [SwaggerOperation(Description = "Requires authentication to use.")]
        public async Task<TelegramSettings> GetSettings()
        {
            string userID = User.Identity.Name;

            await using (MySqlConnection connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var settings = await connection.QueryFirstOrDefaultAsync<TelegramSettings>(@"SELECT u.TelegramUserID AS TelegramID, 
ts.NotificationsEnabled, ts.JobWonEnabled, ts.HasReceivedMessageFromUser FROM users u
JOIN telegramsettings ts ON ts.UserID = u.ID
WHERE u.ID = @userID", new
                {
                    userID = userID
                });

                return settings;
            }
        }
    }

    public class TelegramAccount
    {
        public long id { get; set; }
        public string first_name { get; set; }
        public long auth_date { get; set; }
        public string last_name { get; set; }
        public string username { get; set; }
        public string photo_url { get; set; }
        public string hash { get; set; }
    }

    public class TelegramSettings
    {
        public long? TelegramID { get; set; }
        public bool NotificationsEnabled { get; set; }
        public bool JobWonEnabled { get; set; }
        public bool HasReceivedMessageFromUser { get; set; }
    }
}