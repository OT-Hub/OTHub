using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OTHub.APIServer.Controllers;
using OTHub.Settings;
using ServiceStack.Text;
using Telegram.Bot;
using Telegram.Bot.Extensions.LoginWidget;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace OTHub.APIServer.Helpers
{
    public class TelegramBot
    {
        private TelegramBotClient _botClient;

        public TelegramBot()
        {
            Connect();
        }

        public bool IsConnected { get; private set; }

        public async void Connect()
        {
            if (OTHubSettings.Instance.Telegram?.BotKey == null || _botClient != null)
                return;

            _botClient = new TelegramBotClient(OTHubSettings.Instance.Telegram.BotKey);

            _botClient.OnMakingApiRequest += BotClient_OnMakingApiRequest;
            _botClient.OnApiResponseReceived += BotClient_OnApiResponseReceived;

            User me = await _botClient.GetMeAsync();

            using var cts = new CancellationTokenSource();

            _botClient.StartReceiving(new DefaultUpdateHandler(HandleUpdateAsync, HandleErrorAsync),
                null, cts.Token);

            IsConnected = true;
        }

        private async Task HandleErrorAsync(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
        {

        }

        private async Task HandleUpdateAsync(ITelegramBotClient arg1, Update arg2, CancellationToken arg3)
        {
            if (arg2.Type == UpdateType.Message)
            {
                if (arg2.Message.Text == "/start")
                {
                    await _botClient.SendTextMessageAsync(arg2.Message.Chat.Id, "Response to /start.", cancellationToken: arg3);

                    //await arg1.SetMyCommandsAsync(new List<BotCommand>()
                    //{
                    //    new BotCommand()
                    //    {
                    //        Description = "Test",
                    //        Command = "helo"
                    //    }
                    //}, BotCommandScope.Chat(arg2.Message.Chat.Id), cancellationToken: arg3);
                }
            }
            else if (arg2.Type == UpdateType.CallbackQuery)
            {

            }

            //await arg1.SendTextMessageAsync(arg2.Message.Chat.Id, "Hello Earthling!",
            //    replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Yoyo")));


        }

        private async ValueTask BotClient_OnApiResponseReceived(ITelegramBotClient botClient, Telegram.Bot.Args.ApiResponseEventArgs args, System.Threading.CancellationToken cancellationToken = default)
        {

        }

        private async ValueTask BotClient_OnMakingApiRequest(ITelegramBotClient botClient, Telegram.Bot.Args.ApiRequestEventArgs args, System.Threading.CancellationToken cancellationToken = default)
        {

        }

        public Authorization LinkAccount(TelegramAccount account)
        {
            if (!IsConnected)
                return Authorization.MissingFields;

            LoginWidget widget = new LoginWidget(OTHubSettings.Instance.Telegram.BotKey);

            var dict = account.ToStringDictionary();

            return widget.CheckAuthorization(dict);
        }

        public async Task SendFirstMessage(long telegramUserID)
        {
            await _botClient.SendTextMessageAsync(telegramUserID, "Hello!");
            await Task.Delay(1000);
            await _botClient.SendTextMessageAsync(telegramUserID, "You have successfully linked your OT Hub account with telegram.");
        }

        public async Task JobWon(long telegramUserID, string title, string url)
        {
            await _botClient.SendTextMessageAsync(telegramUserID, title,
                replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl("View Job", url)));
        }
    }
}