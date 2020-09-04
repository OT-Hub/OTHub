using System;
using Dapper;
using MySql.Data.MySqlClient;

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


        public static void InsertIfNotExist(MySqlConnection connection, OTContract_Profile_IdentityTransferred model)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTContract_Profile_IdentityTransferred WHERE TransactionHash = @hash", new
            {
                hash = model.TransactionHash
            });

            if (count == 0)
            {
                connection.Execute(
                    @"INSERT INTO OTContract_Profile_IdentityTransferred(TransactionHash, NodeId, OldIdentity, NewIdentity, ContractAddress, BlockNumber, ManagementWallet
,GasPrice,GasUsed) VALUES(@TransactionHash, @NodeId, @OldIdentity, @NewIdentity, @ContractAddress, @BlockNumber, @ManagementWallet, @GasPrice, @GasUsed)",
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
                        model.GasUsed
                    });
            }
        }
    }
}