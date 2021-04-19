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

        public const String GetDetailed = @"select substring(I.NodeId, 1, 40) as NodeId,
 Version, 
 SUM(COALESCE(I.Stake, 0)) as StakeTokens, 
SUM(COALESCE(I.StakeReserved, 0)) as StakeReservedTokens, 
SUM(COALESCE(I.Paidout, 0)) as PaidTokens, 
SUM(COALESCE(I.TotalOffers, 0)) as TotalWonOffers, 
SUM(COALESCE(I.OffersLast7Days, 0)) WonOffersLast7Days,
(SELECT COUNT(O.OfferID) FROM OTOffer O WHERE O.DCNodeId = I.NodeId) as DCOfferCount,
bc.BlockchainName,
bc.NetworkName,
mn.DisplayName
from OTIdentity I
JOIN blockchains bc ON bc.ID = I.BlockchainID
left JOIN otcontract_profile_identitycreated ic on ic.NewIdentity = I.Identity AND ic.BlockchainID = I.BlockchainID
left JOIN otcontract_profile_profilecreated pc on pc.Profile = I.Identity AND pc.BlockchainID = I.BlockchainID
left join mynodes mn on mn.UserID = @userID and mn.NodeID = I.NodeID
WHERE I.NodeID = @nodeId
GROUP BY I.NodeId";

        public const String GetJobs = @"SELECT h.Holder Identity, h.OfferId, 
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
                JOIN otidentity i ON i.Identity = h.Holder
                left join otcontract_holding_paidout po on po.OfferID = h.OfferID and po.Holder = h.Holder
                left join otcontract_litigation_litigationcompleted lc on lc.OfferId = h.OfferId and lc.HolderIdentity = h.Holder and lc.BlockNumber = h.LitigationStatusBlockNumber and h.LitigationStatus = 0
                WHERE i.nodeId = @nodeId AND (@OfferId_like is null OR o.OfferId = @OfferId_like)
                GROUP BY h.OfferID, h.Holder";

        public const String GetJobsCount = @"SELECT COUNT(DISTINCT h.OfferId)
                FROM otoffer_holders h
                join otoffer o on o.offerid = h.offerid
                JOIN otidentity i ON i.Identity = h.Holder
                left join otcontract_holding_paidout po on po.OfferID = h.OfferID and po.Holder = h.Holder
                left join otcontract_litigation_litigationcompleted lc on lc.OfferId = h.OfferId and lc.HolderIdentity = h.Holder and lc.BlockNumber = h.LitigationStatusBlockNumber and h.LitigationStatus = 0
                WHERE i.NodeId = @nodeId AND (@OfferId_like is null OR o.OfferId = @OfferId_like)";

        public const String GetPayouts = @"SELECT po.OfferID, po.Amount, po.Timestamp, po.TransactionHash, po.GasUsed, po.GasPrice
FROM otcontract_holding_paidout po
JOIN otidentity i ON i.Identity = po.Holder
WHERE i.NodeId = @nodeId AND (@OfferId_like is null OR po.OfferId = @OfferId_like) AND (@TransactionHash_like is null OR po.TransactionHash = @TransactionHash_like)";


        public const String GetPayoutsCount = @"SELECT COUNT(po.OfferID) FROM otcontract_holding_paidout po
JOIN otidentity i ON i.Identity = po.Holder
WHERE i.NodeId = @nodeId AND (@OfferId_like is null OR po.OfferId = @OfferId_like) AND (@TransactionHash_like is null OR po.TransactionHash = @TransactionHash_like)";

        public const String GetProfileTransfers = @"SELECT t.TransactionHash, t.AmountDeposited as Amount, b.Timestamp, t.GasPrice, t.GasUsed, bb.GasTicker, bb.TransactionUrl FROM otcontract_profile_tokensdeposited t
JOIN ethblock b on b.BlockNumber = t.BlockNumber AND b.BlockchainID = t.BlockchainID
JOIN otidentity i ON i.Identity = t.Profile
JOIN blockchains bb ON bb.id = b.BlockchainID
where i.NodeId = @nodeId AND (@TransactionHash_like is null OR t.TransactionHash = @TransactionHash_like)
UNION
SELECT t.TransactionHash, t.AmountWithdrawn * - 1 as Amount, b.Timestamp, t.GasPrice, t.GasUsed, bb.GasTicker, bb.TransactionUrl  FROM otcontract_profile_tokenswithdrawn t
JOIN ethblock b on b.BlockNumber = t.BlockNumber AND b.BlockchainID = t.BlockchainID
JOIN otidentity i ON i.Identity = t.Profile
JOIN blockchains bb ON bb.id = b.BlockchainID
where i.NodeId = @nodeId AND (@TransactionHash_like is null OR t.TransactionHash = @TransactionHash_like)
union
select pc.TransactionHash, pc.InitialBalance as Amount, b.Timestamp, pc.GasPrice, pc.GasUsed, bb.GasTicker, bb.TransactionUrl   from otcontract_profile_profilecreated pc
join ethblock b on b.BlockNumber = pc.BlockNumber AND b.BlockchainID = pc.BlockchainID
JOIN otidentity i ON i.Identity = pc.Profile
JOIN blockchains bb ON bb.id = b.BlockchainID
WHERE i.NodeId = @nodeId AND (@TransactionHash_like is null OR pc.TransactionHash = @TransactionHash_like)";

        public const String GetProfileTransfersCount = @"
SELECT SUM(FoundCount) 
FROM (
SELECT COUNT(t.TransactionHash) FoundCount FROM otcontract_profile_tokensdeposited t
JOIN ethblock b on b.BlockNumber = t.BlockNumber AND b.BlockchainID = t.BlockchainID
JOIN otidentity i ON i.Identity = t.Profile
where i.NodeId = @nodeId AND (@TransactionHash_like is null OR t.TransactionHash = @TransactionHash_like)
union
SELECT COUNT(t.TransactionHash) FoundCount FROM otcontract_profile_tokenswithdrawn t
JOIN ethblock b on b.BlockNumber = t.BlockNumber AND b.BlockchainID = t.BlockchainID
JOIN otidentity i ON i.Identity = t.Profile
where i.NodeId = @nodeId AND (@TransactionHash_like is null OR t.TransactionHash = @TransactionHash_like)
union 
select COUNT(pc.TransactionHash) FoundCount  from otcontract_profile_profilecreated pc
join ethblock b on b.BlockNumber = pc.BlockNumber AND b.BlockchainID = pc.BlockchainID
JOIN otidentity i ON i.Identity = pc.Profile
WHERE i.NodeId = @nodeId AND (@TransactionHash_like is null OR pc.TransactionHash = @TransactionHash_like)
) x";

        public const String GetLitigations = @"SELECT li.TransactionHash, li.Timestamp, li.OfferId, li.requestedBlockIndex RequestedBlockIndex, li.requestedObjectIndex RequestedObjectIndex
FROM otcontract_litigation_litigationinitiated li
JOIN otidentity i ON i.Identity = li.HolderIdentity
WHERE i.NodeId = @nodeId AND (@OfferId_like is null OR li.OfferId = @OfferId_like)";

        public const String GetLitigationsCount = @"SELECT COUNT(li.TransactionHash)
FROM otcontract_litigation_litigationinitiated li
JOIN otidentity i ON i.Identity = li.HolderIdentity
WHERE i.NodeId = @nodeId AND (@OfferId_like is null OR li.OfferId = @OfferId_like)";
    }
}