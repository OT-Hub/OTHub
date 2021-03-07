using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTHub.APIServer.Sql.Models.Contracts
{
    public class CompactContractModelGroup
    {
        public string HubAddress { get; set; }
        public string Name { get; set; }

        public List<CompactContractModel> Items { get; set; }
    }

    public class CompactContractModel
    {
        public string HubAddress { get; set; }
        public string BlockchainDisplayName { get; set; }
        public string Address { get; set; }
        public int Type { get; set; }
        public bool IsLatest { get; set; }
        public uint FromBlockNumber { get; set; }
        public bool IsArchived { get; set; }
        public uint SyncBlockNumber { get; set; }
        public DateTime? LastSyncedTimestamp { get; set; }
    }
}