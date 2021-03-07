using System.Threading.Tasks;
using OTHub.BackendSync.Blockchain.Tasks.Misc.Children;
using OTHub.BackendSync.Logging;

namespace OTHub.BackendSync.Blockchain.Tasks.Misc
{
    public class MiscTask : TaskRunGeneric
    {
        public MiscTask() : base("Misc")
        {
            Add(new CalculateOfferLambdaTask());
            Add(new UpdateHomeJobHistoryChartDataTask());
            //Add(new GetMarketDataTask());
        }

        public override async Task Execute(Source source)
        {
            await RunChildren(source);
        }
    }
}