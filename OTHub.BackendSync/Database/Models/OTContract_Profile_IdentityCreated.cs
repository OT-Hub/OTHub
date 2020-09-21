using System;
using Dapper;
using MySqlConnector;

namespace OTHub.BackendSync.Database.Models
{
    public class OTContract_Profile_IdentityCreated
    {
        public String TransactionHash { get; set; }
        public String Profile { get; set; }
        public String NewIdentity { get; set; }
        public String ContractAddress { get; set; }
        public UInt64 BlockNumber { get; set; }
        public ulong GasUsed { get; set; }
        public ulong GasPrice { get; set; }

        public static void Insert(MySqlConnection connection, OTContract_Profile_IdentityCreated model)
        {
            connection.Execute("INSERT INTO OTContract_Profile_IdentityCreated VALUES(@hash, @profile, @newIdentity, @contractAddress, @blockNumber, @GasPrice, @GasUsed)", new
            {
                hash = model.TransactionHash,
                profile = model.Profile,
                newIdentity = model.NewIdentity,
                contractAddress = model.ContractAddress,
                blockNumber = model.BlockNumber,
                model.GasUsed,
                model.GasPrice
            });
        }

        public static void Update(MySqlConnection connection, OTContract_Profile_IdentityCreated model)
        {
            connection.Execute(@"UPDATE OTContract_Profile_IdentityCreated SET TransactionHash = @hash, BlockNumber = @blockNumber,
Profile = @profile, NewIdentity = @newIdentity, GasUsed = @GasUsed, GasPrice = @GasPrice,
ContractAddress = @contractAddress WHERE TransactionHash = @hash", new
            {
                hash = model.TransactionHash,
                profile = model.Profile,
                newIdentity = model.NewIdentity,
                contractAddress = model.ContractAddress,
                blockNumber = model.BlockNumber,
                model.GasUsed,
                model.GasPrice
            });
        }

        public static void InsertOrUpdate(MySqlConnection connection, OTContract_Profile_IdentityCreated model)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTContract_Profile_IdentityCreated WHERE TransactionHash = @hash", new
            {
                hash = model.TransactionHash
            });

            if (count == 0)
            {
                Insert(connection, model);
            }
            else
            {
                Update(connection, model);
            }
        }
    }
}