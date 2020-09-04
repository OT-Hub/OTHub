using System;

namespace OTHub.APIServer.Sql
{
    public static class RecentActivitySql
    {
        public const String GetRecentActivitySql =
            @"SELECT OH.Holder Identity, O.OfferId, O.CreatedTimestamp as Timestamp, O.TokenAmountPerHolder, 
(CASE WHEN O.IsFinalized = 1  THEN DATE_Add(O.FinalizedTimeStamp, INTERVAL + O.HoldingTimeInMinutes MINUTE) ELSE NULL END) as EndTimestamp
FROM OTOffer_Holders OH
JOIN OTOffer O ON O.OfferID = OH.OfferID
JOIN OTIdentity I ON I.NodeID = O.DCNodeID
WHERE OH.Holder in @identity AND OH.IsOriginalHolder = 1 AND O.CreatedTimestamp >= DATE_Add(NOW(), INTERVAL -7 DAY)
ORDER BY O.CreatedTimestamp DESC";
    }
}
