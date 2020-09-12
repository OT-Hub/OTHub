using System;
using System.Numerics;

namespace OTHub.APIServer.Sql.Models.Jobs
{
    public class OfferSummaryModel
    {
        public string DCIdentity { get; set; }
        public string OfferId { get; set; }
        public DateTime CreatedTimestamp { get; set; }
        public DateTime? FinalizedTimestamp { get; set; }
        public BigInteger DataSetSizeInBytes { get; set; }
        public BigInteger HoldingTimeInMinutes { get; set; }
        public String TokenAmountPerHolder { get; set; }
        public bool IsFinalized { get; set; }
        public String Status { get; set; }
        public DateTime? EndTimestamp { get; set; }
    }
}
