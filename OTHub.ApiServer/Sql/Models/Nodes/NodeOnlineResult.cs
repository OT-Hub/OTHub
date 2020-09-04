using System;

namespace OTHub.APIServer.Sql.Models.Nodes
{
    public class NodeOnlineResult
    {
        public String Header { get; set; }
        public String Message { get; set; }
        public Boolean Success { get; set; }
        public Boolean Error { get; set; }
        public Boolean Warning { get; set; }
    }
}