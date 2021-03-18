using System;
using Dapper;
using MySqlConnector;

namespace OTHub.BackendSync.Database.Models
{
    public class EthBlock
    {
        public UInt64 BlockNumber { get; set; }
        public String BlockHash { get; set; }
        public DateTime Timestamp { get; set; }
        public int BlockchainID { get; set; }

        public static void Insert(MySqlConnection connection, EthBlock model)
        {
            connection.Execute("INSERT INTO EthBlock VALUES(@blockNo, @blockHash, @timestamp, @blockchainID)", new
            {
                blockNo = model.BlockNumber,
                blockHash = model.BlockHash,
                timestamp = model.Timestamp,
                blockchainID = model.BlockchainID
            });
        }

        public static void Update(MySqlConnection connection, EthBlock model)
        {
            connection.Execute(
                "UPDATE EthBlock SET BlockNumber = @blockNo, BlockHash = @blockHash, Timestamp = @timestamp, BlockchainID = @blockchainID" +
                " WHERE BlockNumber = @blockNo AND BlockchainID = @blockchainID", new
                {
                    blockNo = model.BlockNumber,
                    blockHash = model.BlockHash,
                    timestamp = model.Timestamp,
                    blockchainID = model.BlockchainID
                });
        }

        public static void InsertOrUpdate(MySqlConnection connection, EthBlock model)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM EthBlock WHERE BlockNumber = @blockNo AND BlockchainID = @blockchainID", new
            {
                blockNo = model.BlockNumber,
                blockchainID = model.BlockchainID
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

        public static EthBlock GetByNumber(MySqlConnection connection, UInt64 blockNo, int blockchainID)
        {
            return connection.QueryFirstOrDefault<EthBlock>("SELECT * FROM EthBlock where BlockNumber = @blockNo AND BlockchainID = @blockchainID", new
            {
                blockNo = blockNo,
                blockchainID = blockchainID
            });
        }
    }
}