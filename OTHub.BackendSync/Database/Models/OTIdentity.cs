using System;
using System.Linq;
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

        public static void InsertIfNotExist(MySqlConnection connection, OTIdentity model)
        {
            var count = GetCount(connection, model.Identity);

            if (count == 0)
            {
                Insert(connection, model);
            }
        }

        public static OTIdentity[] GetAll(MySqlConnection connection, int blockchainID)
        {
            return connection.Query<OTIdentity>("SELECT * FROM OTIdentity where BlockchainID = @blockchainID", new
            {
                blockchainID = blockchainID
            }).ToArray();
        }

        public static OTIdentity[] GetByVersion(MySqlConnection connection, int version)
        {
            return connection.Query<OTIdentity>("SELECT * FROM OTIdentity WHERE Version = @version", new {version}).ToArray();
        }

        public static int GetCount(MySqlConnection connection, string identity)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTIdentity WHERE Identity = @Identity", new
            {
                Identity = identity
            });
            return count;
        }

        public static void Insert(MySqlConnection connection, OTIdentity model)
        {
            connection.Execute(
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

        public static void UpdateFromProfileFunction(MySqlConnection connection, OTIdentity model)
        {
            connection.Execute(@"UPDATE OTIdentity
SET Stake = @Stake, StakeReserved = @StakeReserved, Reputation = @Reputation, WithdrawalPending = @WithdrawalPending, WithdrawalTimestamp = @WithdrawalTimestamp, WithdrawalAmount = @WithdrawalAmount, NodeId = @NodeId, LastSyncedTimestamp = @LastSyncedTimestamp
WHERE Identity = @Identity", new
            {
                model.Identity,
                Stake = model.Stake ?? 0,
                StakeReserved = model.StakeReserved ?? 0,
                model.Reputation,
                model.WithdrawalAmount,
                model.WithdrawalPending,
                model.WithdrawalTimestamp,
                model.NodeId,
                model.LastSyncedTimestamp
            });
        }

        public static void UpdateFromPaidoutAndApprovedCalculation(MySqlConnection connection, OTIdentity model)
        {
            connection.Execute(@"UPDATE OTIdentity
SET Paidout = @Paidout, Approved = @Approved, ActiveOffers = @ActiveOffers, OffersLast7Days = @OffersLast7Days, TotalOffers = @TotalOffers, ManagementWallet = @ManagementWallet
WHERE Identity = @Identity", new
            {
                model.Identity,
                Paidout = model.Paidout ?? 0,
                Approved = model.Approved ?? false,
                ActiveOffers = model.ActiveOffers ?? 0,
                OffersLast7Days = model.OffersLast7Days ?? 0,
                TotalOffers = model.TotalOffers ?? 0,
                ManagementWallet = model.ManagementWallet
            });
        }

        public static void UpdateLastSyncedTimestamp(MySqlConnection connection, OTIdentity model)
        {
            connection.Execute(@"UPDATE OTIdentity
SET LastSyncedTimestamp = @LastSyncedTimestamp
WHERE Identity = @Identity", new
            {
                model.Identity,
                model.LastSyncedTimestamp
            });
        }
    }
}