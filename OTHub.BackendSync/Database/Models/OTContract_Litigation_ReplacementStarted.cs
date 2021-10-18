using System;
using System.Threading.Tasks;
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
        public int BlockchainID { get; set; }

        public static async Task InsertIfNotExist(MySqlConnection connection, OTContract_Litigation_ReplacementStarted model)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTContract_Litigation_ReplacementStarted WHERE TransactionHash = @hash AND BlockchainID = @blockchainID", new
            {
                hash = model.TransactionHash,
                blockchainID = model.BlockchainID
            });

            if (count == 0)
            {
                await connection.ExecuteAsync(
                    @"INSERT INTO OTContract_Litigation_ReplacementStarted
(TransactionHash, BlockNumber, Timestamp, ChallengerIdentity, OfferId, HolderIdentity, LitigationRootHash, GasPrice, GasUsed, BlockchainID)
VALUES(@TransactionHash, @BlockNumber, @Timestamp, @ChallengerIdentity, @OfferId, @HolderIdentity, @LitigationRootHash, @GasPrice, @GasUsed, @BlockchainID)",
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
                        model.GasUsed,
                        model.BlockchainID
                    });

                await OTOfferHolder.UpdateLitigationStatusesForOffer(connection, model.OfferId, model.BlockchainID);
            }
        }

        public static bool TransactionExists(MySqlConnection connection, string transactionHash, int blockchainID)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTContract_Litigation_ReplacementStarted WHERE TransactionHash = @hash AND BlockchainID = @blockchainID", new
            {
                hash = transactionHash,
                blockchainID = blockchainID
            });

            return count > 0;
        }
    }
}