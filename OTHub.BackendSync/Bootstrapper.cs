using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OTHub.BackendSync.Blockchain.Tasks;
using OTHub.BackendSync.Blockchain.Tasks.BlockchainMaintenance;
using OTHub.BackendSync.Blockchain.Tasks.BlockchainSync;
using OTHub.BackendSync.Blockchain.Tasks.Misc;
using OTHub.BackendSync.Blockchain.Tasks.Misc.Children;
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
                TaskController controller = new TaskController(Source.Misc);

                controller.Schedule(new MiscTask(), TimeSpan.FromHours(6), true);

                await controller.Start();
            }));


            tasks.Add(Task.Run(async () =>
            {
                TaskController controller = new TaskController(Source.BlockchainSync);

                controller.Schedule(new BlockchainMaintenanceTask(), true);
                controller.Schedule(new BlockchainSyncTask(), true);

                await controller.Start();
            }));

            tasks.Add(Task.Run(async () =>
            {
                TaskController controller = new TaskController(Source.Startup);

                controller.Schedule(new xDaiBountyTask(), TimeSpan.FromMinutes(10), true);

                await controller.Start();
            }));


            //This will never return
            Task.WaitAll(tasks.ToArray());
        }
    }
}