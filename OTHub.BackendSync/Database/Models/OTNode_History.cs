using System;
using Dapper;
using MySqlConnector;

namespace OTHub.BackendSync.Database.Models
{
    public class OTNode_History
    {
        public UInt64 Id { get; set; }
        public String NodeId { get; set; }
        public DateTime Timestamp { get; set; }
        public Boolean Success { get; set; }
        public Int16 Duration { get; set; }

        public static void Insert(MySqlConnection connection, OTNode_History row)
        {
            connection.Execute(
                @"INSERT INTO OTNode_History(NodeId, Timestamp, Success, Duration)
VALUES(@NodeId, @Timestamp, @Success, @Duration)",
                new
                {
                    row.NodeId,
                    row.Timestamp,
                    row.Duration,
                    row.Success
                });
        }

        public static void InsertIfNotExist(MySqlConnection connection, OTNode_History row)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTNode_History WHERE NodeId = @NodeId AND Timestamp = @Timestamp", new
            {
                row.NodeId,
                row.Timestamp
            });

            if (count == 0)
            {
                Insert(connection, row);
            }
        }
    }
}