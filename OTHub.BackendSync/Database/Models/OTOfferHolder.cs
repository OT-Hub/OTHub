using System;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using Newtonsoft.Json;

namespace OTHub.BackendSync.Database.Models
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

        public static async Task UpdateLitigationForAllOffers(MySqlConnection connection, int blockchainID)
        {
            await connection.ExecuteAsync(@"UPDATE OTOffer_Holders H
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
JOIN OTOffer_Holders H ON H.OfferId = O.OfferId AND H.BlockchainID = O.BlockchainID
LEFT JOIN otcontract_litigation_litigationinitiated li on li.OfferId = O.OfferId and li.HolderIdentity = H.Holder AND li.BlockchainID = O.BlockchainID
LEFT JOIN otcontract_litigation_litigationanswered la on la.OfferId = O.OfferId and la.HolderIdentity = H.Holder AND la.BlockchainID = O.BlockchainID
LEFT JOIN otcontract_litigation_litigationcompleted lc on lc.OfferId = O.OfferId and lc.HolderIdentity = H.Holder AND lc.BlockchainID = O.BlockchainID
LEFT JOIN otcontract_litigation_replacementstarted rs on rs.OfferId = O.OfferId and rs.HolderIdentity = H.Holder AND rs.BlockchainID = O.BlockchainID
WHERE O.IsFinalized = 1 AND O.BlockchainID = @blockchainID
GROUP BY H.OfferId, H.Holder, H.LitigationStatus) x
WHERE CASE 
 WHEN x.ReplaceStarted = x.MaxNumber THEN 3
 WHEN x.LitFailed = x.MaxNumber THEN 0
 WHEN x.LitPassed = x.MaxNumber THEN 0
 WHEN x.LitAnswered = x.MaxNumber THEN 2
 WHEN x.LitInit = x.MaxNumber THEN 1
 ELSE NULL
 END != COALESCE(LitigationStatus, -1)) p on p.OfferId = H.OfferId AND p.Holder = H.Holder
 SET LitigationStatus = p.Status, LitigationStatusBlockNumber = p.MaxNumber", new
            {
                blockchainID = blockchainID
            }, commandTimeout: (int)TimeSpan.FromMinutes(5).TotalSeconds);
        }

        public static async Task UpdateLitigationStatusesForOffer(MySqlConnection connection, string offerId, int blockchainID)
        {
            await connection.ExecuteAsync(@"UPDATE OTOffer_Holders H
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
JOIN OTOffer_Holders H ON H.OfferId = O.OfferId AND H.BlockchainID = O.BlockchainID
LEFT JOIN otcontract_litigation_litigationinitiated li on li.OfferId = O.OfferId and li.HolderIdentity = H.Holder AND li.BlockchainID = O.BlockchainID
LEFT JOIN otcontract_litigation_litigationanswered la on la.OfferId = O.OfferId and la.HolderIdentity = H.Holder AND la.BlockchainID = O.BlockchainID
LEFT JOIN otcontract_litigation_litigationcompleted lc on lc.OfferId = O.OfferId and lc.HolderIdentity = H.Holder AND lc.BlockchainID = O.BlockchainID
LEFT JOIN otcontract_litigation_replacementstarted rs on rs.OfferId = O.OfferId and rs.HolderIdentity = H.Holder AND rs.BlockchainID = O.BlockchainID
WHERE O.IsFinalized = 1 AND O.OfferId = @offerID AND O.BlockchainID = @blockchainID
GROUP BY H.OfferId, H.Holder, H.LitigationStatus) x
WHERE CASE 
 WHEN x.ReplaceStarted = x.MaxNumber THEN 3
 WHEN x.LitFailed = x.MaxNumber THEN 0
 WHEN x.LitPassed = x.MaxNumber THEN 0
 WHEN x.LitAnswered = x.MaxNumber THEN 2
 WHEN x.LitInit = x.MaxNumber THEN 1
 ELSE NULL
 END != COALESCE(LitigationStatus, -1)) p on p.OfferId = H.OfferId AND p.Holder = H.Holder
 SET LitigationStatus = p.Status, LitigationStatusBlockNumber = p.MaxNumber",
                new
                {
                    OfferId = offerId,
                    blockchainID = blockchainID
                });
        }

        public static async Task<bool> Insert(MySqlConnection connection, string offerId, string holder, bool isOriginalHolder, int blockchainID)
        {
            bool added = false;

            if (await connection.QuerySingleAsync<Int32>(
                    "SELECT COUNT(*) FROM OtOffer_Holders WHERE OfferID = @OfferID AND Holder = @holder AND BlockchainID = @blockchainID",
                    new { OfferID = offerId, holder = holder, blockchainID = blockchainID }) == 0)
            {
                added = true;
                await connection.ExecuteAsync(
                    "INSERT INTO OtOffer_Holders(OfferID, Holder, IsOriginalHolder, BlockchainID) VALUES (@OfferID, @holder, @IsOriginalHolder, @BlockchainID)",
                    new {OfferID = offerId, holder = holder, IsOriginalHolder = isOriginalHolder, BlockchainID  = blockchainID});
            }

            return added;
        }
    }
}