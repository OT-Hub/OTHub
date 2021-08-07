using System;
using System.Collections.Generic;
using System.Linq;
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

            Console.WriteLine("LinkAccountResult: " + result);

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
                }

                await _bot.SendFirstMessage(account.id);
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
}