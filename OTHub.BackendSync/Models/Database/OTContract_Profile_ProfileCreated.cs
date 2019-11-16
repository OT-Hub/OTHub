using System;
using Dapper;
using MySql.Data.MySqlClient;

namespace OTHelperNetStandard.Models.Database
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

        public static void InsertIfNotExist(MySqlConnection connection, OTContract_Profile_ProfileCreated model)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTContract_Profile_ProfileCreated WHERE TransactionHash = @hash", new
            {
                hash = model.TransactionHash
            });

            if (count == 0)
            {
                connection.Execute(
                    @"INSERT INTO OTContract_Profile_ProfileCreated(TransactionHash, ContractAddress, Profile, InitialBalance, BlockNumber, ManagementWallet, NodeId,
GasPrice, GasUsed)
VALUES(@TransactionHash, @ContractAddress, @Profile, @InitialBalance, @BlockNumber, @ManagementWallet, @NodeId, @GasPrice, @GasUsed)",
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
                        model.GasUsed
                    });
            }
        }
    }
}