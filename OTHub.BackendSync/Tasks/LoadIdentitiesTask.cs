using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;
using Nethereum.Contracts;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC;
using Nethereum.Web3;
using OTHub.BackendSync.Models.Contracts;
using OTHub.BackendSync.Models.Database;
using OTHub.Settings;

namespace OTHub.BackendSync.Tasks
{
    public class LoadIdentitiesTask : TaskRun
    {
        public class OfferGroupHolder
        {
            public String Identity { get; set; }
            public Int32 OffersTotal { get; set; }
            public Int32 OffersLast7Days { get; set; }
        }

        public class NodeManagementWallet
        {
            public String Identity { get; set; }
            public String CreateWallet { get; set; }
            public String TransferWallet { get; set; }
        }

        public class PayoutGroupHolder
        {
            public String Holder { get; set; }
            public Decimal Amount { get; set; }
        }

        public class ApprovedGroupNode
        {
            public String NodeId { get; set; }
            public Int32 Count { get; set; }
        }

        public override async Task Execute(Source source)
        {
            DateTime start = DateTime.UtcNow;
            ClientBase.ConnectionTimeout = new TimeSpan(0, 0, 5, 0);

            Random random = new Random();
            
            var randomMinutes = random.Next(0, 60);

            try
            {
                using (var connection =
                    new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                {
                    await CreateMissingIdentities(connection, cl);

                    var profileStorageContractAddress = OTContract
                        .GetByType(connection, (int) ContractType.ProfileStorage).Single(a => a.IsLatest);
                    var profileStorageContract =
                        new Contract(new EthApiService(cl.Client),
                            Constants.GetContractAbi(ContractType.ProfileStorage),
                            profileStorageContractAddress.Address);
                    var profileFunction = profileStorageContract.GetFunction("profile");

                    var currentIdentities = OTIdentity.GetAll(connection);

                    Dictionary<string, decimal> paidOutBalances = connection
                        .Query<PayoutGroupHolder>(
                            @"SELECT Holder, SUM(Amount) Amount FROM OTContract_Holding_Paidout GROUP BY Holder")
                        .ToDictionary(k => k.Holder, k => k.Amount);
                    Dictionary<string, int> approvedNodes = connection
                        .Query<ApprovedGroupNode>(
                            @"select NodeId, COUNT(*) as Count from otcontract_approval_nodeapproved GROUP BY NodeId")
                        .ToDictionary(k => k.NodeId, k => k.Count);
                    Dictionary<string, OfferGroupHolder> offerTotals = connection.Query<OfferGroupHolder>(
                        @"select i.Identity, COUNT(h.Holder) as OffersTotal,
(SELECT count(sh.ID) FROM otoffer_holders sh join otoffer so on so.OfferID = sh.OfferID where sh.Holder = i.Identity AND so.CreatedTimestamp >= Date_Add(NOW(), INTERVAL -7 DAY)) as OffersLast7Days
 from otidentity i
join otoffer_holders h on h.Holder = i.Identity
join otoffer o on o.OfferID = h.OfferID
GROUP BY i.Identity").ToDictionary(k => k.Identity, k => k);
                    NodeManagementWallet[] managementWallets = connection.Query<NodeManagementWallet>(@"SELECT I.Identity, PC.ManagementWallet as CreateWallet, IT.ManagementWallet TransferWallet FROM OTIdentity I
LEFT JOIN OTContract_Profile_ProfileCreated PC ON PC.Profile = I.Identity
LEFT JOIN OTContract_Profile_IdentityTransferred IT ON IT.NewIdentity = I.Identity
WHERE I.Version > 0").ToArray();


                    foreach (OTIdentity currentIdentity in currentIdentities)
                    {
                        bool updateProfile = true;

                        if (currentIdentity.Version != Constants.CurrentERCVersion)
                        {
                            if (currentIdentity.LastSyncedTimestamp.HasValue)
                            {
                                var adjustedNowTime = DateTime.Now.AddMinutes(randomMinutes);
                                
                                if ((adjustedNowTime - currentIdentity.LastSyncedTimestamp.Value).TotalDays <= 7)
                                    updateProfile = false;
                            }
                        }
                        else if (currentIdentity.LastSyncedTimestamp.HasValue)
                        {
                            var dates = connection.Query<DateTime?>(@"
select MAX(Timestamp) from otcontract_profile_identitycreated r
join ethblock b on r.BlockNumber = b.BlockNumber
WHERE r.NewIdentity = @identity
union
select MAX(Timestamp) from otcontract_profile_identitytransferred r
join ethblock b on r.BlockNumber = b.BlockNumber
WHERE r.NewIdentity = @identity
union
select MAX(Timestamp) from otcontract_profile_profilecreated r
join ethblock b on r.BlockNumber = b.BlockNumber
WHERE r.Profile = @identity
union
select MAX(Timestamp) from otcontract_profile_tokensdeposited r
join ethblock b on r.BlockNumber = b.BlockNumber
WHERE r.Profile = @identity
union
select MAX(Timestamp) from otcontract_profile_tokensreleased r
join ethblock b on r.BlockNumber = b.BlockNumber
WHERE r.Profile = @identity
union
select MAX(Timestamp) from otcontract_profile_tokensreserved r
join ethblock b on r.BlockNumber = b.BlockNumber
WHERE r.Profile = @identity
union
select MAX(Timestamp) from otcontract_profile_tokenstransferred r
join ethblock b on r.BlockNumber = b.BlockNumber
WHERE r.Sender = @identity OR r.Receiver = @identity
union
select MAX(Timestamp) from otcontract_profile_tokenswithdrawn r
join ethblock b on r.BlockNumber = b.BlockNumber
WHERE r.Profile = @identity
union
select MAX(b.Timestamp) from otoffer_holders h
join otoffer o on o.offerid = h.offerid
join ethblock b on b.blocknumber = o.finalizedblocknumber
where h.Holder = @identity
union
SELECT MAX(Timestamp)
FROM otcontract_litigation_litigationcompleted lc
WHERE lc.HolderIdentity = @identity AND lc.DHWasPenalized = 1
union
select MAX(b.Timestamp) from otcontract_holding_paidout p
join ethblock b on b.blocknumber = p.blocknumber
where p.holder = @identity
union
select MAX(b.Timestamp)  from otcontract_holding_offerfinalized of
join otcontract_holding_offercreated oc on oc.OfferId = of.OfferId
join OTIdentity i on i.NodeId = oc.DCNodeId
join ethblock b on of.BlockNumber = b.BlockNumber
where i.Identity = @identity", new
                            {
                                identity = currentIdentity.Identity
                            }).ToArray().Where(d => d.HasValue).Select(d => d.Value).ToArray();



                            var adjustedNowTime = DateTime.Now.AddMinutes(randomMinutes);

                            if (dates.Any())
                            {
                                var maxDate = dates.Max();

                                if (maxDate <= currentIdentity.LastSyncedTimestamp)
                                {
                                    if ((adjustedNowTime - currentIdentity.LastSyncedTimestamp.Value).TotalHours <= 16)
                                    {
                                        updateProfile = false;
                                    }
                                }
                            }
                            else
                            {
                                if ((adjustedNowTime - currentIdentity.LastSyncedTimestamp.Value).TotalHours <= 32)
                                {
                                    updateProfile = false;
                                }
                            }
                        }

                        bool updateManagementWallet = false;

                        if (currentIdentity.Version > 0 && String.IsNullOrWhiteSpace(currentIdentity.ManagementWallet))
                        {
                            var wallet = managementWallets.FirstOrDefault(w => w.Identity == currentIdentity.Identity);
                            if (wallet != null)
                            {
                                currentIdentity.ManagementWallet = wallet.TransferWallet ?? wallet.CreateWallet;
                                if (!String.IsNullOrWhiteSpace(currentIdentity.ManagementWallet))
                                {
                                    updateManagementWallet = true;
                                }
                                else
                                {
                                    Console.WriteLine("Failed to find management wallet for " + currentIdentity.Identity);
                                }
                            }
                        }

                        if (updateProfile || (currentIdentity.NodeId ?? "").Length > 40)
                        {
                            var output =
                                await profileFunction.CallDeserializingToObjectAsync<ProfileFunctionOutput>(
                                    currentIdentity.Identity);

                            var stake = Web3.Convert.FromWei(output.stake);
                            var stakeReserved = Web3.Convert.FromWei(output.stakeReserved);
                            var nodeId = HexHelper.ByteArrayToString(output.nodeId, false).Substring(0, 40);
                            var withdrawalAmount = Web3.Convert.FromWei(output.withdrawalAmount);
                            var withdrawalTimestamp = (UInt64) output.withdrawalTimestamp;
                            var reputation = (UInt64) output.reputation;

                            if (currentIdentity.Stake != stake
                                || currentIdentity.StakeReserved != stakeReserved
                                || currentIdentity.NodeId != nodeId
                                || currentIdentity.WithdrawalAmount != withdrawalAmount
                                || currentIdentity.WithdrawalPending != output.withdrawalPending
                                || currentIdentity.WithdrawalTimestamp != withdrawalTimestamp
                                || currentIdentity.Reputation != reputation)
                            {
                                currentIdentity.Stake = stake;
                                currentIdentity.StakeReserved = stakeReserved;
                                currentIdentity.NodeId = nodeId;
                                currentIdentity.WithdrawalAmount = withdrawalAmount;
                                currentIdentity.WithdrawalPending = output.withdrawalPending;
                                currentIdentity.WithdrawalTimestamp = withdrawalTimestamp;
                                currentIdentity.Reputation = reputation;
                                currentIdentity.LastSyncedTimestamp = DateTime.Now;

                                OTIdentity.UpdateFromProfileFunction(connection, currentIdentity);
                            }
                            else
                            {
                                currentIdentity.LastSyncedTimestamp = DateTime.Now;
                                OTIdentity.UpdateLastSyncedTimestamp(connection, currentIdentity);
                            }
                        }

                        if (!paidOutBalances.TryGetValue(currentIdentity.Identity, out var paidRow))
                        {
                            paidRow = 0;
                        }

                        if (!approvedNodes.TryGetValue(currentIdentity.NodeId, out var approvedRow))
                        {
                            approvedRow = 0;
                        }

                        offerTotals.TryGetValue(currentIdentity.Identity, out var offerRow);

                        if (currentIdentity.Paidout != paidRow
                            || currentIdentity.Approved != (approvedRow != 0)
                            || currentIdentity.TotalOffers != (offerRow?.OffersTotal ?? 0)
                            || currentIdentity.OffersLast7Days != (offerRow?.OffersLast7Days ?? 0)
                            || currentIdentity.ActiveOffers != 0
                            || updateManagementWallet)
                        {
                            currentIdentity.Paidout = paidRow;
                            currentIdentity.Approved = (approvedRow != 0);
                            currentIdentity.TotalOffers = offerRow?.OffersTotal ?? 0;
                            currentIdentity.OffersLast7Days = offerRow?.OffersLast7Days ?? 0;
                            currentIdentity.ActiveOffers = 0;

                            OTIdentity.UpdateFromPaidoutAndApprovedCalculation(connection, currentIdentity);
                        }
                    }
                }

                DateTime end = DateTime.UtcNow;

                TimeSpan diff = end - start;

                CachetLogger.UpdateMetricAndComponent(8, 7, diff);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                CachetLogger.FailComponent(8);
            }
        }


        private static async Task<OTContract_Profile_IdentityCreated[]> CreateMissingIdentities(MySqlConnection connection, Web3 cl)
        {
            var allIdentitiesCreated = connection
                .Query<OTContract_Profile_IdentityCreated>(@"select * from OTContract_Profile_IdentityCreated IC
            WHERE IC.NewIdentity not in (SELECT OTIdentity.Identity FROM OTIdentity)")
                .ToArray();

            foreach (var identity in allIdentitiesCreated)
            {
                var ercContract = new Contract(eth, Constants.GetContractAbi(ContractType.ERC725), identity.NewIdentity);

                var otVersionFunction = ercContract.GetFunction("otVersion");

                var value = await otVersionFunction.CallAsync<BigInteger>();

                OTIdentity.Insert(connection, new OTIdentity
                {
                    TransactionHash = identity.TransactionHash,
                    Identity = identity.NewIdentity,
                    Version = (int) value
                });
            }

            //This only happens due to missing blockchain events (only happened in December 2018)
            var profilesCreatedWithoutIdentities = connection.Query(
                @"select TransactionHash, Profile from otcontract_profile_profilecreated
WHERE Profile not in (select otidentity.Identity from otidentity)").ToArray();

            foreach (var profilesCreatedWithoutIdentity in profilesCreatedWithoutIdentities)
            {
                string hash = profilesCreatedWithoutIdentity.TransactionHash;
                string identity = profilesCreatedWithoutIdentity.Profile;

                var ercContract = new Contract(eth, Constants.GetContractAbi(ContractType.ERC725), identity);

                var otVersionFunction = ercContract.GetFunction("otVersion");

                var value = await otVersionFunction.CallAsync<BigInteger>();

                OTIdentity.Insert(connection, new OTIdentity
                {
                    TransactionHash = hash,
                    Identity = identity,
                    Version = (int) value
                });
            }

            return allIdentitiesCreated;
        }

        public LoadIdentitiesTask() : base("Load Identities")
        {
        }
    }
}