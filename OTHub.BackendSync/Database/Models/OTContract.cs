using System;
using System.Linq;
using Dapper;
using MySqlConnector;

namespace OTHub.BackendSync.Database.Models
{
    public class OTContract
    {
        public OTContract()
        {
            SyncBlockNumber = TaskRun.SyncBlockNumber;
            FromBlockNumber = TaskRun.FromBlockNumber;
        }

        public int ID { get; set; }

        public String Address { get; set; }
        public int BlockchainID { get; set; }
        public int Type { get; set; }
        public bool IsLatest { get; set; }

        public UInt64 FromBlockNumber { get; set; }
        public UInt64 SyncBlockNumber { get; set; }

        public bool IsArchived { get; set; }
        public DateTime? LastSyncedTimestamp { get; set; }

        public static void Insert(MySqlConnection connection, OTContract contract)
        {
            connection.Execute("INSERT INTO OTContract(Address, Type, IsLatest, FromBlockNumber, SyncBlockNumber, ToBlockNumber, IsArchived, LastSyncedTimestamp, BlockchainID) VALUES(@address, @type, @isLatest, @fromBlockNo, @syncBlockNo, @toBlockNo, @IsArchived, @LastSyncedTimestamp, @BlockchainID)",
                new
                {
                    address = contract.Address,
                    type = contract.Type,
                    isLatest = contract.IsLatest,
                    fromBlockNo = contract.FromBlockNumber,
                    syncBlockNo = contract.SyncBlockNumber,
                    toBlockNo = (ulong?)null,
                    IsArchived = contract.IsArchived,
                    LastSyncedTimestamp = contract.LastSyncedTimestamp,
                    BlockchainID = contract.BlockchainID
                });
        }

        public static void Update(MySqlConnection connection, OTContract contract, bool onlyAllowIsLatestUpdate, bool onlyAllowIsArchivedUpdate)
        {
            if (onlyAllowIsArchivedUpdate)
            {
                connection.Execute("UPDATE OTContract SET IsArchived = @IsArchived WHERE Address = @address AND BlockchainID = @blockchainID", new
                {
                    address = contract.Address,
                    IsArchived = contract.IsArchived,
                    blockchainID = contract.BlockchainID
                });
            }
            else if (onlyAllowIsLatestUpdate)
            {
                connection.Execute("UPDATE OTContract SET IsLatest = @isLatest WHERE Address = @address AND BlockchainID = @blockchainID", new
                {
                    address = contract.Address,
                    isLatest = contract.IsLatest,
                    blockchainID = contract.BlockchainID
                });
            }
            else
            {
                if (contract.IsLatest)
                {
                    connection.Execute(@"UPDATE OTContract
SET IsLatest = 0
WHERE Type = @type AND IsLatest = 1 AND Address != @address AND BlockchainID = @blockchainID", new
                    {
                        address = contract.Address,
                        type = contract.Type,
                        blockchainID = contract.BlockchainID
                    });
                }

                connection.Execute(@"UPDATE OTContract SET Type = @type, IsLatest = @isLatest, FromBlockNumber = @fromBlockNo, SyncBlockNumber = @syncBlockNo,
IsArchived = @IsArchived, LastSyncedTimestamp = @LastSyncedTimestamp, BlockchainID = @blockchainID
WHERE Address = @address and type = @type", new
                {
                    address = contract.Address,
                    type = contract.Type,
                    isLatest = contract.IsLatest,
                    fromBlockNo = contract.FromBlockNumber,
                    syncBlockNo = contract.SyncBlockNumber,
                    IsArchived = contract.IsArchived,
                    LastSyncedTimestamp = contract.LastSyncedTimestamp,
                    blockchainID = contract.BlockchainID
                });
            }
        }

        public static void InsertOrUpdate(MySqlConnection connection, OTContract otContract, bool onlyAllowIsLatestUpdate = false)
        {
            if (otContract.Address == "0x0000000000000000000000000000000000000000")
                return;

            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTContract WHERE Address = @address AND Type = @type AND BlockchainID = @blockchainID", new
            {
                address = otContract.Address,
                type = otContract.Type,
                blockchainID = otContract.BlockchainID
            });

            if (otContract.IsLatest)
            {
                otContract.IsArchived = false;
            }

            if (count == 0)
            {
                Console.WriteLine("Inserting " + otContract.Address + ". Type: " + otContract.Type);

                Insert(connection, otContract);
            }
            else
            {
                Update(connection, otContract, onlyAllowIsLatestUpdate, false);
            }
        }

        public static OTContract[] GetAll(MySqlConnection connection)
        {
            return connection.Query<OTContract>("SELECT * FROM OTContract").ToArray();
        }

        public static OTContract[] GetByTypeAndBlockchain(MySqlConnection connection, int type, int blockchainID)
        {
            return connection.Query<OTContract>("SELECT * FROM OTContract where Type = @type AND BlockchainID = @blockchainID", new { type = type, blockchainID = blockchainID }).ToArray();
        }
    }
}