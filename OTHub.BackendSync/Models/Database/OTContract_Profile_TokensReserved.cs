using System;
using Dapper;
using MySql.Data.MySqlClient;

namespace OTHelperNetStandard.Models.Database
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

        public static bool TransactionExists(MySqlConnection connection, string transactionHash)
        {
            var count = connection.QueryFirstOrDefault<Int32>(
                "SELECT COUNT(*) FROM OTContract_Profile_TokensReserved WHERE TransactionHash = @transactionHash", new
                {
                    transactionHash
                });

            if (count == 0)
                return false;

            return true;
        }

        public static void Insert(MySqlConnection connection, OTContract_Profile_TokensReserved model)
        {
            connection.Execute(
                @"INSERT INTO OTContract_Profile_TokensReserved(TransactionHash, ContractAddress, Profile, AmountReserved, BlockNumber, GasPrice, GasUsed)
VALUES(@TransactionHash, @ContractAddress, @Profile, @AmountReserved, @BlockNumber, @GasPrice, @GasUsed)",
                new
                {
                    model.TransactionHash,
                    model.ContractAddress,
                    model.Profile,
                    model.AmountReserved,
                    model.BlockNumber,
                    model.GasPrice,
                    model.GasUsed
                });
        }
    }
}