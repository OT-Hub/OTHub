using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;

namespace OTHub.BackendSync.Blockchain.Web3Helper
{
    public static class Web3Extensions
    {
        public static HexBigInteger GetLoadBalancedBlockNumber(this IWeb3 web3)
        {
            Web3LoadBalancer loadBalancer = (Web3LoadBalancer) web3;

            return loadBalancer.LatestBlockNumber;
        }
    }
}