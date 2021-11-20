﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using Nethereum.Web3;
using OTHub.BackendSync.Database.Models;
using OTHub.BackendSync.Logging;
using OTHub.Settings;
using OTHub.Settings.Abis;

namespace OTHub.BackendSync.Blockchain.Tasks.Misc.Children
{
    public class MarkOldContractsAsArchived : TaskRunBlockchain
    {
        public MarkOldContractsAsArchived() : base("Mark Old Smart Contracts as Archived")
        {
        }

        public override async Task<bool> Execute(Source source, BlockchainType blockchain, BlockchainNetwork network, IWeb3 web3, int blockchainID)
        {
            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var profiles = await OTContract.GetByTypeAndBlockchain(connection, (int)ContractTypeEnum.Profile, blockchainID);

                foreach (var otContract in profiles)
                {
                    var dates = (await connection.QueryAsync<DateTime?>(
                        @"select MAX(Timestamp) from otcontract_profile_identitycreated r
join ethblock b on r.BlockNumber = b.BlockNumber AND b.BlockchainID = r.BlockchainID
WHERE r.ContractAddress = @contract AND r.BlockchainID = @blockchainID
union all
select MAX(Timestamp) from otcontract_profile_identitytransferred r
join ethblock b on r.BlockNumber = b.BlockNumber AND b.BlockchainID = r.BlockchainID
WHERE r.ContractAddress = @contract AND r.BlockchainID = @blockchainID
union all
select MAX(Timestamp) from otcontract_profile_profilecreated r
join ethblock b on r.BlockNumber = b.BlockNumber AND b.BlockchainID = r.BlockchainID
WHERE r.ContractAddress = @contract AND r.BlockchainID = @blockchainID
union all
select MAX(Timestamp) from otcontract_profile_tokensdeposited r
join ethblock b on r.BlockNumber = b.BlockNumber AND b.BlockchainID = r.BlockchainID
WHERE r.ContractAddress = @contract AND r.BlockchainID = @blockchainID
union all
select MAX(Timestamp) from otcontract_profile_tokensreleased r
join ethblock b on r.BlockNumber = b.BlockNumber AND b.BlockchainID = r.BlockchainID
WHERE r.ContractAddress = @contract AND r.BlockchainID = @blockchainID
union all
select MAX(Timestamp) from otcontract_profile_tokensreserved r
join ethblock b on r.BlockNumber = b.BlockNumber AND b.BlockchainID = r.BlockchainID
WHERE r.ContractAddress = @contract AND r.BlockchainID = @blockchainID
union all
select MAX(Timestamp) from otcontract_profile_tokenstransferred r
join ethblock b on r.BlockNumber = b.BlockNumber AND b.BlockchainID = r.BlockchainID
WHERE r.ContractAddress = @contract AND r.BlockchainID = @blockchainID
union all
select MAX(Timestamp) from otcontract_profile_tokenswithdrawn r
join ethblock b on r.BlockNumber = b.BlockNumber AND b.BlockchainID = r.BlockchainID
WHERE r.ContractAddress = @contract AND r.BlockchainID = @blockchainID", new
                        {
                            contract = otContract.Address, blockchainID = blockchainID
                        })).Where(d => d.HasValue).Select(d => d.Value).ToArray();

                    if (dates.Any())
                    {
                        var maxDate = dates.Max();

                        if ((DateTime.Now - maxDate).TotalDays >= 30)
                        {
                            if (!otContract.IsArchived)
                            {
                                otContract.IsArchived = true;
                                await OTContract.Update(connection, otContract, false, true);
                            }
                        }
                        else
                        {
                            if (otContract.IsArchived)
                            {
                                otContract.IsArchived = false;
                                await OTContract.Update(connection, otContract, false, true);
                            }
                        }
                    }
                    else
                    {
                        if (!otContract.IsArchived)
                        {
                            otContract.IsArchived = true;
                            await OTContract.Update(connection, otContract, false, true);
                        }
                    }
                }

                profiles = await OTContract.GetByTypeAndBlockchain(connection, (int)ContractTypeEnum.Holding, blockchainID);

                foreach (var otContract in profiles)
                {
                    var dates = (await connection.QueryAsync<DateTime?>(@"select MAX(Timestamp) from otcontract_holding_offertask r
join ethblock b on r.BlockNumber = b.BlockNumber AND r.BlockchainID = b.BlockchainID
WHERE r.ContractAddress = @contract AND b.BlockchainID = r.BlockchainID
union all
select MAX(Timestamp) from otcontract_holding_ownershiptransferred r
join ethblock b on r.BlockNumber = b.BlockNumber AND b.BlockchainID = r.BlockchainID
WHERE r.ContractAddress = @contract AND r.BlockchainID = @blockchainID
union all
select MAX(r.Timestamp) from otcontract_holding_paidout r
join ethblock b on r.BlockNumber = b.BlockNumber AND b.BlockchainID = r.BlockchainID
WHERE r.ContractAddress = @contract AND r.BlockchainID = @blockchainID", new { contract = otContract.Address, blockchainID = blockchainID })).Where(d => d.HasValue).Select(d => d.Value).ToArray();

                    if (dates.Any())
                    {
                        var maxDate = dates.Max();

                        if ((DateTime.Now - maxDate).TotalDays >= 30)
                        {
                            if (!otContract.IsArchived)
                            {
                                otContract.IsArchived = true;
                                await OTContract.Update(connection, otContract, false, true);
                            }
                        }
                        else
                        {
                            if (otContract.IsArchived)
                            {
                                otContract.IsArchived = false;
                                await OTContract.Update(connection, otContract, false, true);
                            }
                        }
                    }
                    else
                    {
                        if (!otContract.IsArchived)
                        {
                            otContract.IsArchived = true;
                            await OTContract.Update(connection, otContract, false, true);
                        }
                    }
                }
            }

            return true;
        }
    }
}