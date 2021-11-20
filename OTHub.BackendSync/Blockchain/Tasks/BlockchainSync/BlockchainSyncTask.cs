﻿using System;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using Nethereum.Web3;
using OTHub.BackendSync.Blockchain.Tasks.BlockchainSync.Children;
using OTHub.BackendSync.Logging;
using OTHub.Settings;
using OTHub.Settings.Constants;

namespace OTHub.BackendSync.Blockchain.Tasks.BlockchainSync
{
    public class BlockchainSyncTask : TaskRunBlockchain
    {
        public BlockchainSyncTask() : base(TaskNames.BlockchainSync)
        {
            Add(new SyncProfileContractTask());
            Add(new SyncHoldingContractTask());
            Add(new ProcessJobsTask());
            Add(new SyncLitigationContractTask());
            Add(new SyncReplacementContractTask());
            Add(new LoadProfileBalancesTask());
        }

        public override TimeSpan GetExecutingInterval(BlockchainType type)
        {
            return TimeSpan.FromMinutes(6);
        }

        public override async Task<bool> Execute(Source source, BlockchainType blockchain, BlockchainNetwork network, IWeb3 web3, int blockchainID)
        {
            return await RunChildren(source, blockchain, network, blockchainID, web3);
        }

        public override async void BlockchainStartup(int blockchainId, BlockchainType blockchain,
            BlockchainNetwork network)
        {
            //            string websocketsUrl;
            //            string rpcUrl;

            //            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            //            {
            //                rpcUrl = await connection.ExecuteScalarAsync<string>(@"SELECT BlockchainNodeUrl FROM Blockchains where id = @id", new
            //                {
            //                    id = blockchainId
            //                });
            //                websocketsUrl = await connection.ExecuteScalarAsync<string>(@"SELECT BlockchainWebSocketsUrl FROM Blockchains where id = @id", new
            //                {
            //                    id = blockchainId
            //                });
            //            }

            //            Console.WriteLine(blockchain +  " RPC: " + rpcUrl);
            //            Console.WriteLine(blockchain + " WS: " + websocketsUrl);

            //            if (string.IsNullOrWhiteSpace(websocketsUrl))
            //                return;

            //#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            //            Task.Run(async () => await WebSocketsManager.Start(blockchainId, websocketsUrl, rpcUrl, blockchain, network));
            //#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }
    }
}