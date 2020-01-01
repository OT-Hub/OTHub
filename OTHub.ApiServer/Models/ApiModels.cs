using System;
using System.Numerics;

namespace OTHub.APIServer.Models
{
    public class PayoutUSDModel
    {
        public string OfferID { get; set; }
        public Decimal TRACAmount { get; set; }
        public Decimal USDAmount { get; set; }
        public DateTime PayoutTimestamp { get; set; }
        public DateTime TickerTimestamp { get; set; }
        public Decimal TickerUSDPrice { get; set; }
    }

    public class HomeJobsChartDataModel
    {
        public DateTime Date { get; set; }
        public Int32 NewJobs { get; set; }
        public Int32 ActiveJobs { get; set; }
    }

    public class HomeJobsChartData
    {
        public String[] Labels { get; set; }
        public Int32[] NewJobs { get; set; }
        public Int32[] ActiveJobs { get; set; }
    }

    public class HomeNodesChartDataModel
    {
        public DateTime Date { get; set; }
        public Int32 OnlineNodes { get; set; }
        public Int32 DataCreatorNodes { get; set; }
        public Int32 ApprovedNodes { get; set; }
    }

    public class HomeNodesChartData
    {
        public String[] Labels { get; set; }
        public Int32[] OnlineNodes { get; set; }
        public Int32[] DataCreatorNodes { get; set; }
        public int[] ApprovedNodes { get; set; }
    }

    public class OfferSummaryWithPaging
    {
        public int draw { get; set; }
        public int recordsTotal { get; set; }
        public int recordsFiltered { get; set; }

        public OfferSummaryModel[] data { get; set; }
    }

    public class RecentActivityJobModel
    {
        public String Identity { get; set; }
        public String OfferId { get; set; }
        public DateTime Timestamp { get; set; }
        public String TokenAmountPerHolder { get; set; }
        public DateTime EndTimestamp { get; set; }
    }

    public class OfferSummaryModel
    {
        public string DCIdentity { get; set; }
        public string OfferId { get; set; }
        public DateTime Timestamp { get; set; }
        public BigInteger DataSetSizeInBytes { get; set; }
        public BigInteger HoldingTimeInMinutes { get; set; }
        public String TokenAmountPerHolder { get; set; }
        public bool IsFinalized { get; set; }
        public String Status { get; set; }
        public DateTime? EndTimestamp { get; set; }
    }

    public class RecentPayoutGasPrice
    {
        public Decimal GasPrice { get; set; }
        public Decimal GasUsed { get; set; }
        public Int32 TotalCount { get; set; }
    }

    public class OfferDetailedModel : OfferSummaryModel
    {
        public String DataSetId { get; set; }
        public UInt64 CreatedBlockNumber { get; set; }
        public string CreatedTransactionHash { get; set; }
        public string DCNodeId { get; set; }
        public string DCIdentity { get; set; }
        public OfferDetailedHolderModel[] Holders { get; set; }
        public Int32 OffersTotal { get; set; }
        public Int32 OffersLast7Days { get; set; }
        public String PaidoutTokensTotal { get; set; }
        public DateTime? FinalizedTimestamp { get; set; }
        public UInt64? FinalizedBlockNumber { get; set; }
        public String FinalizedTransactionHash { get; set; }
        public UInt64 CreatedGasUsed { get; set; }
        public UInt64 CreatedGasPrice { get; set; }
        public UInt64? FinalizedGasUsed { get; set; }
        public UInt64? FinalizedGasPrice { get; set; }
        public UInt64 LitigationIntervalInMinutes { get; set; }
        public OfferDetailedTimelineModel[] Timeline { get; set; }
        public Decimal? EstimatedLambda { get; set; }
    }

    public class OfferDetailedHolderModel
    {
        public String Identity { get; set; }
        public Int32? LitigationStatus { get; set; }
        public String LitigationStatusText { get; set; }
    }

    public class OfferDetailedTimelineModel
    {
        public DateTime Timestamp { get; set; }
        public string Name { get; set; }
        public string RelatedTo { get; set; }
    }

    public class NodeDataCreatorSummaryModel
    {
        public String Identity { get; set; }
        public String NodeId { get; set; }
        public Int32 Version { get; set; }
        public String StakeTokens { get; set; }
        public String StakeReservedTokens { get; set; }
        public Boolean Approved { get; set; }
        public Int32 OffersTotal { get; set; }
        public Int32 OffersLast7Days { get; set; }
        public Int32 AvgDataSetSizeKB { get; set; }
        public Int32 AvgHoldingTimeInMinutes { get; set; }
        public Int32 AvgTokenAmountPerHolder { get; set; }
        public DateTime CreatedTimestamp { get; set; }
    }

    public class NodeDataHolderSummaryModel
    {
        public String Identity { get; set; }
        public String NodeId { get; set; }
        public Int32 Version { get; set; }
        public String StakeTokens { get; set; }
        public String StakeReservedTokens { get; set; }
        public String PaidTokens { get; set; }
        public Int32 ActiveOffers { get; set; }
        public Int32 TotalWonOffers { get; set; }
        public Int32 WonOffersLast7Days { get; set; }
        public Boolean Approved { get; set; }
    }

    public class HomeModel
    {
        public HomeMarketInfo MarketInfo { get; set; }
        public HomeOffersInfo OffersInfo { get; set; }
        public HomePayoutsInfo PayoutsInfo { get; set; }
        public HomeNodesInfo NodesInfo { get; set; }
        public HomeLitigationsInfo LitigationsInfo { get; set; }
    }

    public class HomeMarketInfo
    {
        public Decimal USDValue { get; set; }
        public Decimal MarketCap { get; set; }
        public Decimal Change24Hours { get; set; }
    }

    public class HomeOffersInfo
    {
        public Int32 OffersTotal { get; set; }
        public Int32 OffersActive { get; set; }
        public Int32 OffersLast7Days { get; set; }
        public Int32 OffersLast24Hours { get; set; }
    }

    public class HomeLitigationsInfo
    {
        public Int32 LitigationsTotal { get; set; }
        public Int32 Litigations7Days { get; set; }
        public Int32 Litigations7DaysPenalized { get; set; }
        public Int32 Litigations7DaysNotPenalized { get; set; }
        public Int32 Litigations1Month { get; set; }
        public Int32 Litigations1MonthPenalized { get; set; }
        public Int32 Litigations1MonthNotPenalized { get; set; }
        public Int32 LitigationsActiveLastHour { get; set; }
    }

    public class HomePayoutsInfo
    {
        public Decimal PayoutsTotal { get; set; }
        public Decimal PayoutsLast7Days { get; set; }
        public Decimal PayoutsLast24Hours { get; set; }
    }

    public class HomeNodesInfo
    {
        public Int32 OnlineNodesCount { get; set; }
        public Int32 ApprovedNodesCount { get; set; }
        public Int32 NodesWithActiveJobs { get; set; }
        public Int32 NodesWithJobsThisWeek { get; set; }
        public Int32 NodesWithJobsThisMonth { get; set; }
        public Decimal StakedTokensTotal { get; set; }
        public Decimal LockedTokensTotal { get; set; }
        public DateTime? LastApprovalTimestamp { get; set; }
        public int? LastApprovalAmount { get; set; }
    }
}