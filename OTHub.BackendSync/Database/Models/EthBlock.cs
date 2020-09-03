using System;
using Dapper;
using MySql.Data.MySqlClient;

namespace OTHub.BackendSync.Models.Database
{
    public class EthBlock
    {
        public UInt64 BlockNumber { get; set; }
        public String BlockHash { get; set; }
        public DateTime Timestamp { get; set; }

        public static void Insert(MySqlConnection connection, EthBlock model)
        {
            connection.Execute("INSERT INTO EthBlock VALUES(@blockNo, @blockHash, @timestamp)", new
            {
                blockNo = model.BlockNumber,
                blockHash = model.BlockHash,
                timestamp = model.Timestamp
            });
        }

        public static void Update(MySqlConnection connection, EthBlock model)
        {
            connection.Execute("UPDATE EthBlock SET BlockNumber = @blockNo, BlockHash = @blockHash, Timestamp = @timestamp WHERE BlockNumber = @blockNo", new
            {
                blockNo = model.BlockNumber,
                blockHash = model.BlockHash,
                timestamp = model.Timestamp
            });
        }

        public static void InsertOrUpdate(MySqlConnection connection, EthBlock model)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM EthBlock WHERE BlockNumber = @blockNo", new
            {
                blockNo = model.BlockNumber
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

        public static EthBlock GetByNumber(MySqlConnection connection, UInt64 blockNo)
        {
            return connection.QueryFirstOrDefault<EthBlock>("SELECT * FROM EthBlock where BlockNumber = @blockNo", new { blockNo = blockNo });
        }
    }
}