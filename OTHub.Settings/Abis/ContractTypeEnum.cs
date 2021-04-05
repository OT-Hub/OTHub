using System;

namespace OTHub.Settings.Abis
{
    public enum ContractTypeEnum
    {
        [Obsolete]
        Approval,
        Profile,
        [Obsolete]
        ReadingStorage,
        Reading, //unused
        Token,
        HoldingStorage,
        Holding,
        ProfileStorage,
        Litigation,
        LitigationStorage,
        Replacement,
        ERC725,
        Hub,
        StarfleetStake,
        StarfleetBounty
    }
}
