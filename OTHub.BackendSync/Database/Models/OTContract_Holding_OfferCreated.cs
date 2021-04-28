using System;
using System.Linq;
using Dapper;
using MySqlConnector;

namespace OTHub.BackendSync.Database.Models
{
    public class OTContract_Holding_OfferCreated
    {
        public string OfferID { get; set; }
        public UInt64 BlockNumber { get; set; }
        public string DCNodeId { get; set; }

        public string DataSetId { get; set; }

        public UInt64 HoldingTimeInMinutes { get; set; }

        public decimal TokenAmountPerHolder { get; set; }
        public UInt64 DataSetSizeInBytes { get; set; }
        public UInt64 LitigationIntervalInMinutes { get; set; }


        public string TransactionHash { get; set; }
        public UInt64 TransactionIndex { get; set; }
        public DateTime Timestamp { get; set; }

        public String ContractAddress { get; set; }
        public UInt64 GasUsed { get; set; }
        public string Data { get; set; }
        public UInt64 GasPrice { get; set; }

        public int BlockchainID { get; set; }

        public static Boolean Exists(MySqlConnection connection, string offerID, int blockchainID)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTContract_Holding_OfferCreated WHERE OfferID = @offerID AND BlockchainID = @blockchainID", new
            {
                offerID = offerID,
                blockchainID = blockchainID
            });

            return count > 0;
        }

        public static void InsertIfNotExist(MySqlConnection connection, OTContract_Holding_OfferCreated model)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTContract_Holding_OfferCreated WHERE OfferID = @offerID AND BlockchainID = blockchainID", new
            {
                offerID = model.OfferID,
                blockchainID = model.BlockchainID
            });

            if (count == 0)
            {
                connection.Execute(
                    @"INSERT INTO OTContract_Holding_OfferCreated (OfferID,DCNodeId, DataSetId, TransactionIndex,
Timestamp, BlockNumber, TransactionHash, DataSetSizeInBytes, TokenAmountPerHolder, HoldingTimeInMinutes, LitigationIntervalInMinutes, ContractAddress, GasUsed, Processed, Data, GasPrice, BlockchainID) VALUES(@OfferID, @DCNodeId, @DataSetId, @TransactionIndex, @Timestamp, @BlockNumber, @TransactionHash,
@DataSetSizeInBytes, @TokenAmountPerHolder, @HoldingTimeInMinutes, @LitigationIntervalInMinutes, @ContractAddress, @GasUsed, 0, @Data, @GasPrice, @BlockchainID)",
                    new
                    {
                        model.OfferID,
                        model.DCNodeId,
                        model.DataSetId,
                        model.TransactionIndex,
                        model.Timestamp,
                        model.BlockNumber,
                        model.TransactionHash,
                        model.DataSetSizeInBytes,
                        model.TokenAmountPerHolder,
                        model.HoldingTimeInMinutes,
                        model.LitigationIntervalInMinutes,
                        model.ContractAddress,
                        model.GasUsed,
                        model.Data,
                        model.GasPrice,
                        model.BlockchainID
                    });
            }
        }

        public static OTContract_Holding_OfferCreated[] GetUnprocessed(MySqlConnection connection, int blockchainID)
        {
            return connection.Query<OTContract_Holding_OfferCreated>("SELECT * FROM OTContract_Holding_OfferCreated WHERE Processed = 0 AND BlockchainID = @blockchainID", new
            {
                blockchainID = blockchainID
            }).ToArray();
        }

        public static void SetProcessed(MySqlConnection connection, OTContract_Holding_OfferCreated offerToAdd)
        {
            connection.Execute(@"UPDATE OTContract_Holding_OfferCreated SET Processed = 1 WHERE OfferID = @offerID AND BlockchainID = @blockchainID", new
            {
                offerID = offerToAdd.OfferID,
                blockchainID = offerToAdd.BlockchainID
            });
        }
    }
}