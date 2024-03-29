﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using OTHub.APIServer.Controllers;
using OTHub.APIServer.Notifications;
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
            if (string.IsNullOrWhiteSpace(OTHubSettings.Instance.Telegram?.BotKey) || _botClient != null)
            {
                Console.WriteLine("Bot key is null for Telegram.");
                return;
            }

            _botClient = new TelegramBotClient(OTHubSettings.Instance.Telegram.BotKey);

            _botClient.OnMakingApiRequest += BotClient_OnMakingApiRequest;
            _botClient.OnApiResponseReceived += BotClient_OnApiResponseReceived;

            User me = await _botClient.GetMeAsync();

            await _botClient.SetMyCommandsAsync(new List<BotCommand>()
            {
                //new BotCommand()
                //{
                //    Description = "Change Notification Settings",
                //    Command = "settings"
                //}
            }, BotCommandScope.AllPrivateChats());

            using var cts = new CancellationTokenSource();

            _botClient.StartReceiving(new DefaultUpdateHandler(HandleUpdateAsync, HandleErrorAsync),
                null, cts.Token);

            IsConnected = true;

            Console.WriteLine("Telegram bot is connected.");
        }

        private async Task HandleErrorAsync(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
        {

        }

        private async Task HandleUpdateAsync(ITelegramBotClient arg1, Update arg2, CancellationToken arg3)
        {
            if (arg2.Type == UpdateType.Message)
            {
                if (arg2.Message?.Text == "/start")
                {
                    await FirstUserLoadSetup(arg2.Message.Chat.Id, arg3);
                }
            }
            //else if (arg2.Type == UpdateType.CallbackQuery)
            //{
            //    if (arg2.CallbackQuery.Data == "settings")
            //    {

            //    }
            //}

            //await arg1.SendTextMessageAsync(arg2.Message.Chat.Id, "Hello Earthling!",
            //    replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Yoyo")));


        }

        private async Task FirstUserLoadSetup(long chatID, CancellationToken arg3)
        {
            await _botClient.SendTextMessageAsync(chatID, "Hello there!",
                cancellationToken: arg3);

            await _botClient.SendTextMessageAsync(chatID,
                "Give me a few seconds while I confirm your account is ready for notifications!",
                cancellationToken: arg3);

            await Task.Delay(2000, arg3);

            await using (MySqlConnection connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                string[] userIDs = (await connection.QueryAsync<string>(
                        @"SELECT ID FROM users WHERE TelegramUserID = @telegramID",
                        new
                        {
                            telegramID = chatID
                        })
                    ).ToArray();

                if (userIDs.Any())
                {
                    foreach (string userID in userIDs)
                    {
                        await connection.ExecuteAsync(
                            @"UPDATE telegramsettings SET HasReceivedMessageFromUser = 1 WHERE UserID = @userID",
                            new
                            {
                                userID = userID
                            });
                    }

                    await _botClient.SendTextMessageAsync(chatID,
                        "Your OT Hub account is all setup for notifications!",
                        replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl(
                            "Change Notification Settings",
                            "https://othub.origin-trail.network/nodes/mynodes/manage")),
                        cancellationToken: arg3);
                }
                else
                {
                    await _botClient.SendTextMessageAsync(chatID,
                        "I was unable to find any accounts on OT Hub that are linked to this Telegram user.",
                        cancellationToken: arg3);

                    await Task.Delay(2000, arg3);

                    await _botClient.SendTextMessageAsync(chatID,
                        "Please login to OT Hub, go to this link and then login to Telegram on OT Hub.",
                        replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl(
                            "Go to OT Hub Telegram Login",
                            "https://othub.origin-trail.network/nodes/mynodes/manage")),
                        cancellationToken: arg3);
                }
            }
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
            {
                Console.WriteLine("Not connected to Telegram.");
                return Authorization.MissingFields;
            }

            LoginWidget widget = new LoginWidget(OTHubSettings.Instance.Telegram.BotKey);

            var dict = account.ToStringDictionary();

            return widget.CheckAuthorization(dict);
        }

        public async Task JobWon(string userID, long telegramUserID, string title, string description, string url)
        {


            await _botClient.SendTextMessageAsync(telegramUserID, title + "\n" + description,
                replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl("View Job", url)));

            Console.WriteLine("Sent telegram notification for " + userID + ". Title: " + title);
        }

        public async Task SendTestMessage(long telegramUserID)
        {
            await FirstUserLoadSetup(telegramUserID, CancellationToken.None);
        }

        public async Task LowAvailableTokensOnNode(LowAvailableTokenUsers user, LowAvailableTokenNode node,
            decimal available)
        {
            await _botClient.SendTextMessageAsync(user.TelegramUserID,
                $"Low Available Tokens for New Jobs on {node.NodeName}.\nStaked: {node.Stake} TRAC\nLocked: {node.StakeReserved} TRAC\nAvailable: {available} TRAC\nDeposit more tokens or payout existing jobs to free up tokens.");
        }
    }
}