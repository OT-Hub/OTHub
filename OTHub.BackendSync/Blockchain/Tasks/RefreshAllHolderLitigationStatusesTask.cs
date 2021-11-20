using System.Threading.Tasks;
using MySqlConnector;
using Nethereum.Web3;
using OTHub.BackendSync.Database.Models;
using OTHub.BackendSync.Logging;
using OTHub.Settings;

namespace OTHub.BackendSync.Blockchain.Tasks
{
    public class RefreshAllHolderLitigationStatusesTask : TaskRunBlockchain
    {
        public RefreshAllHolderLitigationStatusesTask() : base("Refresh All Holder Litigation Statuses")
        {
        }

        public override async Task<bool> Execute(Source source, BlockchainType blockchain, BlockchainNetwork network, IWeb3 web3, int blockchainID)
        {
            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                await OTOfferHolder.UpdateLitigationForAllOffers(connection, blockchainID);
            }

            return true;
        }
    }
}