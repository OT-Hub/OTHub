using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;
using OTHub.BackendSync.Database.Models;
using OTHub.BackendSync.Logging;
using OTHub.Settings;
using OTHub.Settings.Abis;

namespace OTHub.BackendSync.Ethereum.Tasks
{
    public class MarkOldContractsAsArchived : TaskRun
    {
        public MarkOldContractsAsArchived() : base("Mark Old Smart Contracts as Archived")
        {
        }

        public override async Task Execute(Source source)
        {
            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var profiles = OTContract.GetByType(connection, (int)ContractTypeEnum.Profile);

                foreach (var otContract in profiles)
                {
                    var dates = connection.Query<DateTime?>(@"select MAX(Timestamp) from otcontract_profile_identitycreated r
join ethblock b on r.BlockNumber = b.BlockNumber
WHERE r.ContractAddress = @contract
union all
select MAX(Timestamp) from otcontract_profile_identitytransferred r
join ethblock b on r.BlockNumber = b.BlockNumber
WHERE r.ContractAddress = @contract
union all
select MAX(Timestamp) from otcontract_profile_profilecreated r
join ethblock b on r.BlockNumber = b.BlockNumber
WHERE r.ContractAddress = @contract
union all
select MAX(Timestamp) from otcontract_profile_tokensdeposited r
join ethblock b on r.BlockNumber = b.BlockNumber
WHERE r.ContractAddress = @contract
union all
select MAX(Timestamp) from otcontract_profile_tokensreleased r
join ethblock b on r.BlockNumber = b.BlockNumber
WHERE r.ContractAddress = @contract
union all
select MAX(Timestamp) from otcontract_profile_tokensreserved r
join ethblock b on r.BlockNumber = b.BlockNumber
WHERE r.ContractAddress = @contract
union all
select MAX(Timestamp) from otcontract_profile_tokenstransferred r
join ethblock b on r.BlockNumber = b.BlockNumber
WHERE r.ContractAddress = @contract
union all
select MAX(Timestamp) from otcontract_profile_tokenswithdrawn r
join ethblock b on r.BlockNumber = b.BlockNumber
WHERE r.ContractAddress = @contract", new {contract = otContract.Address}).Where(d => d.HasValue).Select(d => d.Value).ToArray();

                    if (dates.Any())
                    {
                        var maxDate = dates.Max();

                        if ((DateTime.Now - maxDate).TotalDays >= 30)
                        {
                            if (!otContract.IsArchived)
                            {
                                otContract.IsArchived = true;
                                OTContract.Update(connection, otContract, false, true);
                            }
                        }
                        else
                        {
                            if (otContract.IsArchived)
                            {
                                otContract.IsArchived = false;
                                OTContract.Update(connection, otContract, false, true);
                            }
                        }
                    }
                    else
                    {
                        if (!otContract.IsArchived)
                        {
                            otContract.IsArchived = true;
                            OTContract.Update(connection, otContract, false, true);
                        }
                    }
                }

                profiles = OTContract.GetByType(connection, (int)ContractTypeEnum.Holding);

                foreach (var otContract in profiles)
                {
                    var dates = connection.Query<DateTime?>(@"select MAX(Timestamp) from otcontract_holding_offertask r
join ethblock b on r.BlockNumber = b.BlockNumber
WHERE r.ContractAddress = @contract
union all
select MAX(Timestamp) from otcontract_holding_ownershiptransferred r
join ethblock b on r.BlockNumber = b.BlockNumber
WHERE r.ContractAddress = @contract
union all
select MAX(r.Timestamp) from otcontract_holding_paidout r
join ethblock b on r.BlockNumber = b.BlockNumber
WHERE r.ContractAddress = @contract", new { contract = otContract.Address }).Where(d => d.HasValue).Select(d => d.Value).ToArray();

                    if (dates.Any())
                    {
                        var maxDate = dates.Max();

                        if ((DateTime.Now - maxDate).TotalDays >= 30)
                        {
                            if (!otContract.IsArchived)
                            {
                                otContract.IsArchived = true;
                                OTContract.Update(connection, otContract, false, true);
                            }
                        }
                        else
                        {
                            if (otContract.IsArchived)
                            {
                                otContract.IsArchived = false;
                                OTContract.Update(connection, otContract, false, true);
                            }
                        }
                    }
                    else
                    {
                        if (!otContract.IsArchived)
                        {
                            otContract.IsArchived = true;
                            OTContract.Update(connection, otContract, false, true);
                        }
                    }
                }
            }
        }
    }
}