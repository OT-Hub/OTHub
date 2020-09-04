using System;

namespace OTHub.APIServer.Sql.Models.System
{
    public class SystemStatusItem
    {
        public String Name { get; set; }
        public DateTime? LastSuccessDateTime { get; set; }
        public DateTime LastTriedDateTime { get; set; }
        public bool Success { get; set; }
    }
}
