using System;
using System.Diagnostics;
using Dapper;
using MySql.Data.MySqlClient;

namespace OTHub.BackendSync.Database.Models
{
    [DebuggerDisplay("{Hostname} - {Port}")]
    public class IpInfo
    {
        public string NodeId { get; set; }
        public string Hostname { get; set; }
        public int Port { get; set; }
        public DateTime Timestamp { get; set; }
        public string Wallet { get; set; }
        public DateTime? LastCheckedTimestamp { get; set; }
        public string NetworkId { get; set; }

        public static void Insert(MySqlConnection connection, IpInfo model)
        {
            connection.Execute(
                @"INSERT INTO otnode_ipinfo(NodeId, Wallet, Port, Timestamp, Hostname, LastCheckedTimestamp, NetworkId) VALUES(@NodeId, @Wallet,@Port, @Timestamp, @Hostname, @LastCheckedTimestamp, @NetworkId)",
                new
                {
                    model.NodeId,
                    model.Wallet,
                    model.Timestamp,
                    model.Hostname,
                    model.Port,
                    model.LastCheckedTimestamp,
                    model.NetworkId
                });
        }

        public static void UpdateTimestamp(MySqlConnection connection, IpInfo model)
        {
            connection.Execute(
                @"UPDATE otnode_ipinfo SET Timestamp = @Timestamp WHERE NodeId = @NodeId",
                new
                {
                    model.NodeId,
                    model.Timestamp,
                });
        }

        public static void UpdateTimestampAndLastChecked(MySqlConnection connection, IpInfo model)
        {
            connection.Execute(
                @"UPDATE otnode_ipinfo SET Timestamp = @Timestamp, LastCheckedTimestamp = @LastCheckedTimestamp WHERE NodeId = @NodeId",
                new
                {
                    model.NodeId,
                    model.Timestamp,
                    model.LastCheckedTimestamp
                });
        }

        public static void Update(MySqlConnection connection, IpInfo model)
        {
            connection.Execute(
                @"UPDATE otnode_ipinfo SET Wallet = @Wallet, Port = @Port,
                                    Timestamp = @Timestamp, Hostname = @Hostname, LastCheckedTimestamp = @LastCheckedTimestamp WHERE NodeId = @NodeId",
                new
                {
                    model.NodeId,
                    model.Wallet,
                    model.Timestamp,
                    model.Hostname,
                    model.Port,
                    model.LastCheckedTimestamp
                });
        }

        public static void InsertOrUpdate(MySqlConnection connection, IpInfo model)
        {
            var count = connection.QueryFirstOrDefault<Int32>(
                "SELECT COUNT(*) FROM otnode_ipinfo WHERE NodeId = @NodeId", new
                {
                    model.NodeId
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

        public static void UpdateHost(MySqlConnection connection, string nodeId, string dataHostname, int dataPort)
        {

        }

        public static void UpdateHost(MySqlConnection connection, string nodeId, string dataHostname, int dataPort, DateTime infoTimestamp, DateTime? infoLastCheckedTimestamp, string networkID)
        {
            connection.Execute(
                @"UPDATE otnode_ipinfo SET  Port = @Port, Hostname = @Hostname, LastCheckedTimestamp = @LastCheckedTimestamp, NetworkId = @NetworkId, Timestamp = @Timestamp WHERE NodeId = @NodeId",
                new
                {
                    NodeId = nodeId,
                    Hostname = dataHostname,
                    Port = dataPort,
                    Timestamp = infoTimestamp,
                    LastCheckedTimestamp = infoLastCheckedTimestamp,
                    NetworkId = networkID
                });
        }
    }

}