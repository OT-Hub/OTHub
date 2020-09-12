using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTHub.APIServer.Sql
{
    public class DataCreatorSql
    {
        public const String GetDetailed =
            @"select I.Identity, substring(I.NodeId, 1, 40) as NodeId, Version, COALESCE(I.Stake, 0) as StakeTokens, COALESCE(I.StakeReserved, 0) as StakeReservedTokens, 
 I.Approved,
(select IT.OldIdentity from OTContract_Profile_IdentityTransferred IT WHERE IT.NewIdentity = @identity) as OldIdentity,
(select IT.NewIdentity from OTContract_Profile_IdentityTransferred IT WHERE IT.OldIdentity = @identity) as NewIdentity,
I.ManagementWallet,
COALESCE(ic.TransactionHash, pc.TransactionHash) CreateTransactionHash,
COALESCE(ic.GasPrice, pc.GasPrice) CreateGasPrice,
COALESCE(ic.GasUsed, pc.GasUsed) CreateGasUsed
from OTIdentity I
left JOIN otcontract_profile_identitycreated ic on ic.NewIdentity = I.Identity
left JOIN otcontract_profile_profilecreated pc on pc.Profile = I.Identity
JOIN otoffer O ON O.DCNodeId = I.NodeId
WHERE I.Identity = @identity
GROUP BY I.Identity";

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
                    WHERE i.Identity = @identity AND (@OfferId_like is null OR o.OfferId = @OfferId_like)";

        public const String GetJobsCount =
            @"SELECT COUNT(o.OfferId)
                    FROM OTOffer o
                    join otidentity i on i.NodeId = o.DCNodeId
                    WHERE i.Identity = @identity AND (@OfferId_like is null OR o.OfferId = @OfferId_like)";

        public const String GetLitigations =
            @"SELECT li.TransactionHash, li.Timestamp, li.OfferId, li.HolderIdentity, li.requestedBlockIndex RequestedBlockIndex, li.requestedObjectIndex RequestedObjectIndex
                    FROM otcontract_litigation_litigationinitiated li
                    JOIN OTOffer O ON O.OfferId = li.OfferId
                    JOIN OTIdentity I ON I.NodeId = O.DCNodeId
                    WHERE I.Identity = @identity AND (@OfferId_like is null OR o.OfferId = @OfferId_like) AND (@HolderIdentity_like is null OR li.HolderIdentity=@HolderIdentity_like)";

        public const String GetLitigationsCount =
            @"SELECT COUNT(li.TransactionHash)
                    FROM otcontract_litigation_litigationinitiated li
                    JOIN OTOffer O ON O.OfferId = li.OfferId
                    JOIN OTIdentity I ON I.NodeId = O.DCNodeId
                    WHERE I.Identity = @identity AND (@OfferId_like is null OR o.OfferId = @OfferId_like) AND (@HolderIdentity_like is null OR li.HolderIdentity=@HolderIdentity_like)";
    }
}