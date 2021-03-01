using System;
using Dapper;
using MySqlConnector;

namespace OTHub.BackendSync.Database.Models
{
    public class OTContract_Profile_ProfileCreated
    {
        public String TransactionHash { get; set; }
        public String ContractAddress { get; set; }
        public String Profile { get; set; }
        public UInt64 BlockNumber { get; set; }
        public decimal InitialBalance { get; set; }

        public String ManagementWallet { get; set; }
        public String NodeId { get; set; }
        public ulong GasUsed { get; set; }
        public ulong GasPrice { get; set; }
        public int BlockchainID { get; set; }

        public static void InsertIfNotExist(MySqlConnection connection, OTContract_Profile_ProfileCreated model)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTContract_Profile_ProfileCreated WHERE TransactionHash = @hash AND BlockchainID = @BlockchainID", new
            {
                hash = model.TransactionHash,
                model.BlockchainID
            });

            if (count == 0)
            {
                connection.Execute(
                    @"INSERT INTO OTContract_Profile_ProfileCreated(TransactionHash, ContractAddress, Profile, InitialBalance, BlockNumber, ManagementWallet, NodeId,
GasPrice, GasUsed, BlockchainID)
VALUES(@TransactionHash, @ContractAddress, @Profile, @InitialBalance, @BlockNumber, @ManagementWallet, @NodeId, @GasPrice, @GasUsed, @BlockchainID)",
                    new
                    {
                        model.TransactionHash,
                        model.ContractAddress,
                        model.Profile,
                        model.InitialBalance,
                        model.BlockNumber,
                        model.ManagementWallet,
                        model.NodeId,
                        model.GasPrice,
                        model.GasUsed,
                        model.BlockchainID
                    });
            }
        }
    }
}