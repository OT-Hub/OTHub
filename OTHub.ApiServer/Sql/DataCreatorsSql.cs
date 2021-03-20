using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTHub.APIServer.Sql
{
    public class DataCreatorsSql
    {
        public static String GetDataCreatorsSql(string[] nodes)
        {
            return $@"select substring(I.NodeId, 1, 40) as NodeId, Version, 
SUM(COALESCE(I.Stake, 0)) OVER(PARTITION BY I.NodeId) as StakeTokens, 
SUM(COALESCE(I.StakeReserved, 0)) OVER(PARTITION BY I.NodeId) as StakeReservedTokens,
Count(O.OfferId) OffersTotal,
SUM(CASE WHEN O.CreatedTimestamp >= Date_Add(NOW(), INTERVAL -7 DAY) THEN 1 ELSE 0 END) OffersLast7Days,
ROUND(AVG(O.DataSetSizeInBytes) / 1000) AvgDataSetSizeKB,
ROUND(AVG(O.HoldingTimeInMinutes)) AvgHoldingTimeInMinutes,
ROUND(AVG(O.TokenAmountPerHolder)) AvgTokenAmountPerHolder,
x.Timestamp as CreatedTimestamp,
COALESCE(MAX(O.FinalizedTimestamp), MAX(CreatedTimestamp)) LastJob
from OTIdentity I
JOIN blockchains bc ON bc.ID = I.BlockchainID
JOIN OTOffer O ON O.DCNodeId = I.NodeId
JOIN (SELECT I.Identity, COALESCE(PCB.Timestamp, ICB.Timestamp) as Timestamp FROM OTIdentity I
LEFT JOIN OTContract_Profile_ProfileCreated PC ON PC.Profile = I.Identity AND PC.BlockchainID = I.BlockchainID
LEFT JOIN EthBlock PCB ON PCB.BlockNumber = PC.BlockNumber AND PCB.BlockchainID = I.BlockchainID
LEFT JOIN OTContract_Profile_IdentityCreated IC ON IC.NewIdentity = I.Identity AND IC.BlockchainID = I.BlockchainID
LEFT JOIN EthBlock ICB ON ICB.BlockNumber = IC.BlockNumber AND ICB.BlockchainID = I.BlockchainID
WHERE IC.NewIdentity is not null OR PC.Profile is not null) x on x.Identity = I.Identity
WHERE {(nodes.Any() ? "I.NodeId in @nodes AND" : "")} Version = @version
AND (@NodeId_like is null OR I.NodeId = @NodeId_like)
GROUP BY I.NodeId";
        }

        public static String GetDataCreatorsCountSql(string[] nodes)
        {
            return $@"select COUNT(DISTINCT I.NodeId)
from OTIdentity I
JOIN OTOffer O ON O.DCNodeId = I.NodeId
JOIN (SELECT I.Identity, COALESCE(PCB.Timestamp, ICB.Timestamp) as Timestamp FROM OTIdentity I
LEFT JOIN OTContract_Profile_ProfileCreated PC ON PC.Profile = I.Identity AND PC.BlockchainID = I.BlockchainID
LEFT JOIN EthBlock PCB ON PCB.BlockNumber = PC.BlockNumber AND PCB.BlockchainID = I.BlockchainID
LEFT JOIN OTContract_Profile_IdentityCreated IC ON IC.NewIdentity = I.Identity AND IC.BlockchainID = I.BlockchainID
LEFT JOIN EthBlock ICB ON ICB.BlockNumber = IC.BlockNumber AND ICB.BlockchainID = I.BlockchainID
WHERE IC.NewIdentity is not null OR PC.Profile is not null) x on x.Identity = I.Identity
WHERE {(nodes.Any() ? "I.NodeId in @nodes AND" : "")} Version = @version
AND (@NodeId_like is null OR I.NodeId = @NodeId_like)";
        }
    }
}