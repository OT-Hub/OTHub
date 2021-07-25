using System.Threading.Tasks;
using OTHub.BackendSync.Blockchain.Tasks.Misc.Children;
using OTHub.BackendSync.Logging;
using OTHub.BackendSync.Markets.Tasks;
using OTHub.Settings.Constants;

namespace OTHub.BackendSync.Blockchain.Tasks.Misc
{
    public class MiscTask : TaskRunGeneric
    {
        public MiscTask() : base(TaskNames.Misc)
        {
            Add(new UpdateHomeJobHistoryChartDataTask());
            Add(new UpdateStakedTokenReportTask());
            Add(new GetMarketDataTask());
            //Add(new CalculateOfferLambdaTask());
        }

        public override async Task Execute(Source source)
        {
            await RunChildren(source);
        }
    }
}