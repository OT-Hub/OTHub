using System;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;

namespace OTHub.BackendSync.Database.Models
{
    public class OTContract_Litigation_LitigationTimedOut
    {
        public string TransactionHash { get; set; }
        public UInt64 BlockNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public String OfferId { get; set; }
        public String HolderIdentity { get; set; }
        public ulong GasPrice { get; set; }
        public ulong GasUsed { get; set; }
        public int BlockchainID { get; set; }
        public static async Task InsertIfNotExist(MySqlConnection connection, OTContract_Litigation_LitigationTimedOut model)
        {
            var count = await connection.QueryFirstOrDefaultAsync<Int32>("SELECT COUNT(*) FROM OTContract_Litigation_LitigationTimedOut WHERE TransactionHash = @hash AND BlockchainID = @blockchainID", new
            {
                hash = model.TransactionHash,
                blockchainID = model.BlockchainID
            });

            if (count == 0)
            {
                await connection.ExecuteAsync(
                    @"INSERT INTO OTContract_Litigation_LitigationTimedOut
(TransactionHash, BlockNumber, Timestamp, OfferId, HolderIdentity, GasPrice, GasUsed, BlockchainID)
VALUES(@TransactionHash, @BlockNumber, @Timestamp, @OfferId, @HolderIdentity, @GasPrice, @GasUsed, @BlockchainID)",
                    new
                    {
                        model.TransactionHash,
                        model.BlockNumber,
                        model.Timestamp,
                        model.OfferId,
                        model.HolderIdentity,
                        model.GasPrice,
                        model.GasUsed,
                        model.BlockchainID
                    });

                await OTOfferHolder.UpdateLitigationStatusesForOffer(connection, model.OfferId, model.BlockchainID);
            }
        }

        public static bool TransactionExists(MySqlConnection connection, string transactionHash, int blockchainID)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTContract_Litigation_LitigationTimedOut WHERE TransactionHash = @hash AND BlockchainID = @blockchainID", new
            {
                hash = transactionHash,
                blockchainID = blockchainID
            });

            return count > 0;
        }
    }
}