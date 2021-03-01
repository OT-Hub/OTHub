using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
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
            builder.AddUserSecrets<OTHubSettings>();
           
            IConfigurationRoot configuration = builder.Build();
         
            var settings = configuration.Get<OTHubSettings>();
            settings.Validate();

            //Add any new tables, indexes, columns etc to the database. This can only be used to upgrade somewhat recent databases.
            DatabaseUpgradeTask.Execute();

            Bootstrapper bootstrapper = new Bootstrapper();

            bootstrapper.RunUntilExit();
        }
    }
}