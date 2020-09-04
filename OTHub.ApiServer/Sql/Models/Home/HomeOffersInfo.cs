using System;

namespace OTHub.APIServer.Sql.Models.Home
{
    public class HomeOffersInfo
    {
        public Int32 OffersTotal { get; set; }
        public Int32 OffersActive { get; set; }
        public Int32 OffersLast7Days { get; set; }
        public Int32 OffersLast24Hours { get; set; }
    }
}
