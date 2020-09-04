using System;

namespace OTHub.APIServer.Sql.Models.Home
{
    public class HomeLitigationsInfo
    {
        public Int32 LitigationsTotal { get; set; }
        public Int32 Litigations7Days { get; set; }
        public Int32 Litigations7DaysPenalized { get; set; }
        public Int32 Litigations7DaysNotPenalized { get; set; }
        public Int32 Litigations1Month { get; set; }
        public Int32 Litigations1MonthPenalized { get; set; }
        public Int32 Litigations1MonthNotPenalized { get; set; }
        public Int32 LitigationsActiveLastHour { get; set; }
    }
}
