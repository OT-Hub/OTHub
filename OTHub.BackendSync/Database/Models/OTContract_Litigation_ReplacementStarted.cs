using System;
using Dapper;
using MySqlConnector;

namespace OTHub.BackendSync.Database.Models
{
    public class OTContract_Litigation_ReplacementStarted
    {
        public string TransactionHash { get; set; }
        public UInt64 BlockNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public String OfferId { get; set; }
        public String HolderIdentity { get; set; }
        public String ChallengerIdentity { get; set; }
        public String LitigationRootHash { get; set; }
        public ulong GasPrice { get; set; }
        public ulong GasUsed { get; set; }

        public static void InsertIfNotExist(MySqlConnection connection, OTContract_Litigation_ReplacementStarted model)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTContract_Litigation_ReplacementStarted WHERE TransactionHash = @hash", new
            {
                hash = model.TransactionHash
            });

            if (count == 0)
            {
                connection.Execute(
                    @"INSERT INTO OTContract_Litigation_ReplacementStarted
(TransactionHash, BlockNumber, Timestamp, ChallengerIdentity, OfferId, HolderIdentity, LitigationRootHash, GasPrice, GasUsed)
VALUES(@TransactionHash, @BlockNumber, @Timestamp, @ChallengerIdentity, @OfferId, @HolderIdentity, @LitigationRootHash, @GasPrice, @GasUsed)",
                    new
                    {
                        model.TransactionHash,
                        model.BlockNumber,
                        model.Timestamp,
                        model.ChallengerIdentity,
                        model.OfferId,
                        model.HolderIdentity,
                        model.LitigationRootHash,
                        model.GasPrice,
                        model.GasUsed
                    });

                OTOfferHolder.UpdateLitigationStatusesForOffer(connection, model.OfferId);
            }
        }
    }
}