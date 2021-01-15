using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using OTHub.BackendSync.Database.Models;
using OTHub.BackendSync.Ethereum.Tasks;
using OTHub.BackendSync.Logging;
using OTHub.BackendSync.Markets.Tasks;
using OTHub.BackendSync.System.Tasks;
using OTHub.Settings;

namespace OTHub.BackendSync
{
    partial class Program
    {
        static void Main(string[] args)
        {
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddUserSecrets<OTHubSettings>();
           
            IConfigurationRoot configuration = builder.Build();
         
            var settings = configuration.Get<OTHubSettings>();
            settings.Validate();

            Logger.WriteLine(Source.BlockchainSync, "Infura url: " + settings.Infura.Url);

            //Add any new tables, indexes, columns etc to the database. This can only be used to upgrade somewhat recent databases.
            DatabaseUpgradeTask task = new DatabaseUpgradeTask();
            task.Execute(Source.Startup).GetAwaiter().GetResult();

            using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                new SystemStatus(task.Name).InsertOrUpdate(connection, true, null, false);
            }

            //Get all the latest ethereum smart contracts before we even start up
            GetLatestContractsTask contracts = new GetLatestContractsTask();
            contracts.Execute(Source.Startup).GetAwaiter().GetResult();

            using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                new SystemStatus(contracts.Name).InsertOrUpdate(connection, true, null, false);
            }

            List<Task> tasks = new List<Task>();

            //Tasks controllers below allow grouping of background tasks which run on set timers.
            //Only 1 task in a task controller can run at a time. If multiple are scheduled at the same time
            //it will wait until the previous task has finished before executing the next.

            //Currently we have 3 task controllers for the following areas:
            //1. OriginTrail Node API usage (Very low on cpu usage)
            //2. Node online checks and misc tasks (Low to medium on cpu usage)
            //3. Blockchain sync (medium to high on cpu usage)

            //Sources don't have much impact when configuring a task controller. They are only just for logging purposes.

            //Task controller 1
            tasks.Add(Task.Run(() =>
            {
                TaskController controller = new TaskController(Source.NodeApi);

                controller.Schedule(new OptimiseDatabaseTask(), TimeSpan.FromDays(1), false);

                controller.Start();
            }));

            //Task controller 2
            tasks.Add(Task.Run(() =>
                {
                    TaskController controller = new TaskController(Source.NodeUptimeAndMisc);
                    controller.Schedule(new GetLatestContractsTask(), TimeSpan.FromMinutes(240), false);

                    if (OTHubSettings.Instance.Blockchain.Network == BlockchainNetwork.Mainnet)
                    {
                        controller.Schedule(new GetMarketDataTask(), TimeSpan.FromMinutes(120), true);

                        controller.Schedule(new CalculateOfferLambdaTask(), TimeSpan.FromMinutes(60), true);
                    }

                    controller.Schedule(new MarkOldContractsAsArchived(), TimeSpan.FromDays(1),
                        false); //TODO needs to do litigation contracts
                    
                    controller.Start();
                }));


            //Task controller 3
            tasks.Add(Task.Run(() =>
            {
                TaskController controller = new TaskController(Source.BlockchainSync);
                
                controller.Schedule(new RefreshAllHolderLitigationStatusesTask(), TimeSpan.FromHours(2), true);

                controller.Schedule(new BlockchainSyncTask(), TimeSpan.FromMinutes(5), true);
                controller.Schedule(new LoadProfileBalancesTask(), TimeSpan.FromMinutes(5), true);

                controller.Start();
            }));

            //This will never return
            Task.WaitAll(tasks.ToArray());
        }
    }
}