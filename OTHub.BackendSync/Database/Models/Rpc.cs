using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;

namespace OTHub.BackendSync.Database.Models
{
    public class Rpc
    {
        public int ID { get; set; }

        public string Name { get; set; }
        public string Url { get; set; }
        public int BlockchainID { get; set; }
        public int Weight { get; set; }
        public UInt64 LatestBlockNumber { get; set; }
        public string OwnedByUserID { get; set; }
        public bool EnabledByUser { get; set; }

        public static async Task<Rpc[]> GetByBlockchainID(MySqlConnection connection, int blockchainID)
        {
            IEnumerable<Rpc> rpcs = await connection.QueryAsync<Rpc>(@"SELECT * FROM rpcs where blockchainID = @blockchainID AND EnabledByUser = 1", new
            {
                blockchainID
            });

            return rpcs.ToArray();
        }
    }
}