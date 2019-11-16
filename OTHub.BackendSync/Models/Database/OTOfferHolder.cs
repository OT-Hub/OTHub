﻿using System;
using Dapper;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace OTHelperNetStandard.Models.Database
{
    public class OTOfferHolder
    {
        public String Identity { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public Boolean HasPaidout
        {
            get { return Amount != null; }
        }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public Decimal? Amount { get; set; }

        //public enum LitigationStatus
        //{
        //    initiated,
        //    answered,
        //    completed,
        //    replacing,
        //    replaced
        //}

        public enum LitigationStatus
        {
            completed,
            initiated,
            answered,
            replacing,
            replaced
        }

//        public static void UpdateLitigationStatus(MySqlConnection connection, string offerId, string holderIdentity, ulong blockNumber, LitigationStatus status)
//        {
//            connection.Execute(@"UPDATE otoffer_holders
//SET LitigationStatus = @Status, LitigationStatusBlockNumber = @BlockNumber
//WHERE OfferId = @OfferId AND Holder = @Holder AND (LitigationStatus IS NULL OR LitigationStatus != @Status) AND (LitigationStatusBlockNumber IS NULL OR LitigationStatusBlockNumber < @BlockNumber)",
//                new
//                {
//                    OfferId = offerId,
//                    Holder = holderIdentity,
//                    Status = (int)status,
//                    BlockNumber = blockNumber
//                });
//        }

        public static void UpdateLitigationForAllOffers(MySqlConnection connection)
        {
            connection.Execute(@"UPDATE OTOffer_Holders H
JOIN (
SELECT x.OfferId, x.Holder, 
CASE 
 WHEN x.ReplaceStarted = x.MaxNumber THEN 3
 WHEN x.LitFailed = x.MaxNumber THEN 0
 WHEN x.LitPassed = x.MaxNumber THEN 0
 WHEN x.LitAnswered = x.MaxNumber THEN 2
 WHEN x.LitInit = x.MaxNumber THEN 1
 ELSE NULL
 END as Status,
 x.LitigationStatus as OldStatus,
 x.MaxNumber
FROM (
SELECT  H.OfferId, H.Holder, H.LitigationStatus, MAX(li.BlockNumber) as LitInit,
 MAX(la.BlockNumber) LitAnswered,
    MAX( CASE WHEN lc.DHWasPenalized = 1 THEN null ELSE lc.BlockNumber END) as LitPassed,
      MAX( CASE WHEN lc.DHWasPenalized = 1 THEN lc.BlockNumber ELSE null END) as LitFailed,
  MAX(rs.BlockNumber) ReplaceStarted,
  GREATEST(IFNULL(MAX(li.BlockNumber), 0), IFNULL(MAX(la.BlockNumber), 0), IFNULL(MAX(lc.BlockNumber), 0), IFNULL(MAX(rs.BlockNumber), 0)) as MaxNumber
   FROM OTOffer O
JOIN OTOffer_Holders H ON H.OfferId = O.OfferId
LEFT JOIN otcontract_litigation_litigationinitiated li on li.OfferId = O.OfferId and li.HolderIdentity = H.Holder
LEFT JOIN otcontract_litigation_litigationanswered la on la.OfferId = O.OfferId and la.HolderIdentity = H.Holder
LEFT JOIN otcontract_litigation_litigationcompleted lc on lc.OfferId = O.OfferId and lc.HolderIdentity = H.Holder
LEFT JOIN otcontract_litigation_replacementstarted rs on rs.OfferId = O.OfferId and rs.HolderIdentity = H.Holder
WHERE O.IsFinalized = 1
GROUP BY H.OfferId, H.Holder, H.LitigationStatus) x
WHERE CASE 
 WHEN x.ReplaceStarted = x.MaxNumber THEN 3
 WHEN x.LitFailed = x.MaxNumber THEN 0
 WHEN x.LitPassed = x.MaxNumber THEN 0
 WHEN x.LitAnswered = x.MaxNumber THEN 2
 WHEN x.LitInit = x.MaxNumber THEN 1
 ELSE NULL
 END != LitigationStatus) p on p.OfferId = H.OfferId AND p.Holder = H.Holder
 SET LitigationStatus = p.Status, LitigationStatusBlockNumber = p.MaxNumber");
        }

        public static void UpdateLitigationStatusesForOffer(MySqlConnection connection, string offerId)
        {
            connection.Execute(@"UPDATE OTOffer_Holders H
JOIN (
SELECT x.OfferId, x.Holder, 
CASE 
 WHEN x.ReplaceStarted = x.MaxNumber THEN 3
 WHEN x.LitFailed = x.MaxNumber THEN 0
 WHEN x.LitPassed = x.MaxNumber THEN 0
 WHEN x.LitAnswered = x.MaxNumber THEN 2
 WHEN x.LitInit = x.MaxNumber THEN 1
 ELSE NULL
 END as Status,
 x.LitigationStatus as OldStatus,
 x.MaxNumber
FROM (
SELECT  H.OfferId, H.Holder, H.LitigationStatus, MAX(li.BlockNumber) as LitInit,
 MAX(la.BlockNumber) LitAnswered,
    MAX( CASE WHEN lc.DHWasPenalized = 1 THEN null ELSE lc.BlockNumber END) as LitPassed,
      MAX( CASE WHEN lc.DHWasPenalized = 1 THEN lc.BlockNumber ELSE null END) as LitFailed,
  MAX(rs.BlockNumber) ReplaceStarted,
  GREATEST(IFNULL(MAX(li.BlockNumber), 0), IFNULL(MAX(la.BlockNumber), 0), IFNULL(MAX(lc.BlockNumber), 0), IFNULL(MAX(rs.BlockNumber), 0)) as MaxNumber
   FROM OTOffer O
JOIN OTOffer_Holders H ON H.OfferId = O.OfferId
LEFT JOIN otcontract_litigation_litigationinitiated li on li.OfferId = O.OfferId and li.HolderIdentity = H.Holder
LEFT JOIN otcontract_litigation_litigationanswered la on la.OfferId = O.OfferId and la.HolderIdentity = H.Holder
LEFT JOIN otcontract_litigation_litigationcompleted lc on lc.OfferId = O.OfferId and lc.HolderIdentity = H.Holder
LEFT JOIN otcontract_litigation_replacementstarted rs on rs.OfferId = O.OfferId and rs.HolderIdentity = H.Holder
WHERE O.IsFinalized = 1 AND O.OfferId = @offerID
GROUP BY H.OfferId, H.Holder, H.LitigationStatus) x
WHERE CASE 
 WHEN x.ReplaceStarted = x.MaxNumber THEN 3
 WHEN x.LitFailed = x.MaxNumber THEN 0
 WHEN x.LitPassed = x.MaxNumber THEN 0
 WHEN x.LitAnswered = x.MaxNumber THEN 2
 WHEN x.LitInit = x.MaxNumber THEN 1
 ELSE NULL
 END != LitigationStatus) p on p.OfferId = H.OfferId AND p.Holder = H.Holder
 SET LitigationStatus = p.Status, LitigationStatusBlockNumber = p.MaxNumber",
                new
                {
                    OfferId = offerId
                });
        }

        public static bool Insert(MySqlConnection connection, string offerId, string holder, bool isOriginalHolder)
        {
            bool added = false;

            if (connection.QuerySingle<Int32>(
                    "SELECT COUNT(*) FROM OtOffer_Holders WHERE OfferID = @OfferID AND Holder = @holder",
                    new { OfferID = offerId, holder = holder }) == 0)
            {
                added = true;
                connection.Execute("INSERT INTO OtOffer_Holders(OfferID, Holder, IsOriginalHolder) VALUES (@OfferID, @holder, @IsOriginalHolder)", new { OfferID = offerId, holder = holder, IsOriginalHolder = isOriginalHolder });
            }

            return added;
        }
    }
}