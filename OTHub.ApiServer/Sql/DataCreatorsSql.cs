﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTHub.APIServer.Sql
{
    public class DataCreatorsSql
    {
        public static string GetDataCreatorsSql(string userID, bool filterByMyNodes)
        {
            var data = $@"select substring(I.NodeId, 1, 40) as NodeId,
 MAX(VERSION) Version, 
{(userID != null ? "x.DisplayName as DisplayName," : "")}
SUM(COALESCE(I.Stake, 0)) as StakeTokens, 
SUM(COALESCE(I.StakeReserved, 0)) as StakeReservedTokens,
x.Timestamp as CreatedTimestamp,
p.OffersTotal,
p.OffersLast7Days,
p.AvgDataSetSizeKB,
p.AvgHoldingTimeInMinutes,
p.AvgTokenAmountPerHolder,
p.LastJob
from OTIdentity I
JOIN (SELECT 
O.DCnodeID,
Count(O.OfferId) OffersTotal,
SUM(CASE WHEN O.CreatedTimestamp >= Date_Add(NOW(), INTERVAL -7 DAY) THEN 1 ELSE 0 END) OffersLast7Days,
ROUND(AVG(O.DataSetSizeInBytes) / 1000) AvgDataSetSizeKB,
ROUND(AVG(O.HoldingTimeInMinutes)) AvgHoldingTimeInMinutes,
ROUND(AVG(O.TokenAmountPerHolder)) AvgTokenAmountPerHolder,
COALESCE(MAX(O.FinalizedTimestamp), MAX(CreatedTimestamp)) LastJob
 FROM otoffer O GROUP BY O.DCNodeId) p ON p.DCnodeID = I.NodeID
LEFT JOIN (SELECT I.Identity, COALESCE(PCB.Timestamp, ICB.Timestamp) as Timestamp {(userID != null ? ", MN.DisplayName" : "")} FROM OTIdentity I
{(userID != null ? $"{(filterByMyNodes ? "INNER" : "LEFT")} JOIN MyNodes MN ON MN.NodeID = I.NodeID AND MN.UserID = @userID" : "")}
LEFT JOIN OTContract_Profile_ProfileCreated PC ON PC.Profile = I.Identity AND PC.BlockchainID = I.BlockchainID
LEFT JOIN EthBlock PCB ON PCB.BlockNumber = PC.BlockNumber AND PCB.BlockchainID = I.BlockchainID
LEFT JOIN OTContract_Profile_IdentityCreated IC ON IC.NewIdentity = I.Identity AND IC.BlockchainID = I.BlockchainID
LEFT JOIN EthBlock ICB ON ICB.BlockNumber = IC.BlockNumber AND ICB.BlockchainID = I.BlockchainID
WHERE IC.NewIdentity is not null OR PC.Profile is not null) x on x.Identity = I.Identity
LEFT JOIN otnode_dc_visibility dc ON dc.NodeId = I.NodeId
WHERE VERSION = 1 AND dc.NodeId IS null
AND (@NodeId_like is null OR I.NodeId = @NodeId_like)
GROUP BY I.NodeId";

            return data;
        }

        public static string GetDataCreatorsCountSql(string userID, bool filterByMyNodes)
        {
            return $@"select COUNT(DISTINCT I.NodeId)
from OTIdentity I
{(userID != null ? $"{(filterByMyNodes ? "INNER" : "LEFT")} JOIN MyNodes MN ON MN.NodeID = I.NodeID AND MN.UserID = @userID" : "")}
JOIN (SELECT I.Identity, COALESCE(PCB.Timestamp, ICB.Timestamp) as Timestamp FROM OTIdentity I
LEFT JOIN OTContract_Profile_ProfileCreated PC ON PC.Profile = I.Identity AND PC.BlockchainID = I.BlockchainID
LEFT JOIN EthBlock PCB ON PCB.BlockNumber = PC.BlockNumber AND PCB.BlockchainID = I.BlockchainID
LEFT JOIN OTContract_Profile_IdentityCreated IC ON IC.NewIdentity = I.Identity AND IC.BlockchainID = I.BlockchainID
LEFT JOIN EthBlock ICB ON ICB.BlockNumber = IC.BlockNumber AND ICB.BlockchainID = I.BlockchainID
WHERE IC.NewIdentity is not null OR PC.Profile is not null) x on x.Identity = I.Identity
JOIN otoffer o ON o.DCNodeId = I.NodeId
LEFT JOIN otnode_dc_visibility dc ON dc.NodeId = I.NodeId
WHERE VERSION = 1 AND dc.NodeId IS null
AND (@NodeId_like is null OR I.NodeId = @NodeId_like)";
        }
    }
}