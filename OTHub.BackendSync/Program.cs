using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Newtonsoft.Json;
using OTHelperNetStandard.Models.Database;
using OTHelperNetStandard.Tasks;
using OTHub.Settings;

namespace OTHelperNetStandard
{
    partial class Program
    {
        private static readonly object _getEthBlockLock = new object();
        public static async Task<EthBlock> GetEthBlock(MySqlConnection connection, string blockHash, HexBigInteger blockNumber, Web3 cl)
        {
            var block = EthBlock.GetByNumber(connection, (UInt64)blockNumber.Value);

            if (block == null)
            {
                lock (_getEthBlockLock)
                {
                    block = EthBlock.GetByNumber(connection, (UInt64)blockNumber.Value);

                    if (block == null)
                    {
                        var apiBlock = cl.Eth.Blocks.GetBlockWithTransactionsHashesByNumber.SendRequestAsync(blockNumber).GetAwaiter().GetResult();

                        block = new EthBlock
                        {
                            BlockHash = blockHash,
                            BlockNumber = (UInt64)blockNumber.Value,
                            Timestamp = UnixTimeStampToDateTime((double)apiBlock.Timestamp.Value)
                        };

                        EthBlock.Insert(connection, block);
                    }
                }
            }

            return block;
        }

        static void Main(string[] args)
        {
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddUserSecrets<OTHubSettings>();

            IConfigurationRoot configuration = builder.Build();

            var settings = configuration.Get<OTHubSettings>();

            settings.Validate();

            DatabaseUpgradeTask task = new DatabaseUpgradeTask();
            task.Execute(Source.Startup).GetAwaiter().GetResult();

            GetLatestContractsTask contracts = new GetLatestContractsTask();
            contracts.Execute(Source.Startup).GetAwaiter().GetResult();

            List<Task> tasks = new List<Task>();


            tasks.Add(Task.Run(() =>
            {
                TaskController controller = new TaskController(Source.NodeApi);
                controller.Schedule(new LoadNodesViaAPITask(), TimeSpan.FromHours(16), true);
                controller.Start();
            }));

            tasks.Add(Task.Run(() =>
                {
                    TaskController controller = new TaskController(Source.NodeUptimeAndMisc);
                    controller.Schedule(new GetLatestContractsTask(), TimeSpan.FromMinutes(240), false);

                    controller.Schedule(new GetMarketDataTask(), TimeSpan.FromMinutes(360), true);

                    int upTimeCheckInMinutes = 50;

                    controller.Schedule(new LoadPeercacheTask(), TimeSpan.FromMinutes(upTimeCheckInMinutes), true);

                    controller.Schedule(new MarkOldContractsAsArchived(), TimeSpan.FromDays(1),
                        true); //TODO needs to do litigation contracts

                    //controller.Schedule(new GetMarketDataTask(), TimeSpan.FromHours(20), true);
                    controller.Start();
                }));


            tasks.Add(Task.Run(() =>
            {
                TaskController controller = new TaskController(Source.BlockchainSync);

                //TODO add this back in for testnet
                //controller.Schedule(new RefreshAllHolderLitigationStatuses(), TimeSpan.FromDays(1), true);

                controller.Schedule(new BlockchainSyncTask(), TimeSpan.FromMinutes(2), true);
                controller.Schedule(new LoadIdentitiesTask(), TimeSpan.FromMinutes(2), true);

                controller.Start();
            }));

            Task.WaitAll(tasks.ToArray());
        }






        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
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
        ERC725,
        Hub
    }
}