using System;
using System.Collections.Generic;

namespace OTHub.APIServer.Sql.Models.System
{
    public class SystemStatusItem
    {
        public String Name { get; set; }
        public DateTime? LastSuccessDateTime { get; set; }
        public DateTime? LastTriedDateTime { get; set; }
        public bool Success { get; set; }
        public bool IsRunning { get; set; }
        public DateTime? NextRunDateTime { get; set; }
        public string BlockchainName { get; set; }
        public string NetworkName { get; set; }
        public string ParentName { get; set; }

        public List<SystemStatusItem> Children { get; set; } = new List<SystemStatusItem>();
    }
}
