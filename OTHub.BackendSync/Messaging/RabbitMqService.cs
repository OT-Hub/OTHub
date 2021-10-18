using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OTHub.Messaging;
using RabbitMQ.Client;

namespace OTHub.BackendSync.Messaging
{
    public static class RabbitMqService
    {
        private static ConnectionFactory _factory;
        private static IConnection _connection;
        private static IModel _channel;

        static RabbitMqService()
        {
            _factory = new ConnectionFactory {HostName = "localhost", RequestedHeartbeat = TimeSpan.FromMinutes(4)};
        }

        public static async Task Connect()
        {
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: "OfferFinalized",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _connection.ConnectionShutdown += Connection_ConnectionShutdown;
        }

        private static void Cleanup()
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

        private static async void Connection_ConnectionShutdown(object? sender, ShutdownEventArgs e)
        {
            Console.WriteLine("RMQ Connection broke!");

            Cleanup();

            while (true)
            {
                try
                {
                    await Connect();

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

        public static void OfferFinalized(IEnumerable<OfferFinalizedMessage> offerFinalizedMessages)
        {
            var batch = _channel.CreateBasicPublishBatch();

            foreach (OfferFinalizedMessage offerFinalizedMessage in offerFinalizedMessages)
            {
                var text = JsonConvert.SerializeObject(offerFinalizedMessage);
                var body = Encoding.UTF8.GetBytes(text);

                batch.Add("", "OfferFinalized", false, null, new ReadOnlyMemory<byte>(body));
            }

            batch.Publish();
        }
    }
}