using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MySqlConnector;
using OTHub.BackendSync.Logging;
using OTHub.BackendSync.Markets.Tasks;
using OTHub.Settings;

namespace OTHub.BackendSync.Ethereum.Tasks.Misc
{
    public class MiscTask : TaskRunGeneric
    {
        public MiscTask() : base("Misc")
        {
            Add(new CalculateOfferLambdaTask());
            //Add(new GetMarketDataTask());
        }

        public override async Task Execute(Source source)
        {
            await RunChildren(source);
        }
    }
}