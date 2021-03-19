using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Nethereum.JsonRpc.Client;
using OTHub.BackendSync.Logging;
using OTHub.BackendSync.Markets.Tasks;
using OTHub.BackendSync.System.Tasks;
using OTHub.Settings;

namespace OTHub.BackendSync
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfigurationBuilder builder = new ConfigurationBuilder();
            //builder.AddUserSecrets<OTHubSettings>();
            builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
           


            IConfigurationRoot configuration = builder.Build();

            var settings = new OTHubSettings();
            configuration.Bind("OTHub", settings);

            settings.Validate();

            //Add any new tables, indexes, columns etc to the database. This can only be used to upgrade somewhat recent databases.
            DatabaseUpgradeTask.Execute();

            Bootstrapper bootstrapper = new Bootstrapper();

            bootstrapper.RunUntilExit();
        }
    }
}