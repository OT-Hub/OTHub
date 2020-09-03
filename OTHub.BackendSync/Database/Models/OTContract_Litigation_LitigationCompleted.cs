using System;
using Dapper;
using MySql.Data.MySqlClient;

namespace OTHub.BackendSync.Models.Database
{
    public class OTContract_Litigation_LitigationCompleted
    {
        public string TransactionHash { get; set; }
        public UInt64 BlockNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public String OfferId { get; set; }
        public String HolderIdentity { get; set; }
        public Boolean DHWasPenalized { get; set; }
        public ulong GasPrice { get; set; }
        public ulong GasUsed { get; set; }

        public static void InsertIfNotExist(MySqlConnection connection, OTContract_Litigation_LitigationCompleted model)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTContract_Litigation_LitigationCompleted WHERE TransactionHash = @hash", new
            {
                hash = model.TransactionHash
            });

            if (count == 0)
            {
                connection.Execute(
                    @"INSERT INTO OTContract_Litigation_LitigationCompleted
(TransactionHash, BlockNumber, Timestamp, OfferId, HolderIdentity, DHWasPenalized, GasPrice, GasUsed)
VALUES(@TransactionHash, @BlockNumber, @Timestamp, @OfferId, @HolderIdentity, @DHWasPenalized, @GasPrice, @GasUsed)",
                    new
                    {
                        model.TransactionHash,
                        model.BlockNumber,
                        model.Timestamp,
                        model.OfferId,
                        model.HolderIdentity,
                        model.DHWasPenalized,
                        model.GasPrice,
                        model.GasUsed
                    });

                OTOfferHolder.UpdateLitigationStatusesForOffer(connection, model.OfferId);
            }
        }
    }
}