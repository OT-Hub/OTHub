using System;
using System.Linq;
using Dapper;
using MySql.Data.MySqlClient;
using OTHub.APIServer.Sql.Models;
using OTHub.APIServer.Sql.Models.Jobs;
using OTHub.Settings;

namespace OTHub.APIServer.Sql
{
    public static class JobsSql
    {
        public static OfferSummaryModel[] GetWithPaging(int limit, int page, string OfferId_like,
            string sort,
           string order, out int total)
        {
            string orderBy = String.Empty;

            switch (sort)
            {
                case "Timestamp":
                    orderBy = "ORDER BY Timestamp";
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
                limitSql = $"LIMIT {page},{limit}";
            }

            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                OfferSummaryModel[] rows = connection.Query<OfferSummaryModel>(
                    $@"SELECT I.Identity DCIdentity, O.OfferId, O.CreatedTimestamp as Timestamp, O.DataSetSizeInBytes, O.TokenAmountPerHolder, O.HoldingTimeInMinutes, O.IsFinalized,
(CASE WHEN O.IsFinalized = 1 
	THEN (CASE WHEN NOW() <= DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) THEN 'Active' ELSE 'Completed' END)
	ELSE (CASE WHEN O.CreatedTimeStamp <= DATE_Add(NOW(), INTERVAL -30 MINUTE)
		THEN 'Not Started'
		ELSE 'Not Started'
	END)
END) as Status,
(CASE WHEN O.IsFinalized = 1  THEN DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) ELSE NULL END) as EndTimestamp
FROM OTOffer O
LEFT JOIN OTIdentity I ON I.NodeID = O.DCNodeID
WHERE COALESCE(@OfferId_like, '') = '' OR O.OfferId = @OfferId_like
GROUP BY O.OfferID
{orderBy}
{limitSql}", new
                    {
                        OfferId_like
                    }).ToArray();

                total = connection.ExecuteScalar<int>(@"SELECT COUNT(DISTINCT O.OfferID)
FROM OTOffer O
LEFT JOIN OTIdentity I ON I.NodeID = O.DCNodeID
WHERE COALESCE(@OfferId_like, '') = '' OR O.OfferId = @OfferId_like", new
                {
                    OfferId_like
                });


                return rows;
            }
        }
    }
}