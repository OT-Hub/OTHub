using System;

namespace OTHub.APIServer.Sql.Models.Nodes
{
    public abstract class NodeDetailedModel
    {
        //public NodeProfileDetailedModel_ProfileTransfer[] ProfileTransfers { get; set; }

        //public NodeUptimeHistory NodeUptime { get; set; }

        //public String Identity { get; set; }
        public String NodeId { get; set; }
        public Int32 Version { get; set; }
        public String StakeTokens { get; set; }
        public String StakeReservedTokens { get; set; }
        //public Boolean Approved { get; set; }
        //public String OldIdentity { get; set; }
        //public String NewIdentity { get; set; }
        //public String ManagementWallet { get; set; }
        //public String CreateTransactionHash { get; set; }
        //public UInt64? CreateGasPrice { get; set; }
        //public UInt64? CreateGasUsed { get; set; }

        public NodeDetailedIdentity[] Identities { get; set; }
    }

    public class NodeDetailedIdentity
    {
        public string Identity { get; set; }

        public string BlockchainName { get; set; }
        public string NetworkName { get; set; }
    }
}