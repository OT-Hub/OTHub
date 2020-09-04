using System;

namespace OTHub.APIServer.Sql.Models.Home
{
    public class HomePayoutsInfo
    {
        public Decimal PayoutsTotal { get; set; }
        public Decimal PayoutsLast7Days { get; set; }
        public Decimal PayoutsLast24Hours { get; set; }
    }
}
