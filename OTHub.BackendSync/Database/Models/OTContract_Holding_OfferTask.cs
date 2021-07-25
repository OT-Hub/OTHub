using System;
using Dapper;
using MySqlConnector;

namespace OTHub.BackendSync.Database.Models
{
    public class OTContract_Holding_OfferTask
    {
        public String TransactionHash { get; set; }
        public String ContractAddress { get; set; }
        public String DataSetId { get; set; }
        public String DCNodeId { get; set; }
        public String OfferId { get; set; }
        public String Task { get; set; }
        public UInt64 BlockNumber { get; set; }
        public ulong GasPrice { get; set; }
        public ulong GasUsed { get; set; }
        public int BlockchainID { get; set; }

        public static void InsertIfNotExist(MySqlConnection connection, OTContract_Holding_OfferTask model)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTContract_Holding_OfferTask WHERE TransactionHash = @hash AND BlockchainID = @blockchainID", new
            {
                hash = model.TransactionHash,
                blockchainID = model.BlockchainID
            });

            if (count == 0)
            {
                connection.Execute(
                    @"INSERT INTO OTContract_Holding_OfferTask(TransactionHash, ContractAddress, DataSetId, DCNodeId, OfferId, Task, BlockNumber, GasPrice, GasUsed, BlockchainID)
VALUES(@TransactionHash, @ContractAddress, @DataSetId, @DCNodeId, @OfferId, @Task, @BlockNumber, @GasPrice, @GasUsed, @BlockchainID)",
                    new
                    {
                        model.TransactionHash,
                        model.ContractAddress,
                        model.DataSetId,
                        model.DCNodeId,
                        model.OfferId,
                        model.Task,
                        model.BlockNumber,
                        model.GasPrice,
                        model.GasUsed,
                        model.BlockchainID
                    });
            }
        }

        public static Boolean Exists(MySqlConnection connection, string offerID, int blockchainID)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTContract_Holding_OfferTask WHERE OfferID = @offerID AND BlockchainID = @blockchainID", new
            {
                offerID = offerID,
                blockchainID = blockchainID
            });

            return count > 0;
        }
    }
}