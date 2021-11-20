using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySqlConnector;
using Nethereum.JsonRpc.Client;
using Nethereum.Web3;
using OTHub.BackendSync.Database.Models;
using OTHub.BackendSync.Logging;
using OTHub.Settings;

namespace OTHub.BackendSync.Blockchain.Web3Helper
{
    public class Web3Factory
    {
        private readonly BlockchainType _type;
        private readonly Rpc[] _rpcs;

        private RPCInterceptor RequestInterceptor { get; }

        public Web3Factory(BlockchainType type, Rpc[] rpcs)
        {
            _type = type;
            _rpcs = rpcs;
            RequestInterceptor = new RPCInterceptor(_type);
        }

        public async Task<IWeb3> CreateInstance(MySqlConnection connection)
        {
            Web3LoadBalancer loadBalancer = new Web3LoadBalancer();
            await loadBalancer.SetupWeb3ForRequests(connection, _type, _rpcs);

            loadBalancer.Client.OverridingRequestInterceptor = RequestInterceptor;

            return loadBalancer;
        }
    }
}