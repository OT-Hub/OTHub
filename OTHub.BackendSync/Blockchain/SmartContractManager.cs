using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using OTHub.Settings;
using OTHub.Settings.Abis;

namespace OTHub.BackendSync.Blockchain
{
    public static class SmartContractManager
    {
        private static ConcurrentDictionary<int, ConcurrentDictionary<string, ContractTypeEnum>> _dictionary = new ConcurrentDictionary<int, ConcurrentDictionary<string, ContractTypeEnum>>();

        public static async Task Load()
        {
            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var rows = (await connection.QueryAsync(@"SELECT BlockchainID, Address, Type from otcontract WHERE IsLatest = 1"))
                    .ToArray();

                foreach (var row in rows)
                {
                    int blockchainID = row.BlockchainID;
                    string address = row.Address;
                    int type = row.Type;

                    if (!_dictionary.TryGetValue(blockchainID, out var blockchainDict))
                    {
                        blockchainDict = new ConcurrentDictionary<string,ContractTypeEnum>();
                        _dictionary[blockchainID] = blockchainDict;
                    }

                    blockchainDict[address] = (ContractTypeEnum)type;
                }
            }
        }

        public static bool TryGetAddress(int blockchainID, string address, out ContractTypeEnum type)
        {
            if (_dictionary.TryGetValue(blockchainID, out var blockchainDictionary))
            {
                if (blockchainDictionary.TryGetValue(address, out type))
                {
                    return true;
                }
            }

            type = ContractTypeEnum.Token;

            return false;
        }
    }
}