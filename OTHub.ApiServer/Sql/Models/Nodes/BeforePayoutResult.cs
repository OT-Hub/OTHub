using System;

namespace OTHub.APIServer.Sql.Models.Nodes
{
    public class BeforePayoutResult
    {
        public String Header { get; set; }
        public String Message { get; set; }
        public Boolean CanTryPayout { get; set; }
    }
}