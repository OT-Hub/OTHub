using System;
using System.Linq;
using Dapper;
using MySql.Data.MySqlClient;
using OTHelperNetStandard.Tasks;

namespace OTHelperNetStandard.Models.Database
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
        public int Type { get; set; }
        public bool IsLatest { get; set; }

        public UInt64 FromBlockNumber { get; set; }
        public UInt64 SyncBlockNumber { get; set; }

        public bool IsArchived { get; set; }
        public DateTime? LastSyncedTimestamp { get; set; }

        public static void Insert(MySqlConnection connection, OTContract contract)
        {
            connection.Execute("INSERT INTO OTContract(Address, Type, IsLatest, FromBlockNumber, SyncBlockNumber, ToBlockNumber, IsArchived, LastSyncedTimestamp) VALUES(@address, @type, @isLatest, @fromBlockNo, @syncBlockNo, @toBlockNo, @IsArchived, @LastSyncedTimestamp)",
                new
                {
                    address = contract.Address,
                    type = contract.Type,
                    isLatest = contract.IsLatest,
                    fromBlockNo = contract.FromBlockNumber,
                    syncBlockNo = contract.SyncBlockNumber,
                    toBlockNo = (ulong?)null,
                    IsArchived = contract.IsArchived,
                    LastSyncedTimestamp = contract.LastSyncedTimestamp
                });
        }

        public static void Update(MySqlConnection connection, OTContract contract, bool onlyAllowIsLatestUpdate, bool onlyAllowIsArchivedUpdate)
        {
            if (onlyAllowIsArchivedUpdate)
            {
                connection.Execute("UPDATE OTContract SET IsArchived = @IsArchived WHERE Address = @address", new
                {
                    address = contract.Address,
                    IsArchived = contract.IsArchived
                });
            }
            else if (onlyAllowIsLatestUpdate)
            {
                connection.Execute("UPDATE OTContract SET IsLatest = @isLatest WHERE Address = @address", new
                {
                    address = contract.Address,
                    isLatest = contract.IsLatest
                });
            }
            else
            {
                connection.Execute("UPDATE OTContract SET Type = @type, IsLatest = @isLatest, FromBlockNumber = @fromBlockNo, SyncBlockNumber = @syncBlockNo, IsArchived = @IsArchived, LastSyncedTimestamp = @LastSyncedTimestamp WHERE Address = @address and type = @type", new
                {
                    address = contract.Address,
                    type = contract.Type,
                    isLatest = contract.IsLatest,
                    fromBlockNo = contract.FromBlockNumber,
                    syncBlockNo = contract.SyncBlockNumber,
                    IsArchived = contract.IsArchived,
                    LastSyncedTimestamp = contract.LastSyncedTimestamp
                });
            }
        }

        public static void InsertOrUpdate(MySqlConnection connection, OTContract otContract, bool onlyAllowIsLatestUpdate = false)
        {
            if (otContract.Address == "0x0000000000000000000000000000000000000000")
                return;

            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTContract WHERE Address = @address AND Type = @type", new
            {
                address = otContract.Address,
                type = otContract.Type
            });

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

        public static OTContract[] GetByType(MySqlConnection connection, int type)
        {
            return connection.Query<OTContract>("SELECT * FROM OTContract where Type = @type", new {type = type}).ToArray();
        }
    }
}