﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using Nethereum.Contracts;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC;
using Nethereum.Web3;
using OTHub.BackendSync.Blockchain.Models;
using OTHub.BackendSync.Blockchain.Web3Helper;
using OTHub.BackendSync.Database.Models;
using OTHub.BackendSync.Logging;
using OTHub.Settings;
using OTHub.Settings.Abis;
using OTHub.Settings.Constants;

namespace OTHub.BackendSync.Blockchain.Tasks.BlockchainSync.Children
{
    public class LoadProfileBalancesTask : TaskRunBlockchain
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

        public override async Task<bool> Execute(Source source, BlockchainType blockchain, BlockchainNetwork network, IWeb3 web3, int blockchainID)
        {
            ClientBase.ConnectionTimeout = new TimeSpan(0, 0, 5, 0);

            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var profileStorageContractAddress = (await OTContract
                    .GetByTypeAndBlockchain(connection, (int)ContractTypeEnum.ProfileStorage, blockchainID)).SingleOrDefault(a => a.IsLatest);

                if (profileStorageContractAddress == null)
                    return false;

                var profileStorageContract =
                    new Contract(new EthApiService(web3.Client),
                        AbiHelper.GetContractAbi(ContractTypeEnum.ProfileStorage, blockchain, network),
                        profileStorageContractAddress.Address);
                var profileFunction = profileStorageContract.GetFunction("profile");

                var currentIdentities = await OTIdentity.GetAll(connection, blockchainID);

                Dictionary<string, decimal> paidOutBalances = (await connection
                    .QueryAsync<PayoutGroupHolder>(
                        @"SELECT Holder, SUM(Amount) Amount FROM OTContract_Holding_Paidout WHERE BlockchainID = @blockchainID GROUP BY Holder",
                        new
                        {
                            blockchainID = blockchainID
                        }))
                    .ToDictionary(k => k.Holder, k => k.Amount);

                Dictionary<string, OfferGroupHolder> offerTotals = (await connection.QueryAsync<OfferGroupHolder>(
                    @"select i.Identity, COUNT(o.OfferID) as OffersTotal,
(SELECT count(so.OfferID) FROM otoffer_holders sh join otoffer so on so.OfferID = sh.OfferID AND so.BlockchainID = sh.BlockchainID
    WHERE sh.blockchainid = @blockchainID and sh.Holder = i.Identity AND so.CreatedTimestamp >= Date_Add(NOW(), INTERVAL -7 DAY)) as OffersLast7Days
 from otidentity i
join otoffer_holders h on h.Holder = i.Identity AND h.BlockchainID = i.BlockchainID
join otoffer o on o.OfferID = h.OfferID AND o.BlockchainID = h.BlockchainID
WHERE i.blockchainid = @blockchainID
GROUP BY i.Identity", new
                    {
                        blockchainID
                    })).ToDictionary(k => k.Identity, k => k);
//                NodeManagementWallet[] managementWallets = (await connection.QueryAsync<NodeManagementWallet>(
//                    @"SELECT I.Identity, PC.ManagementWallet as CreateWallet, IT.ManagementWallet TransferWallet FROM OTIdentity I
//LEFT JOIN OTContract_Profile_ProfileCreated PC ON PC.Profile = I.Identity AND PC.BlockchainID = I.BlockchainID
//LEFT JOIN OTContract_Profile_IdentityTransferred IT ON IT.NewIdentity = I.Identity AND IT.BlockchainID = I.BlockchainID
//WHERE I.Version > 0 AND I.BlockchainID = @blockchainID", new
//                    {
//                        blockchainID
//                    })).ToArray();


                foreach (OTIdentity currentIdentity in currentIdentities)
                {
                    bool updateProfile = true;
                    DateTime? lastActivityDate = null;

                    if (currentIdentity.Version != Constants.CurrentERCVersion)
                    {
                        if (currentIdentity.LastSyncedTimestamp.HasValue)
                        {
                            DateTime adjustedNowTime = DateTime.Now;

                            if ((adjustedNowTime - currentIdentity.LastSyncedTimestamp.Value).TotalDays <= 14)
                                updateProfile = false;
                        }
                    }
                    else if (currentIdentity.LastSyncedTimestamp.HasValue)
                    {
                        var dates = (await connection.QueryAsync<DateTime?>(@"
select MAX(Timestamp) from otcontract_profile_identitycreated r
join ethblock b on r.BlockNumber = b.BlockNumber AND r.BlockchainID = b.BlockchainID
WHERE r.NewIdentity = @identity AND r.blockchainID = @blockchainID
union
select MAX(Timestamp) from otcontract_profile_identitytransferred r
join ethblock b on r.BlockNumber = b.BlockNumber AND r.BlockchainID = b.BlockchainID
WHERE r.NewIdentity = @identity AND r.blockchainID = @blockchainID
union
select MAX(Timestamp) from otcontract_profile_profilecreated r
join ethblock b on r.BlockNumber = b.BlockNumber AND r.BlockchainID = b.BlockchainID
WHERE r.Profile = @identity AND r.blockchainID = @blockchainID
union
select MAX(Timestamp) from otcontract_profile_tokensdeposited r
join ethblock b on r.BlockNumber = b.BlockNumber AND r.BlockchainID = b.BlockchainID
WHERE r.Profile = @identity AND r.blockchainID = @blockchainID
union
select MAX(Timestamp) from otcontract_profile_tokensreleased r
join ethblock b on r.BlockNumber = b.BlockNumber AND r.BlockchainID = b.BlockchainID
WHERE r.Profile = @identity AND r.blockchainID = @blockchainID
union
select MAX(Timestamp) from otcontract_profile_tokensreserved r
join ethblock b on r.BlockNumber = b.BlockNumber AND r.BlockchainID = b.BlockchainID
WHERE r.Profile = @identity AND r.blockchainID = @blockchainID
union
select MAX(Timestamp) from otcontract_profile_tokenstransferred r
join ethblock b on r.BlockNumber = b.BlockNumber AND r.BlockchainID = b.BlockchainID
WHERE (r.Sender = @identity OR r.Receiver = @identity) AND r.blockchainID = @blockchainID
union
select MAX(Timestamp) from otcontract_profile_tokenswithdrawn r
join ethblock b on r.BlockNumber = b.BlockNumber AND r.BlockchainID = b.BlockchainID
WHERE r.Profile = @identity AND r.blockchainID = @blockchainID
union
select MAX(b.Timestamp) from otoffer_holders h
join otoffer o on o.offerid = h.offerid and o.BlockchainID = h.BlockchainID
join ethblock b on b.blocknumber = o.finalizedblocknumber AND h.BlockchainID = b.BlockchainID
where h.Holder = @identity  AND h.blockchainID = @blockchainID
union
SELECT MAX(Timestamp)
FROM otcontract_litigation_litigationcompleted lc
WHERE lc.HolderIdentity = @identity AND lc.DHWasPenalized = 1 AND lc.blockchainID = @blockchainID
union
select MAX(b.Timestamp) from otcontract_holding_paidout p
join ethblock b on b.blocknumber = p.blocknumber and b.BlockchainID = p.BlockchainID
where p.holder = @identity AND p.blockchainID = @blockchainID
union
select MAX(b.Timestamp)  from otcontract_holding_offerfinalized of
join otcontract_holding_offercreated oc on oc.OfferId = of.OfferId and oc.BlockchainID = of.BlockchainID
join OTIdentity i on i.NodeId = oc.DCNodeId and i.BlockchainID = oc.BlockchainID
join ethblock b on of.BlockNumber = b.BlockNumber and b.BlockchainID = of.BlockchainID
where i.Identity = @identity AND of.blockchainID = @blockchainID", new
                        {
                            identity = currentIdentity.Identity,
                            blockchainID
                        })).ToArray().Where(d => d.HasValue).Select(d => d.Value).ToArray();



                        var adjustedNowTime = DateTime.Now;

                  

                        if (dates.Any())
                        {
                            lastActivityDate = dates.Max();

                            if (lastActivityDate.Value <= currentIdentity.LastSyncedTimestamp)
                            {
                                if ((adjustedNowTime - currentIdentity.LastSyncedTimestamp.Value).TotalHours <= 16)
                                {
                                    updateProfile = false;
                                }
                            }
                        }
                        else
                        {
                            if ((adjustedNowTime - currentIdentity.LastSyncedTimestamp.Value).TotalHours <= 24)
                            {
                                updateProfile = false;
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
                        var withdrawalTimestamp = (UInt64)output.withdrawalTimestamp;
                        var reputation = (UInt64)output.reputation;

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

                            await OTIdentity.UpdateFromProfileFunction(connection, currentIdentity);
                        }
                        else
                        {
                            currentIdentity.LastSyncedTimestamp = DateTime.Now;
                            await OTIdentity.UpdateLastSyncedTimestamp(connection, currentIdentity);
                        }
                    }

                    if (!paidOutBalances.TryGetValue(currentIdentity.Identity, out var paidRow))
                    {
                        paidRow = 0;
                    }

                    offerTotals.TryGetValue(currentIdentity.Identity, out var offerRow);

                    if (currentIdentity.Paidout != paidRow
                        || currentIdentity.TotalOffers != (offerRow?.OffersTotal ?? 0)
                        || currentIdentity.OffersLast7Days != (offerRow?.OffersLast7Days ?? 0)
                        || currentIdentity.ActiveOffers != 0
                        || (currentIdentity.LastActivityTimestamp != lastActivityDate && lastActivityDate.HasValue))
                    {
                        currentIdentity.Paidout = paidRow;
                        currentIdentity.TotalOffers = offerRow?.OffersTotal ?? 0;
                        currentIdentity.OffersLast7Days = offerRow?.OffersLast7Days ?? 0;
                        currentIdentity.ActiveOffers = 0;
                        currentIdentity.LastActivityTimestamp = lastActivityDate;

                        await OTIdentity.UpdateFromPaidoutAndApprovedCalculation(connection, currentIdentity);
                    }
                }
            }

            return true;
        }




        public LoadProfileBalancesTask() : base(TaskNames.LoadProfileBalances)
        {
        }
    }
}