using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapper;
using MySql.Data.MySqlClient;
using Nethereum.Hex.HexTypes;

namespace OTHelperNetStandard.Models.Database
{
    public class OTContract_Holding_OfferFinalized
    {
        public string OfferID { get; set; }
        public UInt64 BlockNumber { get; set; }
        public string TransactionHash { get; set; }
        public DateTime Timestamp { get; set; }
        public String Holder1 { get; set; }
        public String Holder2 { get; set; }
        public String Holder3 { get; set; }
        public string ContractAddress { get; set; }
        public UInt64 GasUsed { get; set; }
        public string Data { get; set; }
        public UInt64 GasPrice { get; set; }

        public static void InsertIfNotExist(MySqlConnection connection, OTContract_Holding_OfferFinalized model)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTContract_Holding_OfferFinalized WHERE OfferID = @offerID", new
            {
                offerID = model.OfferID
            });

            if (count == 0)
            {
                connection.Execute(
                    @"INSERT INTO OTContract_Holding_OfferFinalized (OfferID, Timestamp,
BlockNumber, TransactionHash, Holder1, Holder2, Holder3, ContractAddress, GasUsed, Processed, Data, GasPrice) VALUES(@OfferID, @Timestamp, @BlockNumber, @TransactionHash, @Holder1, @Holder2, @Holder3, @ContractAddress, @GasUsed, 0, @Data, @GasPrice)",
                    new
                    {
                        model.OfferID,
                        model.Timestamp,
                        model.BlockNumber,
                        model.TransactionHash,
                        model.Holder1,
                        model.Holder2,
                        model.Holder3,
                        model.ContractAddress,
                        model.GasUsed,
                        model.Data,
                        model.GasPrice
                    });

                foreach (var holder in new[] { model.Holder1, model.Holder2, model.Holder3 })
                {
                    if (connection.QuerySingle<Int32>(
                            "SELECT COUNT(*) FROM OtOffer_Holders WHERE OfferID = @OfferID AND Holder = @holder",
                            new { OfferID = model.OfferID, holder = holder }) == 0)
                    {
                        connection.Execute("INSERT INTO OtOffer_Holders(OfferID, Holder, IsOriginalHolder) VALUES (@OfferID, @holder, @IsOriginalHolder)", new { OfferID = model.OfferID, holder = holder, IsOriginalHolder = true });
                    }
                }

                //   DiscordHelper.OfferCreated(model);
            }
        }

        //public static void FinalizeOffer(MySqlConnection connection, string offerId, HexBigInteger logBlockNumber,
        //    string logTransactionHash, string holder1, string holder2, string holder3, DateTime blockTimestamp)
        //{
        //    connection.Execute("UPDATE OtOffer SET FinalizedBlockNumber = @logBlockNumber, FinalizedTransactionHash = @logTransactionHash, FinalizedTimestamp = @FinalizedTimestamp, IsFinalized = 1 WHERE OfferID = @offerId", new
        //    {
        //        offerId,
        //        logBlockNumber = (UInt64)logBlockNumber.Value,
        //        logTransactionHash,
        //        FinalizedTimestamp = blockTimestamp
        //    });

        //    bool added = false;

        //    foreach (var holder in new[] { holder1, holder2, holder3 })
        //    {
        //        if (connection.QuerySingle<Int32>(
        //                "SELECT COUNT(*) FROM OtOffer_Holders WHERE OfferID = @OfferID AND Holder = @holder",
        //                new { OfferID = offerId, holder = holder }) == 0)
        //        {
        //            added = true;
        //            connection.Execute("INSERT INTO OtOffer_Holders(OfferID, Holder) VALUES (@OfferID, @holder)", new { OfferID = offerId, holder = holder });
        //        }
        //    }

        //    if (added)
        //    {
        //        DiscordHelper.OfferFinalized(offerId, holder1, holder2, holder3, blockTimestamp);
        //    }
        //}

        //        public static OTOfferHolder[] GetHolders(MySqlConnection connection, string offerId)
        //        {
        //            return connection.Query<OTOfferHolder>(@"SELECT h.Holder as Identity, po.Amount FROM OTOffer_Holders h
        //left join otcontract_holding_paidout po on po.Holder = h.Holder and po.OfferId = h.OfferId WHERE h.OfferId = @offerId", new { offerId }).ToArray();
        //        }

        public static OTContract_Holding_OfferFinalized[] GetUnprocessed(MySqlConnection connection)
        {
            return connection.Query<OTContract_Holding_OfferFinalized>("SELECT * FROM OTContract_Holding_OfferFinalized WHERE Processed = 0").ToArray();
        }

        public static void SetProcessed(MySqlConnection connection, OTContract_Holding_OfferFinalized offerToAdd)
        {
            connection.Execute(@"UPDATE OTContract_Holding_OfferFinalized SET Processed = 1 WHERE OfferID = @offerID", new { offerID = offerToAdd.OfferID });
        }

        public static bool Exists(MySqlConnection connection, string offerId)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTContract_Holding_OfferFinalized WHERE OfferID = @offerID", new
            {
                offerID = offerId
            });

            return count > 0;
        }
    }
}