using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComposableAsync;
using Dapper;
using MySqlConnector;
using Nethereum.ABI;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC;
using Nethereum.Web3;
using Org.BouncyCastle.Crypto.Digests;
using OTHub.BackendSync.Database.Models;
using OTHub.BackendSync.Logging;
using OTHub.Settings;
using OTHub.Settings.Abis;
using RateLimiter;

namespace OTHub.BackendSync.Blockchain.Tasks.Tools
{
    public class ToolsTask : TaskRunBlockchain
    {
        public ToolsTask() : base("Find Nodes By Wallet")
        {
            CountByIntervalAwaitableConstraint constraint = new CountByIntervalAwaitableConstraint(4, TimeSpan.FromSeconds(1));


            CountByIntervalAwaitableConstraint constraint2 = new CountByIntervalAwaitableConstraint(1, TimeSpan.FromMilliseconds(200));

            TimeConstraint = TimeLimiter.Compose(constraint, constraint2);
        }

        public override async Task<bool> Execute(Source source, BlockchainType blockchain, BlockchainNetwork network)
        {
            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                int blockchainID = await GetBlockchainID(connection, blockchain, network);

                var pendingJobs = (await connection.QueryAsync(
                    @"SELECT * FROM findnodesbywalletjob WHERE EndDate is null AND BlockchainID = @blockchainID ORDER BY StartDate",
                    new
                    {
                        blockchainID
                    })).ToArray();

                foreach (var pendingJob in pendingJobs)
                {
                    OTIdentity[] identities = await OTIdentity.GetAll(connection, blockchainID);
                    uint id = pendingJob.ID;
                    string address = pendingJob.Address;
                    string userID = pendingJob.UserID;

                    Logger.WriteLine(source,
                        "Finding wallets for address " + address + " and user id " + userID + " on blockchain " +
                        blockchain);

                    try
                    {
                        await ProcessJob(connection, blockchainID, identities, address, blockchain, network, source, id);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLine(source, ex.ToString());
                        await connection.ExecuteAsync(@"UPDATE findnodesbywalletjob SET Failed = 1, EndDate = @endDate   WHERE ID = @id", new
                        {
                            id = id,
                            endDate = DateTime.UtcNow
                        });
                        return false;
                    }
                }
            }

            return true;
        }

        private TimeLimiter TimeConstraint { get; }

        private async Task ProcessJob(MySqlConnection connection, int blockchainID, OTIdentity[] identities,
            string address, BlockchainType blockchain, BlockchainNetwork network, Source source, uint id)
        {
            string abi = AbiHelper.GetContractAbi(ContractTypeEnum.ERC725, blockchain, network);

            string nodeUrl = await connection.ExecuteScalarAsync<string>(@"SELECT BlockchainNodeUrl FROM blockchains WHERE id = @id", new
            {
                id = blockchainID
            });

            var cl = new Web3(nodeUrl);

            var eth = new EthApiService(cl.Client);

            Int32 percentage = 0;
            int counter = 0;
            foreach (OTIdentity identity in identities)
            {
                counter++;
                try
                {
                    int loopPercentage = (int)Math.Round((decimal)counter * 100 / identities.Count(), MidpointRounding.AwayFromZero);
                    if (loopPercentage != percentage)
                    {
                        percentage = loopPercentage;

                        await connection.ExecuteAsync(@"UPDATE findnodesbywalletjob SET Progress = @percentage WHERE ID = @id", new
                        {
                            id = id,
                            percentage
                        });
                    }

                    var ercContract = new Contract(eth, abi, identity.Identity);

                    Function keyHasPurposeFunction = ercContract.GetFunction("keyHasPurpose");

                    var abiEncode = new ABIEncode();
                    byte[] data = abiEncode.GetABIEncodedPacked(address.HexToByteArray());

                    byte[] bytes = CalculateHash(data);

                    await TimeConstraint;
                    bool hasPermission = await keyHasPurposeFunction.CallAsync<bool>(bytes, 1);

                    if (hasPermission)
                    {
                        await connection.ExecuteAsync(@"INSERT INTO findnodesbywalletresult(JobID, Identity)
VALUES(@jobID, @identity)", new
                        {
                            jobID = id,
                            identity = identity.Identity
                        });
                    }
                }
                catch (Exception ex)
                {
                    throw;
                }
            }

            await connection.ExecuteAsync(@"UPDATE findnodesbywalletjob SET Progress = 100, EndDate = @endDate WHERE ID = @id", new
            {
                id = id,
                endDate = DateTime.UtcNow
            });
        }

        private static byte[] CalculateHash(byte[] value)
        {
            var digest = new KeccakDigest(256);
            var output = new byte[digest.GetDigestSize()];
            digest.BlockUpdate(value, 0, value.Length);
            digest.DoFinal(output, 0);
            return output;
        }

        public override TimeSpan GetExecutingInterval(BlockchainType type)
        {
            return TimeSpan.FromSeconds(45);
        }
    }
}