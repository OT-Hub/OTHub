using System;

namespace OTHub.APIServer.Models
{
    public abstract class NodeDetailedModel
    {
        public NodeProfileDetailedModel_ProfileTransfer[] ProfileTransfers { get; set; }

        public NodeUptimeHistory NodeUptime { get; set; }

        public String Identity { get; set; }
        public String NodeId { get; set; }
        public Int32 Version { get; set; }
        public String StakeTokens { get; set; }
        public String StakeReservedTokens { get; set; }
        public Boolean Approved { get; set; }
        public String OldIdentity { get; set; }
        public String NewIdentity { get; set; }
        public String ManagementWallet { get; set; }
        public String CreateTransactionHash { get; set; }
        public UInt64? CreateGasPrice { get; set; }
        public UInt64? CreateGasUsed { get; set; }
    }

    public class NodeDataCreatorDetailedModel : NodeDetailedModel
    {
        public OfferSummaryModel[] Offers { get; set; }
        public DataCreatorLitigationSummary[] Litigations { get; set; }
    }

    public class NodeOnlineResult
    {
        public String Header { get; set; }
        public String Message { get; set; }
        public Boolean Success { get; set; }
        public Boolean Error { get; set; }
        public Boolean Warning { get; set; }
    }

    public class BeforePayoutResult
    {
        public String Header { get; set; }
        public String Message { get; set; }
        public Boolean CanTryPayout { get; set; }
    }

    public class NodeDataHolderDetailedModel : NodeDetailedModel
    {
        public String PaidTokens { get; set; }
        public Int32 TotalWonOffers { get; set; }
        public Int32 WonOffersLast7Days { get; set; }

        public NodeProfileDetailedModel_OfferSummary[] Offers { get; set; }
        public NodeProfileDetailedModel_OfferPayout[] Payouts { get; set; }
        public DataHolderLitigationSummary[] Litigations { get; set; }
    }

    public class NodeUptimeHistory
    {
        public DateTime? LastSuccess { get; set; }
        public DateTime? LastCheck { get; set; }
        public int TotalSuccess24Hours { get; set; }
        public int TotalFailed24Hours { get; set; }
        public int TotalSuccess7Days { get; set; }
        public int TotalFailed7Days { get; set; }
        public string ChartData { get; set; }
    }

    public class NodeUptimeChartData
    {
        public DateTime Timestamp { get; set; }
        public Boolean Success { get; set; }

        public DateTime EndTimestamp
        {
            get
            {
                return Timestamp.AddMinutes(2);
            }
        }
    }


    public class NodeProfileDetailedModel_ProfileTransfer
    {
        public string Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public String TransactionHash { get; set; }
        public UInt64 GasUsed { get; set; }
        public UInt64 GasPrice { get; set; }
    }

    public class NodeProfileDetailedModel_OfferPayout
    {
        public string Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public String OfferId { get; set; }
        public String TransactionHash { get; set; }
        public UInt64 GasUsed { get; set; }
        public UInt64 GasPrice { get; set; }
    }

    public class NodeProfileDetailedModel_OfferSummary
    {
        public String OfferId { get; set; }
        public DateTime FinalizedTimestamp { get; set; }
        public UInt64 HoldingTimeInMinutes { get; set; }
        public Boolean Paidout { get; set; }
        public Boolean CanPayout { get; set; }
        public string TokenAmountPerHolder { get; set; }
        public DateTime EndTimestamp { get; set; }
        public String Status { get; set; }
        public bool IsOriginalHolder { get; set; }
    }

    public class DataHolderLitigationSummary
    {
        public String TransactionHash { get; set; }
        public DateTime Timestamp { get; set; }
        public String OfferId { get; set; }
        public UInt64 RequestedObjectIndex { get; set; }
        public UInt64 RequestedBlockIndex { get; set; }
    }

    public class DataCreatorLitigationSummary
    {
        public String TransactionHash { get; set; }
        public DateTime Timestamp { get; set; }
        public String OfferId { get; set; }
        public UInt64 RequestedObjectIndex { get; set; }
        public UInt64 RequestedBlockIndex { get; set; }
        public String HolderIdentity { get; set; }
    }
}