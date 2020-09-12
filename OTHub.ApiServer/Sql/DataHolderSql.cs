using System;

namespace OTHub.APIServer.Sql
{
    public static class DataHolderSql
    {
        public const String GetUSDPayoutsForDataHolder = @"SELECT po.OfferID, (CAST(`Amount` AS CHAR)+0) TRACAmount, po.Timestamp PayoutTimestamp, ticker.Timestamp TickerTimestamp, ticker.Price TickerUSDPrice, ticker.Price * po.Amount USDAmount 
FROM otcontract_holding_paidout po
JOIN ticker_trac ticker ON ticker.Timestamp = (
SELECT MAX(TIMESTAMP)
FROM ticker_trac
WHERE TIMESTAMP <= po.Timestamp)
WHERE po.Holder = @identity AND (@OfferId_like is null OR po.OfferID = @OfferId_like)";

        public const String GetDetailed = @"select I.Identity, substring(I.NodeId, 1, 40) as NodeId, Version, COALESCE(I.Stake, 0) as StakeTokens, COALESCE(I.StakeReserved, 0) as StakeReservedTokens, 
COALESCE(I.Paidout, 0) as PaidTokens, COALESCE(I.TotalOffers, 0) as TotalWonOffers, COALESCE(I.OffersLast7Days, 0) WonOffersLast7Days, I.Approved,
(select IT.OldIdentity from OTContract_Profile_IdentityTransferred IT WHERE IT.NewIdentity = @identity) as OldIdentity,
(select IT.NewIdentity from OTContract_Profile_IdentityTransferred IT WHERE IT.OldIdentity = @identity) as NewIdentity,
I.ManagementWallet,
COALESCE(ic.TransactionHash, pc.TransactionHash) CreateTransactionHash,
COALESCE(ic.GasPrice, pc.GasPrice) CreateGasPrice,
COALESCE(ic.GasUsed, pc.GasUsed) CreateGasUsed,
(SELECT COUNT(O.OfferID) FROM OTOffer O WHERE O.DCNodeId = I.NodeId) as DCOfferCount
from OTIdentity I
left JOIN otcontract_profile_identitycreated ic on ic.NewIdentity = I.Identity
left JOIN otcontract_profile_profilecreated pc on pc.Profile = I.Identity
WHERE I.Identity = @identity";

        public const String GetJobs = @"SELECT h.OfferId, 
                h.IsOriginalHolder,
                o.FinalizedTimestamp, 
                o.HoldingTimeInMinutes, 
                o.TokenAmountPerHolder,
                DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) as EndTimestamp,
                (CASE WHEN IsFinalized = 1 
                	THEN (CASE WHEN NOW() <= DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) THEN
                	(CASE 
                		WHEN h.LitigationStatus = '4' THEN 'Data Holder Replaced' 
                		WHEN h.LitigationStatus = '3' THEN 'Data Holder is Being Replaced' 
                		WHEN h.LitigationStatus = '2' THEN 'Active (Litigation Answered)' 
                		WHEN h.LitigationStatus = '1' THEN 'Active (Litigation Initiated)' 
                		WHEN h.LitigationStatus = '0' and lc.DHWasPenalized = 1 THEN 'Litigation Failed' 
                		WHEN h.LitigationStatus = '0' and (lc.TransactionHash is null OR lc.DHWasPenalized = 0) THEN 'Active (Litigation Passed)' 
                		ELSE 'Active' END)
                	 ELSE
                	(CASE 
                		WHEN h.LitigationStatus = '4' THEN 'Data Holder Replaced' 
                		WHEN h.LitigationStatus = '3' THEN 'Data Holder is Being Replaced' 
                		WHEN h.LitigationStatus = '2' THEN 'Completed (Litigation Answered)' 
                		WHEN h.LitigationStatus = '1' THEN 'Completed (Litigation Initiated)' 
                		WHEN h.LitigationStatus = '0' and lc.DHWasPenalized = 1 THEN 'Litigation Failed' 
                		WHEN h.LitigationStatus = '0' and (lc.TransactionHash is null OR lc.DHWasPenalized = 0) THEN 'Completed (Litigation Passed)' 
                		ELSE 'Completed' END)
                	  END)
                	ELSE ''
                END) as Status,
                (CASE WHEN COALESCE(SUM(po.Amount), 0) = O.TokenAmountPerHolder then true else false end) as Paidout,
                (CASE WHEN po.ID is null THEN 
                	(CASE WHEN (h.LitigationStatus is null OR h.LitigationStatus = 0 OR h.LitigationStatus = 1 OR h.LitigationStatus = 2)
                		 THEN true else false END) 
                ELSE false END) as CanPayout
                FROM otoffer_holders h
                join otoffer o on o.offerid = h.offerid
                left join otcontract_holding_paidout po on po.OfferID = h.OfferID and po.Holder = h.Holder
                left join otcontract_litigation_litigationcompleted lc on lc.OfferId = h.OfferId and lc.HolderIdentity = h.Holder and lc.BlockNumber = h.LitigationStatusBlockNumber and h.LitigationStatus = 0
                WHERE h.holder = @identity AND (@OfferId_like is null OR o.OfferId = @OfferId_like)
                GROUP BY h.OfferID, h.Holder";

        public const String GetJobsCount = @"SELECT COUNT(DISTINCT h.OfferId)
                FROM otoffer_holders h
                join otoffer o on o.offerid = h.offerid
                left join otcontract_holding_paidout po on po.OfferID = h.OfferID and po.Holder = h.Holder
                left join otcontract_litigation_litigationcompleted lc on lc.OfferId = h.OfferId and lc.HolderIdentity = h.Holder and lc.BlockNumber = h.LitigationStatusBlockNumber and h.LitigationStatus = 0
                WHERE h.holder = @identity AND (@OfferId_like is null OR o.OfferId = @OfferId_like)";

        public const String GetPayouts = @"SELECT OfferID, Amount, Timestamp, TransactionHash, GasUsed, GasPrice FROM otcontract_holding_paidout
