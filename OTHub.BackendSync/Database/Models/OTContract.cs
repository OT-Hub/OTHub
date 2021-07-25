using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using OTHub.Settings.Abis;

namespace OTHub.BackendSync.Database.Models
{
    public class OTContract
    {
        public OTContract(ulong syncBlockNumber, ulong fromBlockNumber)
        {
            SyncBlockNumber = syncBlockNumber;
            FromBlockNumber = fromBlockNumber;
        }

        //For dapper to use
        protected OTContract()
        {
            
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

        public static async Task Insert(MySqlConnection connection, OTContract contract)
        {
            await connection.ExecuteAsync("INSERT INTO OTContract(Address, Type, IsLatest, FromBlockNumber, SyncBlockNumber, ToBlockNumber, IsArchived, LastSyncedTimestamp, BlockchainID) VALUES(@address, @type, @isLatest, @fromBlockNo, @syncBlockNo, @toBlockNo, @IsArchived, @LastSyncedTimestamp, @BlockchainID)",
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

        public static async Task Update(MySqlConnection connection, OTContract contract, bool onlyAllowIsLatestUpdate, bool onlyAllowIsArchivedUpdate)
        {
            if (onlyAllowIsArchivedUpdate)
            {
                await connection.ExecuteAsync("UPDATE OTContract SET IsArchived = @IsArchived WHERE Address = @address AND BlockchainID = @blockchainID", new
                {
                    address = contract.Address,
                    IsArchived = contract.IsArchived,
                    blockchainID = contract.BlockchainID
                });
            }
            else if (onlyAllowIsLatestUpdate)
            {
                await connection.ExecuteAsync("UPDATE OTContract SET IsLatest = @isLatest WHERE Address = @address AND BlockchainID = @blockchainID", new
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
                    await connection.ExecuteAsync(@"UPDATE OTContract
SET IsLatest = 0
WHERE Type = @type AND IsLatest = 1 AND Address != @address AND BlockchainID = @blockchainID", new
                    {
                        address = contract.Address,
                        type = contract.Type,
                        blockchainID = contract.BlockchainID
                    });
                }

                await connection.ExecuteAsync(@"UPDATE OTContract SET Type = @type, IsLatest = @isLatest, FromBlockNumber = @fromBlockNo, SyncBlockNumber = @syncBlockNo,
IsArchived = @IsArchived, LastSyncedTimestamp = @LastSyncedTimestamp, BlockchainID = @blockchainID
WHERE Address = @address and type = @type AND BlockchainID = @blockchainID", new
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

        public static async Task InsertOrUpdate(MySqlConnection connection, OTContract otContract, bool onlyAllowIsLatestUpdate = false)
        {
            if (otContract.Address == null || otContract.Address == "0x0000000000000000000000000000000000000000")
                return;

            var count = await connection.QueryFirstOrDefaultAsync<Int32>("SELECT COUNT(*) FROM OTContract WHERE Address = @address AND Type = @type AND BlockchainID = @blockchainID", new
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
                Console.WriteLine("Inserting " + otContract.Address + ". Type: " + (ContractTypeEnum)otContract.Type);

                await Insert(connection, otContract);
            }
            else
            {
                await Update(connection, otContract, onlyAllowIsLatestUpdate, false);
            }
        }

        //public static OTContract[] GetAll(MySqlConnection connection)
        //{
        //    return connection.Query<OTContract>("SELECT * FROM OTContract").ToArray();
        //}

        public static async Task<OTContract[]> GetByTypeAndBlockchain(MySqlConnection connection, int type, int blockchainID)
        {
            return (await connection.QueryAsync<OTContract>("SELECT * FROM OTContract where Type = @type AND BlockchainID = @blockchainID", new { type = type, blockchainID = blockchainID })).ToArray();
        }
    }
}