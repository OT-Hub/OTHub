using System;

namespace OTHub.APIServer.Sql.Models.Home
{
    public class HomeMarketInfo
    {
        public Decimal USDValue { get; set; }
        public Decimal MarketCap { get; set; }
        public Decimal Change24Hours { get; set; }
    }
}
