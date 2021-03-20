using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OTHub.BackendSync.Blockchain.Tasks;
using OTHub.BackendSync.Blockchain.Tasks.BlockchainMaintenance;
using OTHub.BackendSync.Blockchain.Tasks.BlockchainSync;
using OTHub.BackendSync.Blockchain.Tasks.Misc;
using OTHub.BackendSync.Logging;
using OTHub.BackendSync.Markets.Tasks;

namespace OTHub.BackendSync
{
    public class Bootstrapper
    {
        public void RunUntilExit()
        {
            List<Task> tasks = new List<Task>();

            tasks.Add(Task.Run(async () =>
            {
                TaskController controller = new TaskController(Source.NodeUptimeAndMisc);

                controller.Schedule(new MiscTask(), TimeSpan.FromHours(6), true);

                //controller.Schedule(new BoardingContractSyncTask(), TimeSpan.FromMinutes(5), true);

                await controller.Start();
            }));


            tasks.Add(Task.Run(async () =>
            {
                TaskController controller = new TaskController(Source.BlockchainSync);

                controller.Schedule(new BlockchainMaintenanceTask(), TimeSpan.FromHours(3), true);
                controller.Schedule(new BlockchainSyncTask(), TimeSpan.FromMinutes(6), true);

                await controller.Start();
            }));

            //This will never return
            Task.WaitAll(tasks.ToArray());
        }
    }
}