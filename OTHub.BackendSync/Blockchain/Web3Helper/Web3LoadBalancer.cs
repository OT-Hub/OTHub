using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using Nethereum.BlockchainProcessing.Services;
using Nethereum.Contracts.Services;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC;
using Nethereum.RPC.TransactionManagers;
using Nethereum.Web3;
using OTHub.BackendSync.Database.Models;
using OTHub.Settings;

namespace OTHub.BackendSync.Blockchain.Web3Helper
{
    public struct GetBlockResult
    {
        public HexBigInteger BlockNumber { get; set; }
        public TimeSpan Duration { get; set; }

        public GetBlockResult(HexBigInteger blockNumber, TimeSpan duration)
        {
            BlockNumber = blockNumber;
            Duration = duration;
        }
    }

    public class Web3LoadBalancer : IWeb3
    {
        private readonly Random _rand = new Random();
        private Web3RpcEndpoint[] _endpoints;
        private int _endDistribution;

        public Web3LoadBalancer()
        {

        }

        public async Task SetupWeb3ForRequests(MySqlConnection connection, BlockchainType type, Rpc[] rpcs)
        {
            ConcurrentDictionary<Rpc, GetBlockResult> rpcsToBlockNumberDict = new ConcurrentDictionary<Rpc, GetBlockResult>();

            IEnumerable<Task> tasks = rpcs.Select(async rpc =>
            {
                var start = DateTime.UtcNow;

                Web3 web3 = new Web3(rpc.Url);

                HexBigInteger latestBlockNumber = null;

                try
                {
                    latestBlockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    latestBlockNumber = new HexBigInteger(0);
                }
                finally
                {
                    var end = DateTime.UtcNow;

                    var diff = end - start;

                    rpcsToBlockNumberDict[rpc] = new GetBlockResult(latestBlockNumber, diff);
                }
            });

            await Task.WhenAll(tasks);

            foreach (var kvp in rpcsToBlockNumberDict)
            {
                if (kvp.Value.BlockNumber.Value != 0)
                {
                    await connection.ExecuteAsync(@"UPDATE rpcs SET LatestBlockNumber = @blockNumber WHERE ID = @id",
                        new
                        {
                            id = kvp.Key.ID,
                            blockNumber = (UInt64) kvp.Value.BlockNumber.Value
                        });
                }
            }

            KeyValuePair<Rpc, GetBlockResult>[] orderedByBlockNumberDesc = rpcsToBlockNumberDict.OrderByDescending(r => r.Value.BlockNumber.Value).ToArray();

            List<KeyValuePair<Rpc, GetBlockResult>> rpcsToRemoveAsBehindInBlocks = new List<KeyValuePair<Rpc, GetBlockResult>>();
            HexBigInteger maxBlockNumber = null;
            bool subtractOneFromMaxBlockNumber = false;
            for (var index = 0; index < orderedByBlockNumberDesc.Length; index++)
            {
                KeyValuePair<Rpc, GetBlockResult> keyValuePair = orderedByBlockNumberDesc[index];

                if (index == 0)
                {
                    maxBlockNumber = keyValuePair.Value.BlockNumber;
                }
                else
                {
                    if (maxBlockNumber.Value == keyValuePair.Value.BlockNumber)
                    {

                    }
                    else if (maxBlockNumber.Value - 1 == keyValuePair.Value.BlockNumber)
                    {
                        subtractOneFromMaxBlockNumber = true;
                    }
                    else
                    {
                        rpcsToRemoveAsBehindInBlocks.Add(keyValuePair);
                    }
                }
            }

            foreach (var rpcsToRemoveAsBehindInBlock in rpcsToRemoveAsBehindInBlocks)
            {
                Rpcshistory history = new Rpcshistory
                {
                    Timestamp = DateTime.UtcNow,
                    RPCID = rpcsToRemoveAsBehindInBlock.Key.ID,
                    Method = "eth_getBlockByNumber",
                    Success = false,
                    Duration = (int)rpcsToRemoveAsBehindInBlock.Value.Duration.TotalMilliseconds
                };

                await history.Insert(connection);

                rpcsToBlockNumberDict.Remove(rpcsToRemoveAsBehindInBlock.Key, out _);
            }

            foreach (var keyValuePair in rpcsToBlockNumberDict)
            {
                Rpcshistory history = new Rpcshistory
                {
                    Timestamp = DateTime.UtcNow,
                    RPCID = keyValuePair.Key.ID,
                    Method = "eth_getBlockByNumber",
                    Success = true,
                    Duration = (int)keyValuePair.Value.Duration.TotalMilliseconds
                };

                await history.Insert(connection);
            }

            if (subtractOneFromMaxBlockNumber)
            {
                maxBlockNumber = new HexBigInteger(maxBlockNumber.Value - 1);
            }

            LatestBlockNumber = maxBlockNumber;

            int defaultBlocksToIgnore = 3;

            if (type == BlockchainType.xDai)
            {
                defaultBlocksToIgnore = 12;
            }
            else if (type == BlockchainType.Polygon)
            {
                defaultBlocksToIgnore = 28;
            }
            else if (type == BlockchainType.Starfleet)
            {
                defaultBlocksToIgnore = 20;
            }

            LatestBlockNumber  = new HexBigInteger(LatestBlockNumber.Value - defaultBlocksToIgnore);

            _endpoints = rpcsToBlockNumberDict.Select(r => new Web3RpcEndpoint(r.Key)).ToArray();

            int startingDistribution = 0;
            foreach (Web3RpcEndpoint web3RpcEndpoint in _endpoints.OrderByDescending(e => e.Weight))
            {
                web3RpcEndpoint.LoadBalancerStartIndex = startingDistribution;
                web3RpcEndpoint.LoadBalancerEndndex = startingDistribution + web3RpcEndpoint.Weight;

                _endDistribution = web3RpcEndpoint.LoadBalancerEndndex;
                startingDistribution = web3RpcEndpoint.LoadBalancerEndndex + 1;
            }

            this.Client = (IClient)new CustomRpcClient(_endpoints, GetEndpoint, GetEndpointsToTryOnFailure);
            this.LoadServices();
        }

