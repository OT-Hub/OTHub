using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using OTHub.BackendSync.Ethereum.Tasks;
using OTHub.BackendSync.Logging;
using OTHub.BackendSync.Markets.Tasks;
using OTHub.BackendSync.System.Tasks;
using OTHub.Settings;

namespace OTHub.BackendSync
{
    public class Bootstrapper
    {
        public void RunUntilExit()
        {
            List<Task> tasks = new List<Task>();

            tasks.Add(Task.Run(() =>
            {
                TaskController controller = new TaskController(Source.NodeUptimeAndMisc);

                controller.Schedule(new GetMarketDataTask(), TimeSpan.FromMinutes(180), true);

                controller.Schedule(new CalculateOfferLambdaTask(), TimeSpan.FromMinutes(60), true);

                controller.Schedule(new MarkOldContractsAsArchived(), TimeSpan.FromDays(1),
                    false); //TODO needs to do litigation contracts

                controller.Start();
            }));


            tasks.Add(Task.Run(() =>
            {
                TaskController controller = new TaskController(Source.BlockchainSync);

                controller.Schedule(new GetLatestContractsTask(), TimeSpan.FromMinutes(300), true);

                controller.Schedule(new RefreshAllHolderLitigationStatusesTask(), TimeSpan.FromHours(2), true);

                controller.Schedule(new BlockchainSyncTask(), TimeSpan.FromMinutes(6), true);

                controller.Schedule(new LoadProfileBalancesTask(), TimeSpan.FromMinutes(6), true);

                controller.Start();
            }));

            //This will never return
            Task.WaitAll(tasks.ToArray());
        }
    }
}