using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;

namespace OTHub.BackendSync.Database.Models
{
    public class OTIdentity
    {
        public string Identity { get; set; }
        public string TransactionHash { get; set; }
        public int Version { get; set; }

        public decimal? Stake { get; set; }
        public decimal? StakeReserved { get; set; }
        public UInt64? Reputation { get; set; }
        public bool? WithdrawalPending { get; set; }
        public UInt64? WithdrawalTimestamp { get; set; }
        public decimal? WithdrawalAmount { get; set; }
        public String NodeId { get; set; }
        public Boolean? Approved { get; set; }
        public decimal? Paidout { get; set; }

        public Int32? TotalOffers { get; set; }
        public Int32? OffersLast7Days { get; set; }
        public Int32? ActiveOffers { get; set; }
        public DateTime? LastSyncedTimestamp { get; set; }
        public DateTime? LastSeenTimestamp { get; set; }
        public String ManagementWallet { get; set; }
        public int BlockchainID { get; set; }

        public static async Task InsertIfNotExist(MySqlConnection connection, OTIdentity model)
        {
            var count = await GetCount(connection, model.Identity);

            if (count == 0)
            {
                await Insert(connection, model);
            }
        }

        public static async Task<OTIdentity[]> GetAll(MySqlConnection connection, int blockchainID)
        {
            return (await connection.QueryAsync<OTIdentity>("SELECT * FROM OTIdentity where BlockchainID = @blockchainID", new
            {
                blockchainID = blockchainID
            })).ToArray();
        }

        public static async Task<OTIdentity[]> GetByVersion(MySqlConnection connection, int version)
        {
            return (await connection.QueryAsync<OTIdentity>("SELECT * FROM OTIdentity WHERE Version = @version", new {version})).ToArray();
        }

        public static async Task<int> GetCount(MySqlConnection connection, string identity)
        {
            var count = await connection.QueryFirstOrDefaultAsync<Int32>("SELECT COUNT(*) FROM OTIdentity WHERE Identity = @Identity", new
            {
                Identity = identity
            });
            return count;
        }

        public static async Task Insert(MySqlConnection connection, OTIdentity model)
        {
            await connection.ExecuteAsync(
                @"INSERT INTO OTIdentity(Identity, TransactionHash, Version, BlockchainID)
VALUES(@Identity, @TransactionHash, @Version, @BlockchainID)",
                new
                {
                    model.Identity,
                    model.TransactionHash,
                    model.Version,
                    model.BlockchainID
                });
        }

        public static async Task UpdateFromProfileFunction(MySqlConnection connection, OTIdentity model)
        {
            await connection.ExecuteAsync(@"UPDATE OTIdentity
SET Stake = @Stake, StakeReserved = @StakeReserved, Reputation = @Reputation, WithdrawalPending = @WithdrawalPending, WithdrawalTimestamp = @WithdrawalTimestamp, WithdrawalAmount = @WithdrawalAmount, NodeId = @NodeId, LastSyncedTimestamp = @LastSyncedTimestamp
WHERE Identity = @Identity AND BlockchainID = @BlockchainID", new
            {
                model.Identity,
                Stake = model.Stake ?? 0,
                StakeReserved = model.StakeReserved ?? 0,
                model.Reputation,
                model.WithdrawalAmount,
                model.WithdrawalPending,
                model.WithdrawalTimestamp,
                model.NodeId,
                model.LastSyncedTimestamp,
                model.BlockchainID
            });
        }

        public static async Task UpdateFromPaidoutAndApprovedCalculation(MySqlConnection connection, OTIdentity model)
        {
            await connection.ExecuteAsync(@"UPDATE OTIdentity
SET Paidout = @Paidout, Approved = @Approved, ActiveOffers = @ActiveOffers, OffersLast7Days = @OffersLast7Days, TotalOffers = @TotalOffers, ManagementWallet = @ManagementWallet
WHERE Identity = @Identity AND BlockchainID = @BlockchainID", new
            {
                model.Identity,
                Paidout = model.Paidout ?? 0,
                Approved = model.Approved ?? false,
                ActiveOffers = model.ActiveOffers ?? 0,
                OffersLast7Days = model.OffersLast7Days ?? 0,
                TotalOffers = model.TotalOffers ?? 0,
                ManagementWallet = model.ManagementWallet,
                model.BlockchainID
            });
        }

        public static async Task UpdateLastSyncedTimestamp(MySqlConnection connection, OTIdentity model)
        {
            await connection.ExecuteAsync(@"UPDATE OTIdentity
SET LastSyncedTimestamp = @LastSyncedTimestamp
WHERE Identity = @Identity AND BlockchainID = @BlockchainID", new
            {
                model.Identity,
                model.LastSyncedTimestamp,
                model.BlockchainID
            });
        }
    }
}