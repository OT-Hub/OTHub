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
        public long? JobsReward24H { get; set; }
        public long? JobsDuration24H { get; set; }
        public long? JobsSize24H { get; set; }
        
        public HomeFeesModel[] FeesByBlockchain { get; set; }
        public HomeStakedModel[] StakedByBlockchain { get; set; }
        public HomeJobsModel[] TotalJobsByBlockchain { get; set; }
        public HomeJobsModel[] Jobs24HByBlockchain { get; set; }
        //public HomeJobBlockchainDistributionModel[] JobBlockchainDistribution { get; set; }
        public decimal PercentChange24H { get; set; }
        public decimal PriceUsd { get; set; }
        public long CirculatingSupply { get; set; }
    }

    public class HomeFeesModel
    {
        public String BlockchainName { get; set; }
        public bool ShowCostInUSD { get; set; }
        public decimal? JobCreationCost { get; set; }
        public decimal? JobFinalisedCost { get; set; }
        public decimal? PayoutCost { get; set; }
    }

    public class HomeStakedModel
    {
        public String BlockchainName { get; set; }
        public string StakedTokens { get; set; }
    }

    public class HomeJobsModel
    {
        public String BlockchainName { get; set; }
        public int Jobs { get; set; }
    }

    public class HomeJobBlockchainDistributionModel
    {
        public String DisplayName { get; set; }
        public string Color { get; set; }
        public int Jobs { get; set; }
        public int Percentage { get; set; }
    }

}