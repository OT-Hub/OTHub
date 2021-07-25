using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MySqlConnector;
using OTHub.APIServer.Sql;
using OTHub.APIServer.Sql.Models.Nodes;
using OTHub.APIServer.Sql.Models.Nodes.DataCreator;
using OTHub.Settings;
using OTHub.Settings.Constants;
using Swashbuckle.AspNetCore.Annotations;

namespace OTHub.APIServer.Controllers
{
    [Route("api/[controller]")]
    public class BadgeController : Controller
    {
        private readonly IMemoryCache _cache;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public BadgeController(IMemoryCache cache)
        {
            _cache = cache;
        }

        [Route("")]
        [HttpGet]
        [SwaggerResponse(200, type: typeof(BadgeModel[]))]
        public async Task<BadgeModel> Get()
        {
            const string key = "MenuBadges";

            if (_cache.TryGetValue(key, out var val))
            {
                return (BadgeModel) val;
            }

            await _semaphore.WaitAsync();

            try
            {
                if (_cache.TryGetValue(key, out val))
                {
                    return (BadgeModel)val;
                }

                await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                {
                    var badges = await connection.QueryFirstOrDefaultAsync<BadgeModel>(@"SELECT 
(SELECT COUNT(*) FROM otoffer WHERE IsFinalized = 1) AS TotalJobs,
(SELECT COUNT(DISTINCT NodeId) FROM otidentity WHERE VERSION = 1) AS DataHolders,
(SELECT COUNT(DISTINCT DcNodeID) FROM otcontract_holding_offercreated) AS DataCreators");

                    var status = (await connection.QueryAsync<StatusModel>(@"SELECT 
s.Name, s.Success, b.DisplayName BlockchainName, s.ParentName 
FROM systemstatus s
LEFT JOIN blockchains b ON b.ID = s.BlockchainID
WHERE s.Success = 0
ORDER BY s.ParentName, b.id, s.Name")).ToArray();

                    List<string> errors = new List<string>();

                    foreach (StatusModel statusModel in status)
                    {
                        switch (statusModel.ParentName)
                        {
                            case TaskNames.BlockchainSync:
                                switch (statusModel.Name)
                                {
                                    case TaskNames.HoldingContractSync:
                                        errors.Add($"New jobs on {statusModel.BlockchainName} may be delayed or missing due to an error.");
                                        errors.Add($"New payouts on {statusModel.BlockchainName} may be delayed or missing due to an error.");
                                        break;
                                    case TaskNames.LitigationContractSync:
                                        errors.Add($"New litigations on {statusModel.BlockchainName} may be delayed or missing due to an error.");
                                        break;
                                    case TaskNames.ProfileContractSync:
                                        errors.Add($"New nodes that have been created on {statusModel.BlockchainName} may be delayed or missing due to an error.");
                                        errors.Add($"New token deposits on {statusModel.BlockchainName} may be delayed or missing due to an error.");
                                        errors.Add($"New token withdrawals on {statusModel.BlockchainName} may be delayed or missing due to an error.");
                                        break;
                                    case TaskNames.LoadProfileBalances:
                                        errors.Add($"Node staked tokens on {statusModel.BlockchainName} may be out of date due to an error.");
                                        errors.Add($"Node locked tokens on {statusModel.BlockchainName} may be out of date due to an error.");
                                        errors.Add($"Node statistics for Data Holders, Data Creators and My Nodes on {statusModel.BlockchainName} may be out of date due to an error.");
                                        break;
                                    case TaskNames.ProcessJobs:
                                        errors.Add($"New jobs on {statusModel.BlockchainName} may be delayed or missing due to an error.");
                                        break;
                                }
                                break;
                            case TaskNames.BlockchainMaintenance:
                                switch (statusModel.Name)
                                {
                                    case TaskNames.GetLatestContracts:
                                        errors.Add($"New smart contract changes on {statusModel.BlockchainName} may be delayed or missing due to an error.");
                                        break;
                                }

                                break;
                            case TaskNames.Misc:
                                switch (statusModel.Name)
                                {
                                    case TaskNames.UpdateJobHistoryChartData:
                                        errors.Add($"New jobs may be missing from the jobs page charts due to an error.");
                                        break;
                                    case TaskNames.GetMarketData:
                                        errors.Add($"Market ticker prices for ETH, USD and TRAC may be out of date due to an error.");
                                        break;
                                    case TaskNames.UpdateStakedTokenReport:
                                        errors.Add($"The staked tokens report may be out of date due to an error.");
                                        break;
                                }
                                break;
                        }
                    }

                    badges.LiveOutages = errors.ToArray();

#if !DEBUG
                    _cache.Set(key, badges, TimeSpan.FromMinutes(1));
#endif

                    return badges;

                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    public class StatusModel
    {
        public string Name { get; set; }
        public bool Success { get; set; }
        public string BlockchainName { get; set; }
        public string ParentName { get; set; }
    }

    public class BadgeModel
    {
        public int TotalJobs { get; set; }
        public int DataHolders { get; set; }
        public int DataCreators { get; set; }
        public string[] LiveOutages { get; set; }
    }
}