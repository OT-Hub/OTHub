using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace OTHub.BackendSync.Blockchain.Models
{
    partial class Program
    {
        [Event("OfferCreated")]
        public class OfferCreated : IEventDTO
        {
            [Parameter("bytes32", "offerId", 1, false)]
            public byte[] offerId { get; set; }

            [Parameter("bytes32", "dataSetId", 2, false)]
            public byte[] dataSetId { get; set; }

            [Parameter("bytes32", "dcNodeId", 3, false)]
            public byte[] dcNodeId { get; set; }

            [Parameter("uint256", "holdingTimeInMinutes", 4, false)]
            public BigInteger holdingTimeInMinutes { get; set; }

            [Parameter("uint256", "dataSetSizeInBytes", 5, false)]
            public BigInteger dataSetSizeInBytes { get; set; }

            [Parameter("uint256", "tokenAmountPerHolder", 6, false)]
            public BigInteger tokenAmountPerHolder { get; set; }

            [Parameter("uint256", "litigationIntervalInMinutes", 7, false)]
            public BigInteger litigationIntervalInMinutes { get; set; }
        }
    }
}