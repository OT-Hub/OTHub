using System;
using Dapper;
using MySql.Data.MySqlClient;

namespace OTHub.BackendSync.Database.Models
{
    public class OTContract_Litigation_LitigationInitiated
    {
        public string TransactionHash { get; set; }
        public UInt64 BlockNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public String OfferId { get; set; }
        public String HolderIdentity { get; set; }
        public ulong GasPrice { get; set; }
        public ulong GasUsed { get; set; }
        public ulong RequestedObjectIndex { get; set; }
        public ulong RequestedBlockIndex { get; set; }

        public static void InsertIfNotExist(MySqlConnection connection, OTContract_Litigation_LitigationInitiated model)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTContract_Litigation_LitigationInitiated WHERE TransactionHash = @hash", new
            {
                hash = model.TransactionHash
            });

            if (count == 0)
            {
                connection.Execute(
                    @"INSERT INTO OTContract_Litigation_LitigationInitiated
(TransactionHash, BlockNumber, Timestamp, OfferId, HolderIdentity, RequestedObjectIndex, GasPrice, GasUsed, RequestedBlockIndex)
VALUES(@TransactionHash, @BlockNumber, @Timestamp, @OfferId, @HolderIdentity, @RequestedObjectIndex, @GasPrice, @GasUsed, @RequestedBlockIndex)",
                    new
                    {
                        model.TransactionHash,
                        model.BlockNumber,
                        model.Timestamp,
                        model.OfferId,
                        model.HolderIdentity,
                        model.RequestedObjectIndex,
                        model.RequestedBlockIndex,
                        model.GasPrice,
                        model.GasUsed
                    });

                OTOfferHolder.UpdateLitigationStatusesForOffer(connection, model.OfferId);
            }
        }
    }
}