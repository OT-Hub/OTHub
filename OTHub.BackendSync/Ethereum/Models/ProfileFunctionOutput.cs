using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace OTHub.BackendSync.Ethereum.Models
{
    [FunctionOutput]
    public class ProfileFunctionOutput
    {
        [Parameter("uint256", "stake", 1, false)]
        public BigInteger stake { get; set; }

        [Parameter("uint256", "stakeReserved", 2, false)]
        public BigInteger stakeReserved { get; set; }

        [Parameter("uint256", "reputation", 3, false)]
        public BigInteger reputation { get; set; }

        [Parameter("bool", "withdrawalPending", 4, false)]
        public bool withdrawalPending { get; set; }

        [Parameter("uint256", "withdrawalTimestamp", 5, false)]
        public BigInteger withdrawalTimestamp { get; set; }

        [Parameter("uint256", "withdrawalAmount", 6, false)]
        public BigInteger withdrawalAmount { get; set; }

        [Parameter("bytes32", "nodeId", 7, false)]
        public byte[] nodeId { get; set; }
    }
}
