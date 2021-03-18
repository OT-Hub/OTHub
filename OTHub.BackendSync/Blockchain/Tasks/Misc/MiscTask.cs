using System.Threading.Tasks;
using OTHub.BackendSync.Blockchain.Tasks.Misc.Children;
using OTHub.BackendSync.Logging;
using OTHub.BackendSync.Markets.Tasks;

namespace OTHub.BackendSync.Blockchain.Tasks.Misc
{
    public class MiscTask : TaskRunGeneric
    {
        public MiscTask() : base("Misc")
        {
            Add(new GetMarketDataTask());
            Add(new CalculateOfferLambdaTask());
            Add(new UpdateHomeJobHistoryChartDataTask());
        }

        public override async Task Execute(Source source)
        {
            await RunChildren(source);
        }
    }
}