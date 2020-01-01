using System;

namespace OTHub.BackendSync.Models.Json
{
    public class MarketValueByDayJson
    {
        public DateTime time_open { get; set; }
        public decimal open { get; set; }
        public decimal high { get; set; }
        public decimal low { get; set; }
        public decimal close { get; set; }
        public int volume { get; set; }
        public int market_cap { get; set; }
    }
}