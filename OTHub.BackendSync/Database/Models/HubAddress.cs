using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;

namespace OTHub.BackendSync.Database.Models
{
    public class HubAddress
    {
        public uint ID { get; set; }
        public string Address { get; set; }
        public int BlockchainID { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime? DateReplaced { get; set; }
        public UInt64 FromBlockNumber { get; set; }
        public UInt64 SyncBlockNumber { get; set; }

        public static async Task<bool> Exists(MySqlConnection connection, int blockchainID, string address)
        {
            return await connection.ExecuteScalarAsync<bool?>("SELECT 1 FROM hubaddresses where blockchainid = @blockchainID and Address = @address",
                new
                {
                    blockchainID,
                    address
                }) ?? false;
        }

        public static async Task Insert(MySqlConnection connection, int blockchainId, string hubAddress, ulong fromBlockNumber)
        {
            await connection.ExecuteAsync(
                @"INSERT INTO hubaddresses (Address, BlockchainID, DateAdded, DateReplaced, FromBlockNumber, SyncBlockNumber)
VALUES (@address, @blockchainID, @dateAdded, NULL, @fromBlockNumber, @syncBlockNumber)",
                new
                {
                    address = hubAddress,
                    blockchainID = blockchainId,
                    dateAdded = DateTime.UtcNow,
                    fromBlockNumber,
                    syncBlockNumber = fromBlockNumber - 1
                });
        }

        public static async Task<HubAddress> GetByID(MySqlConnection connection, int blockchainId, string hubAddress)
        {
            return await connection.QuerySingleOrDefaultAsync<HubAddress>(@"SELECT * FROM hubaddresses where blockchainid = @blockchainID and Address = @address",
                new
                {
                    blockchainID = blockchainId,
                    address = hubAddress
                });
        }

        public static async Task UpdateSyncBlockNumber(MySqlConnection connection, int blockchainID, string address, 
            ulong blockNumber)
        {
            await connection.ExecuteAsync(
                @"UPDATE hubaddresses
set syncblocknumber = @blockNumber
where blockchainid = @blockchainID and Address = @address",
                new
                {
                    blockchainID,
                    address = address,
                    blockNumber
                });
        }

        public static async Task MarkAsReplaced(MySqlConnection connection, int blockchainID, string address)
        {
            await connection.ExecuteAsync(
                @"UPDATE hubaddresses
set datereplaced = @date
where blockchainid = @blockchainID and Address = @address",
                new
                {
                    blockchainID,
                    address = address,
                    date = DateTime.UtcNow
                });
        }
    }
}