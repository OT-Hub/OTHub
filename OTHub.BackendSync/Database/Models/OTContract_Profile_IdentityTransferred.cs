using System;
using Dapper;
using MySqlConnector;

namespace OTHub.BackendSync.Database.Models
{
    public class OTContract_Profile_IdentityTransferred
    {
        public String TransactionHash { get; set; }
        public String NodeId { get; set; }
        public String OldIdentity { get; set; }
        public String NewIdentity { get; set; }
        public String ContractAddress { get; set; }
        public UInt64 BlockNumber { get; set; }
        public string ManagementWallet { get; set; }
        public ulong GasUsed { get; set; }
        public ulong GasPrice { get; set; }
        public int BlockchainID { get; set; }

        public static void InsertIfNotExist(MySqlConnection connection, OTContract_Profile_IdentityTransferred model)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTContract_Profile_IdentityTransferred WHERE TransactionHash = @hash AND BlockchainID = @BlockchainID", new
            {
                hash = model.TransactionHash,
                model.BlockchainID
            });

            if (count == 0)
            {
                connection.Execute(
                    @"INSERT INTO OTContract_Profile_IdentityTransferred(TransactionHash, NodeId, OldIdentity, NewIdentity, ContractAddress, BlockNumber, ManagementWallet
,GasPrice,GasUsed, BlockchainID) VALUES(@TransactionHash, @NodeId, @OldIdentity, @NewIdentity, @ContractAddress, @BlockNumber, @ManagementWallet, @GasPrice, @GasUsed, @BlockchainID)",
                    new
                    {
                        model.TransactionHash,
                        model.NodeId,
                        model.OldIdentity,
                        model.NewIdentity,
                        model.ContractAddress,
                        model.BlockNumber,
                        model.ManagementWallet,
                        model.GasPrice,
                        model.GasUsed,
                        model.BlockchainID
                    });
            }
        }
    }
}