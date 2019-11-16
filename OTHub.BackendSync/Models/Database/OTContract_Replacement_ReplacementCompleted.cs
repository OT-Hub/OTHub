using System;
using System.Collections.Generic;
using System.Text;
using Dapper;
using MySql.Data.MySqlClient;

namespace OTHelperNetStandard.Models.Database
{
    public class OTContract_Replacement_ReplacementCompleted
    {
        public string TransactionHash { get; set; }
        public UInt64 BlockNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public String OfferId { get; set; }
        public String ChallengerIdentity { get; set; }
        public String ChosenHolder { get; set; }
        public ulong GasPrice { get; set; }
        public ulong GasUsed { get; set; }

        public static void InsertIfNotExist(MySqlConnection connection, OTContract_Replacement_ReplacementCompleted model)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTContract_Replacement_ReplacementCompleted WHERE TransactionHash = @hash", new
            {
                hash = model.TransactionHash
            });

            if (count == 0)
            {
                connection.Execute(
                    @"INSERT INTO OTContract_Replacement_ReplacementCompleted
(TransactionHash, BlockNumber, Timestamp, ChallengerIdentity, OfferId, ChosenHolder, GasPrice, GasUsed)
VALUES(@TransactionHash, @BlockNumber, @Timestamp, @ChallengerIdentity, @OfferId, @ChosenHolder, @GasPrice, @GasUsed)",
                    new
                    {
                        model.TransactionHash,
                        model.BlockNumber,
                        model.Timestamp,
                        model.ChallengerIdentity,
                        model.OfferId,
                        model.ChosenHolder,
                        model.GasPrice,
                        model.GasUsed
                    });
            }
        }
    }
}