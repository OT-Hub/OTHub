using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using OTHub.Settings;
using Swashbuckle.AspNetCore.Annotations;

namespace OTHub.APIServer.Controllers
{
    [Route("api/[controller]")]
    public class RpcController : Controller
    {
        [HttpGet]
        public async Task<RPCModel[]> Get()
        {
            await using (MySqlConnection connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var data = await connection.QueryAsync<RPCModel>(@"SELECT r.`Name`, r.LatestBlockNumber BlockNumber, r.Weight, b.DisplayName BlockchainName,
SUM(CASE WHEN h.Timestamp >= DATE_Add(NOW(), INTERVAL -1 DAY) THEN 1 ELSE 0 END) DailyRequestsTotal,
SUM(CASE WHEN h.Timestamp >= DATE_Add(NOW(), INTERVAL -1 MONTH) THEN 1 ELSE 0 END) MonthlyRequestsTotal,
SUM(CASE WHEN h.Success AND h.Timestamp >= DATE_Add(NOW(), INTERVAL -1 DAY) THEN 1 ELSE 0 END) DailySuccessTotal,
SUM(CASE WHEN h.Success AND h.Timestamp >= DATE_Add(NOW(), INTERVAL -1 MONTH) THEN 1 ELSE 0 END) MonthlySuccessTotal
FROM rpcs r
JOIN blockchains b ON b.ID = r.BlockchainID
LEFT JOIN rpcshistory h ON h.RPCID = r.ID
WHERE r.EnabledByUser = 1
GROUP BY r.`Name`, r.LatestBlockNumber, r.Weight, b.DisplayName
ORDER BY b.ID, r.ID");

                return data.ToArray();
            }
        }
    }

    public class RPCModel
    {
        public string Name { get; set; }
        public UInt64 BlockNumber { get; set; }
        public int Weight { get; set; }
        public string BlockchainName { get; set; }

        public long DailyRequestsTotal { get; set; }
        public long MonthlyRequestsTotal { get; set; }
        public int DailySuccessTotal { get; set; }
        public int MonthlySuccessTotal { get; set; }
    }
}
