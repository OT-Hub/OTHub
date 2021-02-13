using System;

namespace OTHub.Settings
{
    //public class BlockchainSettings
    //{
    //    public BlockchainType Type { get; set; }
    //    public BlockchainNetwork Network { get; set; }

    //    //public String HubAddress { get; set; }

    //    //public uint StartSyncFromBlockNumber { get; set; }

    //    public void Validate()
    //    {
    //        //if (String.IsNullOrWhiteSpace(HubAddress))
    //        //{
    //        //    throw new Exception("HubAddress must be provided in Blockchain settings.");
    //        //}

    //        //if (StartSyncFromBlockNumber <= 1)
    //        //{
    //        //    throw new Exception("You should not try syncing the blockchain from the beginning to time. Mainnet ODN launched in December 2018, so using block number 6655078 is recommended for mainnet.");
    //        //}
    //    }
    //}

    public enum BlockchainNetwork
    {
        Mainnet,
        Rinkeby,
        Kovan
    }

    public enum BlockchainType
    {
        Ethereum,
        Starfleet
    }
}