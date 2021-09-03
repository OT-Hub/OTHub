using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.SignalR;
using MySqlConnector;
using Newtonsoft.Json;
using OTHub.APIServer.Helpers;
using OTHub.APIServer.Notifications;
using OTHub.APIServer.SignalR;
using OTHub.Messaging;
using OTHub.Settings;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OTHub.APIServer.Messaging
{
    public class RabbitMQService
    {
        private readonly IHubContext<NotificationsHub> _hubContext;
        private readonly TelegramBot _bot;
        private readonly ConnectionFactory _factory;
        private IConnection _connection;
        private IModel _channel;

        public RabbitMQService(IHubContext<NotificationsHub> hubContext, TelegramBot bot)
        {
            _hubContext = hubContext;
            _bot = bot;
            _factory = new ConnectionFactory
                {HostName = "localhost", RequestedHeartbeat = TimeSpan.FromMinutes(4), DispatchConsumersAsync = true};
            Connect();
        }

        public void Connect()
        {
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();

            _connection.ConnectionShutdown += Connection_ConnectionShutdown;

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += Consumer_Received;



            _channel.BasicConsume("OfferFinalized",
                false,
                consumer);
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                var body = e.Body.ToArray();
                var text = Encoding.UTF8.GetString(body);
                OfferFinalizedMessage message = JsonConvert.DeserializeObject<OfferFinalizedMessage>(text);

                if (message == null)
                {
                    _channel.BasicAck(e.DeliveryTag, false);
                    return;
                }

                string[] holders = {message.Holder1, message.Holder2, message.Holder3};

                await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                {
                    foreach (string holder in holders)
                    {
                        var users = (await connection.QueryAsync(
                            @"SELECT DISTINCT mn.UserID, COALESCE(mn.DisplayName, i.NodeID) NodeName, U.TelegramUserID, 
ts.NotificationsEnabled, ts.JobWonEnabled, ts.HasReceivedMessageFromUser FROM mynodes mn
JOIN otidentity I ON I.NodeID = mn.NodeID
JOIN users U ON U.ID = mn.UserID
LEFT JOIN telegramsettings ts ON ts.UserID = U.ID
WHERE I.Version > 0 AND I.Identity = @identity", new
                            {
                                identity = holder
                            })).ToArray();

                        if (users.Any())
                        {
                            var jobData = await connection.QueryFirstOrDefaultAsync(
                                "SELECT TokenAmountPerHolder, HoldingTimeInMinutes FROM OTOffer o where o.BlockchainID = @blockchainID AND o.OfferID = @offerID",
                                new
                                {
                                    blockchainID = message.BlockchainID,
                                    offerID = message.OfferID
                                });

                            if (jobData == null)
                            {
                                continue;
                            }

                            decimal tokenAmount = jobData.TokenAmountPerHolder;
                            long holdingTimeInMinutes = jobData.HoldingTimeInMinutes;

                            foreach (var user in users)
                            {
                                string userID = user.UserID;
                                string nodeName = user.NodeName;
                                long? telegramUserID = user.TelegramUserID;
                                bool? notificationsEnabled = user.NotificationsEnabled == 1;
                                bool? jobWonEnabled = user.JobWonEnabled == 1;
                                bool? hasReceivedMessageFromUser = user.HasReceivedMessageFromUser == 1;

                                (string title, string description, string url) data = await NotificationsReaderWriter.InsertJobWonNotification(connection, message, userID,
                                    nodeName, tokenAmount, holdingTimeInMinutes);

                                if (data.title != null)
                                {
                                    try
                                    {
                                        await _hubContext.Clients.User(userID).SendAsync("JobWon", data.title, data.description);
                                    }
                                    catch (Exception ex)
                                    {

                                    }

                                    if (telegramUserID.HasValue && notificationsEnabled == true && jobWonEnabled == true && hasReceivedMessageFromUser == true)
                                    {
                                        try
                                        {
                                            await _bot.JobWon(userID, telegramUserID.Value, data.title, data.description, $"https://othub.origin-trail.network/{data.url}");
                                        }
                                        catch (Exception ex)
                                        {

                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                _channel.BasicAck(e.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to process offer finalized: " + ex.ToString());
                _channel.BasicNack(e.DeliveryTag, false, true);
            }
        }

        private void Cleanup()
        {
            try
            {
                if (_channel != null && _channel.IsOpen)
                {
                    _channel.Close();
                    _channel = null;
                }

                if (_connection != null && _connection.IsOpen)
                {
                    _connection.Close();
                    _connection = null;
                }
            }
            catch (IOException ex)
            {
                // Close() may throw an IOException if connection
                // dies - but that's ok (handled by reconnect)
            }
        }

        private async void Connection_ConnectionShutdown(object? sender, ShutdownEventArgs e)
        {
            Console.WriteLine("RMQ Connection broke!");

            Cleanup();

            while (true)
            {
                try
                {
                    Connect();

                    Console.WriteLine("RMQ Reconnected!");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("RMQ Reconnect failed!");
                    await Task.Delay(3000);
                }
            }
        }
    }
}