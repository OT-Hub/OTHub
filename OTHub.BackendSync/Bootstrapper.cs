using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OTHub.BackendSync.Blockchain.Tasks.BlockchainMaintenance;
using OTHub.BackendSync.Blockchain.Tasks.BlockchainSync;
using OTHub.BackendSync.Blockchain.Tasks.Misc;
using OTHub.BackendSync.Blockchain.Tasks.Tools;
using OTHub.BackendSync.Logging;
using OTHub.BackendSync.System.Tasks;

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

                controller.Schedule(new MiscTask(), TimeSpan.FromHours(10), true);

                await controller.Start();
            }));


            tasks.Add(Task.Run(async () =>
            {
                TaskController controller = new TaskController(Source.BlockchainSync);

                controller.Schedule(new BlockchainMaintenanceTask(), true, true);

                await controller.Start();
            }));

            tasks.Add(Task.Run(async () =>
            {
                TaskController controller = new TaskController(Source.BlockchainSync);

                controller.Schedule(new BlockchainSyncTask(), true, true);

                await controller.Start();
            }));

            tasks.Add(Task.Run(async () =>
            {
                TaskController controller = new TaskController(Source.Tools);

                controller.Schedule(new ToolsTask(), true, false);

                await controller.Start();
            }));

            tasks.Add(Task.Run(async () =>
            {
                TaskController controller = new TaskController(Source.Tools);

                controller.Schedule(new RabbitMQMonitoringTask(), TimeSpan.FromMinutes(1), false);

                await controller.Start();
            }));



            //This will never return
            Task.WaitAll(tasks.ToArray());
        }
    }
}