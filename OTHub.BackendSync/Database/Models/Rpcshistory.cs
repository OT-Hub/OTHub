using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;

namespace OTHub.BackendSync.Database.Models
{
    public class Rpcshistory
    {
        public long ID { get; set; }
        public int RPCID { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
        public int Duration { get; set; }
        public int? RedirectedRPCID { get; set; }
        public string Method { get; set; }

        public async Task Insert(MySqlConnection connection)
        {
            await connection.ExecuteAsync(
                @"INSERT INTO Rpcshistory(RPCID, Timestamp, Success, Duration, RedirectedRPCID, Method) VALUES(@RPCID, @Timestamp, @Success, @Duration, @RedirectedRPCID, @Method)"
                , new
                {
                    RPCID,
                    Timestamp,
                    Success,
                    Duration,
                    RedirectedRPCID,
                    Method
                });
        }
    }
}