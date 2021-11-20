using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OTHub.BackendSync.Database.Models;

namespace OTHub.BackendSync.Blockchain.Web3Helper
{
    public class Web3RpcEndpoint
    {
        public Uri Url { get; }

        public Web3RpcEndpoint(Rpc model)
        {
            Url = new Uri(model.Url);
            Weight = model.Weight;
        }

        public int Weight { get; }
        public int LoadBalancerStartIndex { get; set; }
        public int LoadBalancerEndndex { get; set; }
    }
}