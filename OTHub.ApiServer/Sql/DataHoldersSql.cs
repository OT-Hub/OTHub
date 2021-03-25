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

        public static async Task<(NodeDataHolderSummaryModel[] results, int total)> Get(int ercVersion,
            string[] nodes,
            string[] managementWallet,
            int limit, int page,
            string NodeId_like,
            string sort,
            string order)
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

                var sql = $@"select COUNT(DISTINCT CASE WHEN O.IsFinalized = 1 
	THEN (CASE WHEN NOW() <= DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) THEN O.OfferId ELSE null END)
	ELSE null
END) as ActiveOffers,
substring(I.NodeId, 1, 40) as NodeId, I.Version, 
SUM(COALESCE(I.Stake, 0)) OVER(PARTITION BY I.NodeId)  as StakeTokens,
SUM(COALESCE(I.StakeReserved, 0)) OVER(PARTITION BY I.NodeId) as StakeReservedTokens, 
SUM(COALESCE(I.Paidout, 0)) OVER(PARTITION BY I.NodeId) as PaidTokens,
SUM(COALESCE(I.TotalOffers, 0)) OVER(PARTITION BY I.NodeId) as TotalWonOffers, 
SUM(COALESCE(I.OffersLast7Days, 0)) OVER(PARTITION BY I.NodeId)  WonOffersLast7Days
from OTIdentity I
JOIN blockchains bc ON bc.ID = I.BlockchainID
LEFT JOIN OTOffer_Holders OH ON OH.Holder = I.Identity
LEFT JOIN OTOffer O ON O.OfferID = OH.OfferID
WHERE (@NodeId_like IS NULL OR (I.NodeId = @NodeId_like OR I.Identity = @NodeId_like)) AND {(nodes.Any() ? "I.NodeId in @nodes AND" : "")} {(managementWallet.Any() ? "I.ManagementWallet in @managementWallet AND" : "")} I.Version = @version
GROUP BY I.NodeId
{orderBy}
{limitSql}";

                NodeDataHolderSummaryModel[] summary = connection.Query<NodeDataHolderSummaryModel>(
                    sql, new {version = ercVersion, nodes, managementWallet, NodeId_like}).ToArray();

                var total = connection.ExecuteScalar<int>($@"select COUNT(DISTINCT I.NodeId)
from OTIdentity I
LEFT JOIN OTOffer_Holders OH ON OH.Holder = I.Identity
LEFT JOIN OTOffer O ON O.OfferID = OH.OfferID
WHERE (@NodeId_like IS NULL OR I.NodeId = @NodeId_like) AND {(nodes.Any() ? "I.NodeId in @nodes AND" : "")} {(managementWallet.Any() ? "I.ManagementWallet in @managementWallet AND" : "")} I.Version = @version",
                    new {version = ercVersion, nodes, managementWallet, NodeId_like });

                return (summary, total);
            }
        }
    }
}