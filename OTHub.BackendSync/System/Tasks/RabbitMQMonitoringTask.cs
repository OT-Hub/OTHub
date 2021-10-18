using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OTHub.BackendSync.Logging;
using OTHub.Settings.Constants;
using RabbitMQ.Client;

namespace OTHub.BackendSync.System.Tasks
{
    public class RabbitMQMonitoringTask : TaskRunGeneric
    {
        private static readonly ConnectionFactory _factory;
        private static List<uint> _listOfPreviousMessageCounts = new List<uint>(3);
        

        static RabbitMQMonitoringTask()
        {
            _factory = new ConnectionFactory
                { HostName = "localhost", RequestedHeartbeat = TimeSpan.FromMinutes(4), DispatchConsumersAsync = true };
        }

        public RabbitMQMonitoringTask() : base(TaskNames.RabbitMQMonitoring)
        {
      
        }

        public override async Task Execute(Source source)
        {
            using var connection = _factory.CreateConnection();
            using var channel = connection.CreateModel();

            uint count = channel.ConsumerCount("OfferFinalized");

            if (count == 0)
            {
                throw new Exception("No consumers on OfferFinalized queue.");
            }

            uint messageCount = channel.MessageCount("OfferFinalized");

            while (_listOfPreviousMessageCounts.Count > 4)
            {
                _listOfPreviousMessageCounts.RemoveAt(0);
            }

            _listOfPreviousMessageCounts.Add(messageCount);


            if (_listOfPreviousMessageCounts.Count > 4)
            {
                Dictionary<int, bool> loopIncreaseTrackerDictionary = new Dictionary<int, bool>();

                for (int i = 0; i < _listOfPreviousMessageCounts.Count; i++)
                {
                    uint loopCount = _listOfPreviousMessageCounts[i];

                    if (_listOfPreviousMessageCounts.Count > i + 1)
                    {
                        uint nextLoopCount = _listOfPreviousMessageCounts[i + 1];

                        loopIncreaseTrackerDictionary[i] = nextLoopCount >= loopCount && nextLoopCount != 0;
                    }
                }

                if (loopIncreaseTrackerDictionary.Values.Reverse().Take(3).All(b => b == true))
                {
                    throw new Exception("Message count for OfferFinalized not going down: " +
                                        _listOfPreviousMessageCounts.Select(a => a.ToString())
                                            .Aggregate((a, b) => a + ", " + b));
                }
            }
        }

        public override string ParentName { get; } = TaskNames.Notifications;
    }
}