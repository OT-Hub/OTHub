using System;
using System.Diagnostics;
using Dapper;
using MySqlConnector;

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
        public DateTime? LastCheckedOnlineTimestamp { get; set; }
        public string NetworkId { get; set; }

        public static void Insert(MySqlConnection connection, IpInfo model)
        {
            connection.Execute(
                @"INSERT INTO otnode_ipinfov2(NodeId, Wallet, Port, Timestamp, Hostname, LastCheckedOnlineTimestamp, NetworkId) VALUES(@NodeId, @Wallet,@Port, @Timestamp, @Hostname, @LastCheckedOnlineTimestamp, @NetworkId)",
                new
                {
                    model.NodeId,
                    model.Wallet,
                    model.Timestamp,
                    model.Hostname,
                    model.Port,
                    model.LastCheckedOnlineTimestamp,
                    model.NetworkId
                });
        }

        public static void UpdateTimestamp(MySqlConnection connection, IpInfo model)
        {
            connection.Execute(
                @"UPDATE otnode_ipinfov2 SET Timestamp = @Timestamp WHERE NodeId = @NodeId",
                new
                {
                    model.NodeId,
                    model.Timestamp,
                });
        }

        public static void UpdateTimestampAndLastChecked(MySqlConnection connection, IpInfo model)
        {
            connection.Execute(
                @"UPDATE otnode_ipinfov2 SET Timestamp = @Timestamp, LastCheckedOnlineTimestamp = @LastCheckedOnlineTimestamp WHERE NodeId = @NodeId",
                new
                {
                    model.NodeId,
                    model.Timestamp,
                    model.LastCheckedOnlineTimestamp
                });
        }

        public static void UpdateForCleanupNodesFromDifferentNetworks(MySqlConnection connection, IpInfo model)
        {
            connection.Execute(
                @"UPDATE otnode_ipinfov2 SET UnknownNodeResponseCount = 0, Wallet = @Wallet, Port = @Port, NetworkId = @NetworkId,
                                    Timestamp = @Timestamp, Hostname = @Hostname, LastCheckedGetContactTimestamp = @LastCheckedGetContactTimestamp, UnknownNodeResponseCount = 0 WHERE NodeId = @NodeId",
                new
                {
                    model.NodeId,
                    model.Wallet,
                    model.Timestamp,
                    model.Hostname,
                    model.Port,
                    model.NetworkId,
                    LastCheckedGetContactTimestamp = DateTime.Now
                });
        }

        internal static void UnknownResponse(MySqlConnection connection, string nodeToCheck)
        {
            connection.Execute(
    @"UPDATE otnode_ipinfov2 SET UnknownNodeResponseCount = UnknownNodeResponseCount + 1, LastCheckedGetContactTimestamp = @LastCheckedGetContactTimestamp WHERE NodeId = @NodeId",
    new
    {
        NodeId = nodeToCheck,
        LastCheckedGetContactTimestamp = DateTime.Now
    });
        }

        //public static void InsertOrUpdate(MySqlConnection connection, IpInfo model)
        //{
        //    var count = connection.QueryFirstOrDefault<Int32>(
        //        "SELECT COUNT(*) FROM otnode_ipinfov2 WHERE NodeId = @NodeId", new
        //        {
        //            model.NodeId
        //        });

        //    if (count == 0)
        //    {
        //        Insert(connection, model);
        //    }
        //    else
        //    {
        //        Update(connection, model);
        //    }
        //}

        //public static void UpdateHost(MySqlConnection connection, string nodeId, string dataHostname, int dataPort)
        //{

        //}

        public static void UpdateHost(MySqlConnection connection, string nodeId, string dataHostname, int dataPort, DateTime infoTimestamp, DateTime? infoLastCheckedTimestamp, string networkID)
        {
            connection.Execute(
                @"UPDATE otnode_ipinfov2 SET UnknownNodeResponseCount = 0, Port = @Port, Hostname = @Hostname, LastCheckedOnlineTimestamp = @LastCheckedOnlineTimestamp, NetworkId = @NetworkId, Timestamp = @Timestamp WHERE NodeId = @NodeId",
                new
                {
                    NodeId = nodeId,
                    Hostname = dataHostname,
                    Port = dataPort,
                    Timestamp = infoTimestamp,
                    LastCheckedOnlineTimestamp = infoLastCheckedTimestamp,
                    NetworkId = networkID
                });
        }
    }

}