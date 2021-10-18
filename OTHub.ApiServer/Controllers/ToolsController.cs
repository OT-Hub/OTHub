using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using OTHub.Settings;

namespace OTHub.APIServer.Controllers
{
    [Route("api/[controller]")]
    public class ToolsController : Controller
    {
        [HttpGet]
        [Route("GetFindNodesByWalletJobs")]
        [Authorize]
        public async Task<FindNodesByWalletJob[]> GetFindNodesByWalletJobs()
        {
            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                FindNodesByWalletJob[] jobs = (await connection.QueryAsync<FindNodesByWalletJob>(
                    @"SELECT j.ID, bc.DisplayName AS BlockchainName, j.Address, j.StartDate, j.EndDate, j.Progress, j.Failed FROM findnodesbywalletjob j
JOIN blockchains bc ON bc.id = j.BlockchainID
WHERE j.UserID = @userID", new
                    {
                        userID = User?.Identity.Name
                    })).ToArray();

                foreach (FindNodesByWalletJob findNodesByWalletJob in jobs)
                {
                    findNodesByWalletJob.Identities = (await connection.QueryAsync<MatchIdentity>(@"SELECT i.Identity, i.Stake as Tokens, i.NodeId, mn.DisplayName FROM findnodesbywalletresult r
JOIN findnodesbywalletjob j ON j.ID = r.JobID
JOIN otidentity i ON i.BlockchainID = j.BlockchainID AND i.Identity = r.Identity
LEFT JOIN mynodes mn ON mn.NodeID = i.NodeId AND mn.UserID = @userID
WHERE j.ID = @id", new
                    {
                        userID = User?.Identity.Name,
                        id = findNodesByWalletJob.ID
                    })).ToArray();
                }

                return jobs;
            }
        }

        [HttpPost]
        [Route("FindNodesByWallet")]
        [Authorize]
        public async Task<FindNodesByWalletJobResult> FindNodesByWallet([FromQuery]int blockchainID, [FromQuery]string address)
        {   
            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var runningJobs = (await connection.QueryAsync(@"SELECT * FROM findnodesbywalletjob WHERE EndDate is null AND UserID = @userID AND Address = @address AND BlockchainID = @blockchainID ORDER BY StartDate DESC", 
                    new
                {
                    userID = User?.Identity.Name,
                    address,
                    blockchainID
                })).ToArray();

                foreach (var job in runningJobs)
                {
                    DateTime startDate = job.StartDate;
                    DateTime? endDate = job.EndDate;
                    Boolean? failed = job.Failed;

                    if (!endDate.HasValue)
                    {
                        return new FindNodesByWalletJobResult() {IsError = true, Message = "There is already a search queued for this address."};
                    }

                    if ((DateTime.UtcNow - startDate).TotalDays <= 7 && failed != true)
                    {
                        return new FindNodesByWalletJobResult() { IsError = true, Message = "You can only perform this search once a week per address." };
                    }
                }

                await connection.ExecuteAsync(
                    @"INSERT INTO findnodesbywalletjob(UserID, BlockchainID, Address, StartDate, Progress) 
VALUES (@userID, @blockchainID, @address, @startDate, 0)", new
                    {
                        userID = User?.Identity.Name,
                        address,
                        blockchainID,
                        startDate = DateTime.UtcNow
                    });

                return new FindNodesByWalletJobResult(){Message = "Your search has been added to the queue."};
            }
        }
    }

    public class FindNodesByWalletJob
    {
        public int ID { get; set; }
        public string BlockchainName { get; set; }
        public string Address { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Progress { get; set; }
        public bool? Failed { get; set; }
        public MatchIdentity[] Identities { get; set; }
    }

    public class MatchIdentity
    {
        public string Tokens { get; set; }
        public string Identity { get; set; }
        public string NodeID { get; set; }
        public string DisplayName { get; set; }
    }

    public class FindNodesByWalletJobResult
    {
        public bool IsError { get; set; }
        public string Message { get; set; }
    }
}