using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTHub.APIServer.Sql
{
    public class DataCreatorSql
    {
        public const String GetDetailed =
            @"SELECT 
substring(I.NodeId, 1, 40) as NodeId,
 Version, 
 SUM(COALESCE(I.Stake, 0)) as StakeTokens, 
SUM(COALESCE(I.StakeReserved, 0)) as StakeReservedTokens
from OTIdentity I
JOIN blockchains bc ON bc.ID = I.BlockchainID
WHERE I.NodeId = @nodeId
GROUP BY I.NodeId";

        public const String GetJobs =
            @"SELECT o.OfferId, o.CreatedTimestamp as CreatedTimestamp, o.FinalizedTimestamp as FinalizedTimestamp, o.DataSetSizeInBytes, o.TokenAmountPerHolder, o.HoldingTimeInMinutes, o.IsFinalized,
                    (CASE WHEN o.IsFinalized = 1 
                    	THEN (CASE WHEN NOW() <= DATE_Add(o.FinalizedTimeStamp, INTERVAL +o.HoldingTimeInMinutes MINUTE) THEN 'Active' ELSE 'Completed' END)
                    	ELSE (CASE WHEN o.CreatedTimeStamp <= DATE_Add(NOW(), INTERVAL -30 MINUTE)
                    		THEN 'Not Started'
                    		ELSE 'Not Started'
                    	END)
                    END) as Status,
                    (CASE WHEN o.IsFinalized = 1  THEN DATE_Add(o.FinalizedTimeStamp, INTERVAL +o.HoldingTimeInMinutes MINUTE) ELSE NULL END) as EndTimestamp
                    FROM OTOffer o
                    join otidentity i on i.NodeId = o.DCNodeId
                    join otcontract_holding_offercreated oc on oc.OfferID = o.OfferID
                    left join otcontract_holding_offerfinalized of on of.OfferID = o.OfferID
                    WHERE i.NodeId = @nodeId AND (@OfferId_like is null OR o.OfferId = @OfferId_like)
    GROUP BY o.OfferID, i.NodeId";

        public const String GetJobsCount =
            @"SELECT COUNT(distinct o.OfferId)
                    FROM OTOffer o
                    join otidentity i on i.NodeId = o.DCNodeId
                    WHERE i.NodeId = @nodeId AND (@OfferId_like is null OR o.OfferId = @OfferId_like)";

        public const String GetLitigations =
            @"SELECT li.TransactionHash, li.Timestamp, li.OfferId, II.NodeId, li.requestedBlockIndex RequestedBlockIndex, li.requestedObjectIndex RequestedObjectIndex
                    FROM otcontract_litigation_litigationinitiated li
                    JOIN OTOffer O ON O.OfferId = li.OfferId
                    JOIN OTIdentity I ON I.NodeId = O.DCNodeId
                    JOIN OTIdentity II ON II.Identity = li.HolderIdentity
                    WHERE I.NodeId = @nodeId AND (@OfferId_like is null OR o.OfferId = @OfferId_like) AND (@HolderIdentity_like is null OR li.HolderIdentity=@HolderIdentity_like)
                    GROUP BY li.TransactionHash";

        public const String GetLitigationsCount =
            @"SELECT COUNT(li.TransactionHash)
                    FROM otcontract_litigation_litigationinitiated li
                    JOIN OTOffer O ON O.OfferId = li.OfferId
                    JOIN OTIdentity I ON I.NodeId = O.DCNodeId
                    JOIN OTIdentity II ON II.Identity = li.HolderIdentity
                    WHERE I.NodeId = @nodeId AND (@OfferId_like is null OR o.OfferId = @OfferId_like) AND (@HolderIdentity_like is null OR li.HolderIdentity=@HolderIdentity_like)
                    GROUP BY li.TransactionHash";
    }
}