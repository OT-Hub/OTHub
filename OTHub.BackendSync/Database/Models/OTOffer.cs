using System;
using System.Linq;
using Dapper;
using MySqlConnector;

namespace OTHub.BackendSync.Database.Models
{
    public class OTOffer
    {
        public string OfferID { get; set; }
        public UInt64 CreatedBlockNumber { get; set; }
        public UInt64? FinalizedBlockNumber { get; set; }

        public string DCNodeId { get; set; }

        public string DataSetId { get; set; }

        public UInt64 HoldingTimeInMinutes { get; set; }

        public decimal TokenAmountPerHolder { get; set; }
        public UInt64 DataSetSizeInBytes { get; set; }
        public UInt64 LitigationIntervalInMinutes { get; set; }

        public bool IsFinalized { get; set; }

        public string CreatedTransactionHash { get; set; }
        public string FinalizedTransactionHash { get; set; }
        public UInt64 TransactionIndex { get; set; }
        public DateTime CreatedTimestamp { get; set; }
        public DateTime? FinalizedTimestamp { get; set; }

        public int BlockchainID { get; set; }

        public static void InsertIfNotExist(MySqlConnection connection, OTOffer model)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTOffer WHERE OfferID = @offerID AND BlockchainID = @blockchainID", new
            {
                offerID = model.OfferID,
                blockchainID = model.BlockchainID
            });

            if (count == 0)
            {
                connection.Execute(
                    @"INSERT INTO OTOffer VALUES(@OfferID, @DCNodeId, @DataSetId, @TransactionIndex, @CreatedTimestamp, @CreatedBlockNumber, @CreatedTransactionHash,
@DataSetSizeInBytes, @TokenAmountPerHolder, @HoldingTimeInMinutes, @LitigationIntervalInMinutes, @IsFinalized, @FinalizedTransactionHash, @FinalizedBlockNumber, @FinalizedTimestamp, NULL, @BlockchainID)",
                    new
                    {
                        model.OfferID,
                        model.DCNodeId,
                        model.DataSetId,
                        model.TransactionIndex,
                        model.CreatedTimestamp,
                        model.CreatedBlockNumber,
                        model.CreatedTransactionHash,
                        model.DataSetSizeInBytes,
                        model.TokenAmountPerHolder,
                        model.HoldingTimeInMinutes,
                        model.LitigationIntervalInMinutes,
                        model.IsFinalized,
                        model.FinalizedTransactionHash,
                        model.FinalizedBlockNumber,
                        model.FinalizedTimestamp,
                        model.BlockchainID
                    });
            }
        }

        public static void FinalizeOffer(MySqlConnection connection, string offerId, UInt64 logBlockNumber,
            string logTransactionHash, string holder1, string holder2, string holder3, DateTime blockTimestamp, int blockchainID)
        {
            var count = connection.Execute(@"UPDATE OtOffer SET FinalizedBlockNumber = @logBlockNumber, FinalizedTransactionHash = @logTransactionHash, 
FinalizedTimestamp = @FinalizedTimestamp, IsFinalized = 1 WHERE OfferID = @offerId And IsFinalized = 0 AND BlockchainID = @blockchainID", new
            {
                offerId,
                logBlockNumber = logBlockNumber,
                logTransactionHash,
                FinalizedTimestamp = blockTimestamp,
                blockchainID = blockchainID
            });

            if (count == 0)
                return;

            bool added = false;

            foreach (var holder in new[] {holder1, holder2, holder3})
            {
                added = OTOfferHolder.Insert(connection, offerId, holder, true, blockchainID);
            }
            
        }

//        public static OTOfferHolder[] GetHolders(MySqlConnection connection, string offerId)
//        {
//            return connection.Query<OTOfferHolder>(@"SELECT h.Holder as Identity, po.Amount FROM OTOffer_Holders h
//left join otcontract_holding_paidout po on po.Holder = h.Holder and po.OfferId = h.OfferId WHERE h.OfferId = @offerId", new {offerId}).ToArray();
//        }

//        public static OTOffer[] GetAll(MySqlConnection connection)
//        {
//            return connection.Query<OTOffer>("SELECT * FROM OTOffer").ToArray();
//        }
    }
}