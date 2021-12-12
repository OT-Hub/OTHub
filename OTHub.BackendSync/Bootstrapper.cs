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

                controller.Schedule(new RPCWeightAdjustorTask(), TimeSpan.FromMinutes(5), true);
                controller.Schedule(new MiscTask(), TimeSpan.FromHours(10), true);
           

                await controller.Start();
            }));

            tasks.AddRange(TaskController.Schedule<BlockchainMaintenanceTask>(Source.BlockchainSync, true, out _));

            Task[] syncBlockchainTasks = TaskController.Schedule<BlockchainSyncTask>(Source.BlockchainSync, true, out TaskController.TaskControllerItem[] blockchainSyncTaskControllers);

            BlockchainSyncTimeAdjustorTask.BlockchainSyncTaskControllers = blockchainSyncTaskControllers;

            tasks.AddRange(syncBlockchainTasks);

            tasks.AddRange(TaskController.Schedule<ToolsTask>(Source.Tools, false, out _));

            tasks.AddRange(TaskController.Schedule<BlockchainSyncTimeAdjustorTask>(Source.BlockchainSync, true, out _));

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