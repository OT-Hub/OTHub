using System;

namespace OTHub.APIServer.Sql.Models.Nodes
{
    public class PayoutUSDModel
    {
        public string OfferID { get; set; }
        public Decimal TRACAmount { get; set; }
        public Decimal USDAmount { get; set; }
        public DateTime PayoutTimestamp { get; set; }
        public DateTime TickerTimestamp { get; set; }
        public Decimal TickerUSDPrice { get; set; }
    }
}
