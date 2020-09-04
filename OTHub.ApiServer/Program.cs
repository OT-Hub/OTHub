using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using OTHub.Settings;

namespace OTHub.APIServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = BuildWebHost(args);

            host.Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseKestrel(o => o.AddServerHeader = false)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder.AddUserSecrets<OTHubSettings>();
                    builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    builder.AddEnvironmentVariables();
                })
                .Build();
    }
}
