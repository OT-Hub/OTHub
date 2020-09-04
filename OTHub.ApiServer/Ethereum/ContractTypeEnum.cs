using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTHub.APIServer.Ethereum
{
    public enum ContractTypeEnum
    {
        Approval,
        Profile,
        ReadingStorage, //unused
        Reading, //unused
        Token,
        HoldingStorage,
        Holding,
        ProfileStorage,
        Litigation,
        LitigationStorage,
        Replacement,
        ERC725
    }
}
