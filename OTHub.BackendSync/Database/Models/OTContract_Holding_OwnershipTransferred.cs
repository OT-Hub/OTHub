using System;
using Dapper;
using MySqlConnector;

namespace OTHub.BackendSync.Database.Models
{
    public class OTContract_Holding_OwnershipTransferred
    {
        public String TransactionHash { get; set; }
        public String PreviousOwner { get; set; }
        public String NewOwner { get; set; }
        public String ContractAddress { get; set; }
        public UInt64 BlockNumber { get; set; }
        public ulong GasPrice { get; set; }
        public ulong GasUsed { get; set; }
        public int BlockchainID { get; set; }

        public static void InsertIfNotExist(MySqlConnection connection, OTContract_Holding_OwnershipTransferred model)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTContract_Holding_OwnershipTransferred WHERE TransactionHash = @hash AND BlockchainID = @blockchainID", new
            {
                hash = model.TransactionHash,
                blockchainID = model.BlockchainID
            });

            if (count == 0)
            {
                connection.Execute(
                    @"INSERT INTO OTContract_Holding_OwnershipTransferred(TransactionHash, PreviousOwner, NewOwner, ContractAddress, BlockNumber,
GasPrice, GasUsed, BlockchainID) VALUES(@TransactionHash, @PreviousOwner, @NewOwner, @ContractAddress, @BlockNumber, @GasPrice, @GasUsed, @BlockchainID)",
                    new
                    {
                        model.TransactionHash,
                        model.PreviousOwner,
                        model.NewOwner,
                        model.ContractAddress,
                        model.BlockNumber,
                        model.GasPrice,
                        model.GasUsed,
                        model.BlockchainID
                    });
            }
        }
    }
}