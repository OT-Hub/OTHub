using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using OTHub.APIServer.Sql.Models;
using OTHub.APIServer.Sql.Models.Nodes.DataHolders;
using OTHub.Settings;

namespace OTHub.APIServer.Sql
{
    public static class DataHoldersSql
    {
        public const String GetManagementWalletForIdentitySql = @"select managementwallet from otidentity
where identity = @identity";

        public const String GetRecentPayoutGasPricesSql = @"
SELECT GasPrice, AVG(GasUsed) GasUsed, COUNT(*) TotalCount FROM otcontract_holding_paidout
WHERE Timestamp >= DATE_Add(NOW(), INTERVAL -3 DAY)
GROUP BY GasPrice
ORDER BY GasPrice";

        public static async Task<(NodeDataHolderSummaryModel[] results, int total)> Get(
            string userID,
            int limit, int page,
            string NodeId_like,
            string sort,
            string order,
            bool filterByMyNodes)
        {
            string orderBy = String.Empty;

            switch (sort)
            {
                case "WonOffersLast7Days":
                    orderBy = "ORDER BY WonOffersLast7Days";
                    break;
                case "TotalWonOffers":
                    orderBy = "ORDER BY TotalWonOffers";
                    break;
                case "ActiveOffers":
                    orderBy = "ORDER BY ActiveOffers";
                    break;
                case "PaidTokens":
                    orderBy = "ORDER BY PaidTokens";
                    break;
                case "StakeReservedTokens":
                    orderBy = "ORDER BY StakeReservedTokens";
                    break;
                case "StakeTokens":
                    orderBy = "ORDER BY StakeTokens";
                    break;
            }

            if (!String.IsNullOrWhiteSpace(orderBy))
            {
                switch (order)
                {
                    case "ASC":
                        orderBy += " ASC";
                        break;
                    case "DESC":
                        orderBy += " DESC";
                        break;
                }
            }

            string limitSql = string.Empty;

            if (page >= 0 && limit >= 0)
            {
                limitSql = $"LIMIT {page * limit},{limit}";
            }

            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                //{ (includeHistory ? ",MAX(CASE WHEN H.Success = 1 THEN H.Timestamp ELSE NULL END) LastSeenOnline," : "")}
                //{ (includeHistory ? "MAX(CASE WHEN H.Success = 0 THEN H.Timestamp ELSE NULL END) LastSeenOffline" : "")}
                //{(includeHistory ? "LEFT JOIN (SELECT NodeID, Success, MAX(TIMESTAMP) Timestamp FROM otnode_history GROUP BY NodeID, Success) H ON H.NodeID = I.NodeID" : "")}

                var sql = $@"select 
substring(I.NodeId, 1, 40) as NodeId, 
{(userID != null ? "MN.DisplayName as DisplayName," : "")}
MAX(I.Version) Version, 
SUM(COALESCE(I.Stake, 0))  as StakeTokens,
SUM(COALESCE(I.StakeReserved, 0))  as StakeReservedTokens, 
SUM(COALESCE(I.Paidout, 0))  as PaidTokens,
SUM(COALESCE(I.TotalOffers, 0))  as TotalWonOffers, 
SUM(COALESCE(I.OffersLast7Days, 0))  WonOffersLast7Days,
(SELECT COUNT(DISTINCT  CASE WHEN O.IsFinalized = 1 
	THEN (CASE WHEN NOW() <= DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) THEN O.OfferId ELSE null END)
	ELSE null
END) FROM otoffer o 
JOIN otoffer_holders h ON h.OfferID = o.OfferID AND h.BlockchainID = o.BlockchainID
WHERE o.BlockchainID = I.blockchainID AND h.Holder = I.Identity) ActiveJobs
from OTIdentity I
{(userID != null ? $"{(filterByMyNodes ? "INNER" : "LEFT")} JOIN MyNodes MN ON MN.NodeID = I.NodeID AND MN.UserID = @userID" : "")}
WHERE (@NodeId_like IS NULL OR (I.NodeId = @NodeId_like OR I.Identity = @NodeId_like))
AND I.Version = 1
GROUP BY I.NodeId
{orderBy}
{limitSql}";

                NodeDataHolderSummaryModel[] summary = (await connection.QueryAsync<NodeDataHolderSummaryModel>(
                    sql, new { userID = userID, NodeId_like })).ToArray();

                var total = await connection.ExecuteScalarAsync<int>($@"select COUNT(DISTINCT I.NodeId)
from OTIdentity I
{(userID != null ? $"{(filterByMyNodes ? "INNER" : "LEFT")} JOIN MyNodes MN ON MN.NodeID = I.NodeID AND MN.UserID = @userID" : "")}
WHERE (@NodeId_like IS NULL OR I.NodeId = @NodeId_like) AND I.Version = 1",
                    new { userID = userID, NodeId_like });

                return (summary, total);
            }
        }
    }
}