        private Web3RpcEndpoint GetEndpointsToTryOnFailure(List<Web3RpcEndpoint> triedEndpoints)
        {
            return _endpoints.Where(e => !triedEndpoints.Contains(e)).FirstOrDefault();
        }

        //TODO remove some blocks to avoid uncle blocks
        public HexBigInteger LatestBlockNumber { get; set; }

        private Web3RpcEndpoint GetEndpoint()
        {
            int randomNumber = _rand.Next(0, _endDistribution);

            Web3RpcEndpoint endpoint = _endpoints.Single(e =>
                e.LoadBalancerStartIndex <= randomNumber && e.LoadBalancerEndndex >= randomNumber);

            return endpoint;
        }

        private void LoadServices()
        {
            this.Eth = (IEthApiContractService)new EthApiContractService(this.Client);
            this.Processing = (IBlockchainProcessingService)new BlockchainProcessingService(this.Eth);
            this.Shh = (IShhApiService)new ShhApiService(this.Client);
            this.Net = (INetApiService)new NetApiService(this.Client);
            this.Personal = (IPersonalApiService)new PersonalApiService(this.Client);
        }

        public IClient Client { get; private set; }
        public IEthApiContractService Eth { get; private set; }
        public IBlockchainProcessingService Processing { get; private set; }
        public INetApiService Net { get; private set; }
        public IPersonalApiService Personal { get; private set; }
        public IShhApiService Shh { get; private set; }
        public ITransactionManager TransactionManager
        {
            get => this.Eth.TransactionManager;
            set => this.Eth.TransactionManager = value;
        }
    }
}