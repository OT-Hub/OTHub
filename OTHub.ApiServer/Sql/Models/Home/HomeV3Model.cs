using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nethereum.Util;

namespace OTHub.APIServer.Sql.Models.Home
{
    public class HomeV3Model
    {
        public HomeV3BlockchainModel All { get; set; }
        public HomeV3BlockchainModel[] Blockchains { get; set; }
        public decimal PercentChange24H { get; set; }
        public decimal PriceUsd { get; set; }
        public long CirculatingSupply { get; set; }
        public long MarketCapUsd { get; set; }
        public decimal Volume24HUsd { get; set; }
        public decimal PriceBtc { get; set; }
    }

    public class HomeV3BlockchainModel
    {
        public string LogoLocation { get; set; }
        public string BlockchainName { get; set; }
        public int ActiveNodes { get; set; }
        public long TotalJobs { get; set; }
        public long ActiveJobs { get; set; }
        public decimal StakedTokens { get; set; }
        public long Jobs24H { get; set; }
        public long? JobsReward24H { get; set; }
        public long? JobsDuration24H { get; set; }
        public long? JobsSize24H { get; set; }
        public string GasTicker { get; set; }

        public HomeFeesModel Fees { get; set; }
        public int BlockchainID { get; set; }
    }

    public class HomeFeesModel
    {
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