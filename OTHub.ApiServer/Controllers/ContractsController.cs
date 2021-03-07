using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using OTHub.APIServer.Sql.Models;
using OTHub.APIServer.Sql.Models.Contracts;
using OTHub.Settings;
using Swashbuckle.AspNetCore.Annotations;

namespace OTHub.APIServer.Controllers
{
    [Route("api/[controller]")]
    public class ContractsController : Controller
    {
        [Route("GetAllContracts")]
        [HttpGet]
        public async Task<CompactContractModelGroup[]> GetAllContracts()
        {
            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                CompactContractModel[] items = (await connection.QueryAsync<CompactContractModel>(@"SELECT b.HubAddress, b.DisplayName BlockchainDisplayName, c.Address, c.`Type`, c.IsLatest, c.FromBlockNumber, c.IsArchived, c.SyncBlockNumber, c.LastSyncedTimestamp FROM otcontract c
JOIN blockchains b ON b.id = c.BlockchainID
ORDER BY b.ID, c.IsLatest desc, c.`Type`")).ToArray();

                CompactContractModelGroup[] groups = items.GroupBy(i => i.BlockchainDisplayName)
                    .Select(g => new CompactContractModelGroup
                        {Name = g.Key, Items = g.ToList(), HubAddress = g.First().HubAddress}).ToArray();

                return groups;
            }
        }

        [Route("GetHoldingAddress")]
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the latest holding smart contract address known to OT Hub.",
            Description = @"Please note that when updates are released to the ODN the holding smart contract address may not be updated for a few hours.

This holding smart contract is responsible for the movement and storage of reserved tokens (like escrow). It also handles the payouts for a data holder."
        )]
        [SwaggerResponse(200, type: typeof(ContractAddress))]
        [SwaggerResponse(500, "Internal server error")]
        public ContractAddress GetHoldingAddress()
        {
            using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                return connection.QuerySingle<ContractAddress>(@"select Address, IsLatest from otcontract
where Type = 6 AND IsLatest = 1 AND IsArchived = 0");
            }
        }

        [Route("GetHoldingAddresses")]
        [HttpGet]
        [SwaggerResponse(200, type: typeof(ContractAddress[]))]
        [SwaggerResponse(500, "Internal server error")]
        public ContractAddress[] GetHoldingAddresses()
        {
            using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                return connection.Query<ContractAddress>(@"select Address, IsLatest from otcontract
where Type = 6").ToArray();
            }
        }

        [Route("GetHoldingStorageAddresses")]
        [HttpGet]
        [SwaggerResponse(200, type: typeof(ContractAddress[]))]
        [SwaggerResponse(500, "Internal server error")]
        public ContractAddress[] GetHoldingStorageAddresses()
        {
            using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                return connection.Query<ContractAddress>(@"select Address, IsLatest from otcontract
where Type = 5").ToArray();
            }
        }

        [Route("GetLitigationStorageAddresses")]
        [HttpGet]
        [SwaggerResponse(200, type: typeof(ContractAddress[]))]
        [SwaggerResponse(500, "Internal server error")]
        public ContractAddress[] GetLitigationStorageAddresses()
        {
            using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                return connection.Query<ContractAddress>(@"select Address, IsLatest from otcontract
where Type = 9").ToArray();
            }
        }
    }
}