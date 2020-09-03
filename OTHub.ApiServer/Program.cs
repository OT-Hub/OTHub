using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using OTHub.APIServer.Models;
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
        
        public static string GetContractAbi(ContractType contractType)
        {
            string path = "OTHub.APIServer.Abis.";

            switch (contractType)
            {
                case ContractType.Approval:
                    path += "approval.json";
                    break;
                case ContractType.Holding:
                    path += "holding.json";
                    break;
                case ContractType.HoldingStorage:
                    path += "holding-storage.json";
                    break;
                case ContractType.Profile:
                    path += "profile.json";
                    break;
                case ContractType.ProfileStorage:
                    path += "profile-storage.json";
                    break;
                case ContractType.Replacement:
                    path += "replacement.json";
                    break;
                case ContractType.Litigation:
                    path += "litigation.json";
                    break;
                case ContractType.LitigationStorage:
                    path += "litigation-storage.json";
                    break;
                case ContractType.Token:
                    path += "token.json";
                    break;
                case ContractType.ERC725:
                    path += "erc725.json";
                    break;
                default:
                    throw new Exception("Not supported: " + contractType);
            }

            using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(path))
            using (StreamReader reader = new StreamReader(resource))
            {
                return reader.ReadToEnd();
            }
        }
    }

    public enum ContractType
    {
        Approval,
        Profile,
        ReadingStorage, //unused
        Reading, //unused
        Token,
        HoldingStorage,
        Holding,
        ProfileStorage,
        Litigation,
        LitigationStorage,
        Replacement,
        ERC725
    }

}
