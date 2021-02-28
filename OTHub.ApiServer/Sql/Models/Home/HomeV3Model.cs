using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTHub.APIServer.Sql.Models.Home
{
    public class HomeV3Model
    {
        public int ActiveNodes { get; set; }
        public long TotalJobs { get; set; }
        public long ActiveJobs { get; set; }
        public string StakedTokens { get; set; }
        public long Jobs24H { get; set; }

        public HomeFeesModel[] FeesByBlockchain { get; set; }
        public HomeStakedModel[] StakedByBlockchain { get; set; }
    }

    public class HomeFeesModel
    {
        public String BlockchainName { get; set; }
        public string NetworkName { get; set; }
        public bool ShowCostInUSD { get; set; }
        public decimal JobCreationCost { get; set; }
        public decimal JobFinalisedCost { get; set; }
    }

    public class HomeStakedModel
    {
        public String BlockchainName { get; set; }
        public string NetworkName { get; set; }
        public string StakedTokens { get; set; }
    }
}