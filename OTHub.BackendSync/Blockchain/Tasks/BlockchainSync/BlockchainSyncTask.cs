using System;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using OTHub.BackendSync.Blockchain.Tasks.BlockchainSync.Children;
using OTHub.BackendSync.Logging;
using OTHub.Settings;

namespace OTHub.BackendSync.Blockchain.Tasks.BlockchainSync
{
    public class BlockchainSyncTask : TaskRunBlockchain
    {
        public BlockchainSyncTask() : base("Blockchain Sync")
        {
            Add(new SyncProfileContractTask());
            Add(new SyncHoldingContractTask());
            Add(new SyncLitigationContractTask());
            Add(new SyncReplacementContractTask());
            Add(new LoadProfileBalancesTask());
            Add(new ProcessJobsTask());
        }

        public override TimeSpan GetExecutingInterval(BlockchainType type)
        {
            if (type == BlockchainType.xDai)
            {
                return TimeSpan.FromMinutes(10);
            }

            return TimeSpan.FromMinutes(10);
        }

        public override async Task Execute(Source source, BlockchainType blockchain, BlockchainNetwork network)
        {
            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                int blockchainID = GetBlockchainID(connection, blockchain, network);

                await RunChildren(source, blockchain, network, blockchainID);
            }
        }

        public override async void BlockchainStartup(int blockchainId, BlockchainType blockchain,
            BlockchainNetwork network)
        {
            string websocketsUrl;
            string rpcUrl;

            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                rpcUrl = await connection.ExecuteScalarAsync<string>(@"SELECT BlockchainNodeUrl FROM Blockchains where id = @id", new
                {
                    id = blockchainId
                });
                websocketsUrl = await connection.ExecuteScalarAsync<string>(@"SELECT BlockchainWebSocketsUrl FROM Blockchains where id = @id", new
                {
                    id = blockchainId
                });
            }

            if (string.IsNullOrWhiteSpace(websocketsUrl))
                return;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () => await WebSocketsManager.Start(blockchainId, websocketsUrl, rpcUrl, blockchain, network));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }
    }
}