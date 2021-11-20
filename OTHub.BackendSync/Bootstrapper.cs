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

            tasks.AddRange(TaskController.Schedule<BlockchainMaintenanceTask>(Source.BlockchainSync, false));

            tasks.AddRange(TaskController.Schedule<BlockchainSyncTask>(Source.BlockchainSync, true));

            tasks.AddRange(TaskController.Schedule<ToolsTask>(Source.Tools, false));


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