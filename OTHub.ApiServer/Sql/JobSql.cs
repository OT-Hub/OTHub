using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OTHub.Settings;

namespace OTHub.APIServer.Sql
{
    public static class JobSql
    {
        public const String GetJobDetailed =
			@"SELECT O.OfferId, O.EstimatedLambda, O.CreatedTimestamp as CreatedTimestamp, O.FinalizedTimestamp, O.LitigationIntervalInMinutes, O.DataSetId, O.DataSetSizeInBytes, O.TokenAmountPerHolder, O.HoldingTimeInMinutes, O.IsFinalized,
(CASE WHEN IsFinalized = 1 
	THEN (CASE WHEN NOW() <= DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) THEN 'Active' ELSE 'Completed' END)
	ELSE (CASE WHEN O.CreatedTimeStamp <= DATE_Add(NOW(), INTERVAL -30 MINUTE)
		THEN 'Not Started'
		ELSE 'Not Started'
	END)
END) as Status,
(CASE WHEN O.IsFinalized = 1  THEN DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) ELSE NULL END) as EndTimestamp
, O.CreatedBlockNumber, O.CreatedTransactionHash, O.DcNodeId, DCI.Identity DCIdentity,
(SELECT COUNT(*) FROM OTOffer IO WHERE IO.DCNodeID = O.DCNodeID AND IO.IsFinalized = 1) as OffersTotal,
(SELECT COUNT(*) FROM OTOffer IO WHERE IO.DCNodeID = O.DCNodeID AND IO.IsFinalized = 1 AND IO.CreatedTimeStamp >= DATE_Add(NOW(), INTERVAL -7 DAY)) as OffersLast7Days,
(SELECT COALESCE(SUM(Amount), 0) FROM OTContract_Holding_Paidout IP
JOIN OTOffer IO ON IO.OfferID = IP.OfferID
WHERE IO.DCNodeId = O.DCNodeId) as PaidoutTokensTotal,
O.FinalizedBlockNumber,
O.FinalizedTransactionHash,
OC.GasUsed CreatedGasUsed,
OF.GasUsed FinalizedGasUsed,
OC.GasPrice CreatedGasPrice,
OF.GasPrice FinalizedGasPrice,
bc.BlockchainName,
bc.NetworkName
 FROM OTOffer O
JOIN blockchains bc ON bc.ID = O.BlockchainID
 JOIN OTContract_Holding_OfferCreated OC ON OC.OfferID = O.OfferID
 LEFT JOIN OTContract_Holding_OfferFinalized OF ON OF.OfferID = O.OfferID
 LEFT JOIN OTIdentity DCI ON DCI.NodeId = O.DCNodeId
WHERE O.OfferId = @offerID";

        public const String GetJobHolders =
			@"SELECT H.Holder as Identity, CASE WHEN H.LitigationStatus = 0 AND (lc.TransactionHash is null OR lc.DHWasPenalized = 0) THEN NULL ELSE H.LitigationStatus END LitigationStatus,
(CASE WHEN IsFinalized = 1 
	THEN (CASE WHEN NOW() <= DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) THEN
	(CASE 
		WHEN h.LitigationStatus = '4' THEN 'Data Holder Replaced' 
		WHEN h.LitigationStatus = '3' THEN 'Data Holder is Being Replaced' 
		WHEN h.LitigationStatus = '2' THEN 'Active (Litigation Answered)' 
		WHEN h.LitigationStatus = '1' THEN 'Active (Litigation Initiated)' 
		WHEN h.LitigationStatus = '0' and lc.DHWasPenalized = 1 THEN 'Active (Litigation Failed)' 
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
END) as LitigationStatusText
 FROM OTOffer_Holders H
 JOIN OTOffer O ON O.OfferID = H.OfferID
left join otcontract_litigation_litigationcompleted lc on lc.OfferId = h.OfferId and lc.HolderIdentity = h.Holder and lc.BlockchainID = h.BlockchainID and lc.BlockNumber = h.LitigationStatusBlockNumber and h.LitigationStatus = 0
Where H.OfferId = @offerID
ORDER BY H.LitigationStatus";

        public static String GetJobTimeline()
        {
            return
                $@"select Timestamp, 'Offer Created' as Name, null as 'RelatedTo', TransactionHash  from otcontract_holding_offercreated
where OfferId = @offerID
union all
select Timestamp, 'Offer Finalized', null, TransactionHash from otcontract_holding_offerfinalized
where OfferId = @offerID
union all
select Timestamp, 'Data Holder Chosen' as Name, Holder as 'RelatedTo', TransactionHash  from otoffer_holders h
join otcontract_holding_offerfinalized of on of.OfferID = h.OfferId
where h.OfferId = @offerID AND h.IsOriginalHolder = 1
union all
select Timestamp, 'Litigation Initiated', HolderIdentity, TransactionHash from otcontract_litigation_litigationinitiated
where OfferId = @offerID
union all
select Timestamp, 'Litigation Timed out', HolderIdentity, TransactionHash from otcontract_litigation_litigationtimedout
where OfferId = @offerID
union all
select Timestamp, 'Litigation Answered', HolderIdentity, TransactionHash from otcontract_litigation_litigationanswered
where OfferId = @offerID
union all
select Timestamp, CASE WHEN DHWasPenalized = 1 THEN 'Litigation Failed' ELSE 'Litigation Passed' END, HolderIdentity, TransactionHash from otcontract_litigation_litigationcompleted
where OfferId = @offerID
union all
select Timestamp, 'Data Holder Replaced', HolderIdentity, TransactionHash from otcontract_litigation_replacementstarted
where OfferId = @offerID
union all
select Timestamp, 'Data Holder Chosen', ChosenHolder, TransactionHash from otcontract_replacement_replacementcompleted
where OfferId = @offerID
union all
select Timestamp, CONCAT('Offer Paidout for ', (CAST(TRUNCATE(`Amount`, 3) AS CHAR)+0), ' {(OTHubSettings.Instance.Blockchain.Network == BlockchainNetwork.Rinkeby ? "ATRAC" : "TRAC")}'), Holder, TransactionHash from otcontract_holding_paidout
where OfferId = @offerID
union all
select DATE_Add(of.Timestamp, INTERVAL + oc.HoldingTimeInMinutes MINUTE), 'Offer Completed', null, null from otcontract_holding_offerfinalized of
join otcontract_holding_offercreated oc on oc.OfferId = of.OfferId
where of.OfferId = @offerID 
and NOW() >= DATE_Add(of.Timestamp, INTERVAL + oc.HoldingTimeInMinutes MINUTE)";
        }
    }
}