using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nethereum.Util;

namespace OTHub.APIServer.Sql.Models.Nodes
{
    public class TaxReportModel
    {
        public string OfferID { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public DateTime TickerTimestamp { get; set; }
        public decimal TickerUSDPrice { get; set; }
        public decimal USDAmount { get; set; }
    }
}
