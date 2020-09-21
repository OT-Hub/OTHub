using System;
using System.Linq;
using Dapper;
using MySql.Data.MySqlClient;
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

        public static NodeDataHolderSummaryModel[] Get(int ercVersion,
            string[] identity,
            string[] managementWallet,
            int limit, int page,
            string Identity_like,
            string sort,
            string order, out int total)
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

            //History is expensive which can be improved on in the future. I think I fixed the performance but better to be safe
            bool includeHistory = false;

            if (identity.Any())
            {
                includeHistory = true;
            }

            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var sql = $@"select I.Identity,
COUNT(DISTINCT CASE WHEN O.IsFinalized = 1 
	THEN (CASE WHEN NOW() <= DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) THEN O.OfferId ELSE null END)
	ELSE 0
END) as ActiveOffers,
 substring(I.NodeId, 1, 40) as NodeId, I.Version, COALESCE(I.Stake, 0) as StakeTokens,
COALESCE(I.StakeReserved, 0) as StakeReservedTokens, COALESCE(I.Paidout, 0) as PaidTokens, COALESCE(I.TotalOffers, 0) as TotalWonOffers, 
COALESCE(I.OffersLast7Days, 0) WonOffersLast7Days, I.Approved
{(includeHistory ? ",MAX(CASE WHEN H.Success = 1 THEN H.Timestamp ELSE NULL END) LastSeenOnline," : "")}
{(includeHistory ? "MAX(CASE WHEN H.Success = 0 THEN H.Timestamp ELSE NULL END) LastSeenOffline" : "")}
from OTIdentity I
LEFT JOIN OTOffer_Holders OH ON OH.Holder = I.Identity
LEFT JOIN OTOffer O ON O.OfferID = OH.OfferID
{(includeHistory ? "LEFT JOIN (SELECT NodeID, Success, MAX(TIMESTAMP) Timestamp FROM otnode_history GROUP BY NodeID, Success) H ON H.NodeID = I.NodeID" : "")}
WHERE (@Identity_like IS NULL OR I.Identity = @Identity_like) AND {(identity.Any() ? "I.Identity in @identity AND" : "")} {(managementWallet.Any() ? "I.ManagementWallet in @managementWallet AND" : "")} I.Version = @version
GROUP BY I.Identity
{orderBy}
{limitSql}";

                NodeDataHolderSummaryModel[] summary = connection.Query<NodeDataHolderSummaryModel>(
                    sql, new {version = ercVersion, identity, managementWallet, Identity_like}).ToArray();

                total = connection.ExecuteScalar<int>($@"select COUNT(DISTINCT I.Identity)
from OTIdentity I
LEFT JOIN OTOffer_Holders OH ON OH.Holder = I.Identity
LEFT JOIN OTOffer O ON O.OfferID = OH.OfferID
WHERE (@Identity_like IS NULL OR I.Identity = @Identity_like) AND {(identity.Any() ? "I.Identity in @identity AND" : "")} {(managementWallet.Any() ? "I.ManagementWallet in @managementWallet AND" : "")} I.Version = @version",
                    new {version = ercVersion, identity, managementWallet, Identity_like});

                return summary;
            }
        }
    }
}