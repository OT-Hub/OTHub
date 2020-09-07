using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTHub.APIServer.Sql
{
    public class DataCreatorsSql
    {
        public static String GetDataCreatorsSql(string[] identity)
        {
            return $@"select I.Identity, substring(I.NodeId, 1, 40) as NodeId, Version, 
COALESCE(I.Stake, 0) as StakeTokens, COALESCE(I.StakeReserved, 0) as StakeReservedTokens,
I.Approved,
Count(O.OfferId) OffersTotal,
SUM(CASE WHEN O.CreatedTimestamp >= Date_Add(NOW(), INTERVAL -7 DAY) THEN 1 ELSE 0 END) OffersLast7Days,
ROUND(AVG(O.DataSetSizeInBytes) / 1000) AvgDataSetSizeKB,
ROUND(AVG(O.HoldingTimeInMinutes)) AvgHoldingTimeInMinutes,
ROUND(AVG(O.TokenAmountPerHolder)) AvgTokenAmountPerHolder,
x.Timestamp as CreatedTimestamp,
COALESCE(MAX(O.FinalizedTimestamp), MAX(CreatedTimestamp)) LastJob
from OTIdentity I
JOIN OTOffer O ON O.DCNodeId = I.NodeId
JOIN (SELECT I.Identity, COALESCE(PCB.Timestamp, ICB.Timestamp) as Timestamp FROM OTIdentity I
LEFT JOIN OTContract_Profile_ProfileCreated PC ON PC.Profile = I.Identity
LEFT JOIN EthBlock PCB ON PCB.BlockNumber = PC.BlockNumber
LEFT JOIN OTContract_Profile_IdentityCreated IC ON IC.NewIdentity = I.Identity
LEFT JOIN EthBlock ICB ON ICB.BlockNumber = IC.BlockNumber
WHERE IC.NewIdentity is not null OR PC.Profile is not null) x on x.Identity = I.Identity
WHERE {(identity.Any() ? "I.Identity in @identity AND" : "")} Version = @version
AND (@Identity_like is null OR I.Identity = @Identity_like)
GROUP BY I.Identity";
        }

        public static String GetDataCreatorsCountSql(string[] identity)
        {
            return $@"select COUNT(DISTINCT I.Identity)
from OTIdentity I
JOIN OTOffer O ON O.DCNodeId = I.NodeId
JOIN (SELECT I.Identity, COALESCE(PCB.Timestamp, ICB.Timestamp) as Timestamp FROM OTIdentity I
LEFT JOIN OTContract_Profile_ProfileCreated PC ON PC.Profile = I.Identity
LEFT JOIN EthBlock PCB ON PCB.BlockNumber = PC.BlockNumber
LEFT JOIN OTContract_Profile_IdentityCreated IC ON IC.NewIdentity = I.Identity
LEFT JOIN EthBlock ICB ON ICB.BlockNumber = IC.BlockNumber
WHERE IC.NewIdentity is not null OR PC.Profile is not null) x on x.Identity = I.Identity
WHERE {(identity.Any() ? "I.Identity in @identity AND" : "")} Version = @version
AND (@Identity_like is null OR I.Identity = @Identity_like)";
        }
    }
}