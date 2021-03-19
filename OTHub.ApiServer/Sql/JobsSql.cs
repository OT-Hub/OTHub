using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using OTHub.APIServer.Sql.Models;
using OTHub.APIServer.Sql.Models.Jobs;
using OTHub.Settings;

namespace OTHub.APIServer.Sql
{
    public static class JobsSql
    {
        public static async Task<(OfferSummaryModel[] results, int total)> GetWithPaging(int limit, int page, string OfferId_like,
            string sort,
           string order)
        {
            string orderBy = String.Empty;

            switch (sort)
            {
                case "CreatedTimestamp":
                    orderBy = "ORDER BY CreatedTimestamp";
                    break;
                case "FinalizedTimestamp":
                    orderBy = "ORDER BY FinalizedTimestamp";
                    break;
                case "DataSetSizeInBytes":
                    orderBy = "ORDER BY DataSetSizeInBytes";
                    break;
                case "HoldingTimeInMinutes":
                    orderBy = "ORDER BY HoldingTimeInMinutes";
                    break;
                case "TokenAmountPerHolder":
                    orderBy = "ORDER BY TokenAmountPerHolder";
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
                OfferSummaryModel[] rows = (await connection.QueryAsync<OfferSummaryModel>(
                    $@"SELECT I.Identity DCIdentity, O.OfferId, O.CreatedTimestamp as CreatedTimestamp, o.FinalizedTimestamp as FinalizedTimestamp, O.DataSetSizeInBytes, O.TokenAmountPerHolder, O.HoldingTimeInMinutes, O.IsFinalized,
(CASE WHEN O.IsFinalized = 1 
	THEN (CASE WHEN NOW() <= DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) THEN 'Active' ELSE 'Completed' END)
	ELSE (CASE WHEN O.CreatedTimeStamp <= DATE_Add(NOW(), INTERVAL -30 MINUTE)
		THEN 'Not Started'
		ELSE 'Not Started'
	END)
END) as Status,
(CASE WHEN O.IsFinalized = 1  THEN DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) ELSE NULL END) as EndTimestamp,
bc.DisplayName BlockchainDisplayName
FROM OTOffer O
JOIN blockchains bc ON bc.ID = O.BlockchainID
LEFT JOIN OTIdentity I ON I.NodeID = O.DCNodeID
WHERE O.IsFinalized = 1 AND COALESCE(@OfferId_like, '') = '' OR O.OfferId = @OfferId_like
GROUP BY O.OfferID
{orderBy}
{limitSql}", new
                    {
                        OfferId_like
                    })).ToArray();

                var total = await connection.ExecuteScalarAsync<int>(@"SELECT COUNT(DISTINCT O.OfferID)
FROM OTOffer O
JOIN blockchains bc ON bc.ID = O.BlockchainID
LEFT JOIN OTIdentity I ON I.NodeID = O.DCNodeID
WHERE O.IsFinalized = 1 AND COALESCE(@OfferId_like, '') = '' OR O.OfferId = @OfferId_like", new
                {
                    OfferId_like
                });


                return (rows, total);
            }
        }
    }
}