WHERE holder = @identity AND (@OfferId_like is null OR OfferId = @OfferId_like) AND (@TransactionHash_like is null OR TransactionHash = @TransactionHash_like)";


        public const String GetPayoutsCount = @"SELECT COUNT(OfferID) FROM otcontract_holding_paidout
WHERE holder = @identity AND (@OfferId_like is null OR OfferId = @OfferId_like) AND (@TransactionHash_like is null OR TransactionHash = @TransactionHash_like)";

        public const String GetProfileTransfers = @"SELECT TransactionHash, AmountDeposited as Amount, b.Timestamp, t.GasPrice, t.GasUsed FROM otcontract_profile_tokensdeposited t
JOIN ethblock b on b.BlockNumber = t.BlockNumber
where t.Profile = @identity AND (@TransactionHash_like is null OR t.TransactionHash = @TransactionHash_like)
union
SELECT TransactionHash, AmountWithdrawn * - 1 as Amount, b.Timestamp, t.GasPrice, t.GasUsed FROM otcontract_profile_tokenswithdrawn t
JOIN ethblock b on b.BlockNumber = t.BlockNumber
where t.Profile = @identity AND (@TransactionHash_like is null OR t.TransactionHash = @TransactionHash_like)
union 
select pc.TransactionHash, pc.InitialBalance as Amount, b.Timestamp, pc.GasPrice, pc.GasUsed  from otcontract_profile_profilecreated pc
join ethblock b on b.BlockNumber = pc.BlockNumber
WHERE pc.Profile = @identity AND (@TransactionHash_like is null OR pc.TransactionHash = @TransactionHash_like)";

        public const String GetProfileTransfersCount = @"
SELECT SUM(FoundCount) 
FROM (
SELECT COUNT(TransactionHash) FoundCount FROM otcontract_profile_tokensdeposited t
JOIN ethblock b on b.BlockNumber = t.BlockNumber
where t.Profile = @identity AND (@TransactionHash_like is null OR t.TransactionHash = @TransactionHash_like)
union
SELECT COUNT(TransactionHash) FoundCount FROM otcontract_profile_tokenswithdrawn t
JOIN ethblock b on b.BlockNumber = t.BlockNumber
where t.Profile = @identity AND (@TransactionHash_like is null OR t.TransactionHash = @TransactionHash_like)
union 
select COUNT(TransactionHash) FoundCount  from otcontract_profile_profilecreated pc
join ethblock b on b.BlockNumber = pc.BlockNumber
WHERE pc.Profile = @identity AND (@TransactionHash_like is null OR pc.TransactionHash = @TransactionHash_like)
) x";

        public const String GetLitigations = @"SELECT li.TransactionHash, li.Timestamp, li.OfferId, li.requestedBlockIndex RequestedBlockIndex, li.requestedObjectIndex RequestedObjectIndex
FROM otcontract_litigation_litigationinitiated li
WHERE li.HolderIdentity = @identity AND (@OfferId_like is null OR li.OfferId = @OfferId_like)";

        public const String GetLitigationsCount = @"SELECT COUNT(li.TransactionHash)
FROM otcontract_litigation_litigationinitiated li
WHERE li.HolderIdentity = @identity AND (@OfferId_like is null OR li.OfferId = @OfferId_like)";
    }
}