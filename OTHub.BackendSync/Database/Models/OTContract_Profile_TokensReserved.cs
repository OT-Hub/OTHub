using System;
using Dapper;
using MySqlConnector;

namespace OTHub.BackendSync.Database.Models
{
    public class OTContract_Profile_TokensReserved
    {
        public String TransactionHash { get; set; }
        public String ContractAddress { get; set; }
        public UInt64 BlockNumber { get; set; }
        public String Profile { get; set; }
        public decimal AmountReserved { get; set; }
        public ulong GasUsed { get; set; }
        public ulong GasPrice { get; set; }
        public int BlockchainID { get; set; }

        public static bool TransactionExists(MySqlConnection connection, string transactionHash, int blockchainID)
        {
            var count = connection.QueryFirstOrDefault<Int32>(
                "SELECT COUNT(*) FROM OTContract_Profile_TokensReserved WHERE TransactionHash = @transactionHash AND BlockchainID = @blockchainID", new
                {
                    transactionHash,
                    blockchainID = blockchainID
                });

            if (count == 0)
                return false;

            return true;
        }

        public static void Insert(MySqlConnection connection, OTContract_Profile_TokensReserved model)
        {
            connection.Execute(
                @"INSERT INTO OTContract_Profile_TokensReserved(TransactionHash, ContractAddress, Profile, AmountReserved, BlockNumber, GasPrice, GasUsed, BlockchainID)
VALUES(@TransactionHash, @ContractAddress, @Profile, @AmountReserved, @BlockNumber, @GasPrice, @GasUsed, @BlockchainID)",
                new
                {
                    model.TransactionHash,
                    model.ContractAddress,
                    model.Profile,
                    model.AmountReserved,
                    model.BlockNumber,
                    model.GasPrice,
                    model.GasUsed,
                    model.BlockchainID
                });
        }
    }
}