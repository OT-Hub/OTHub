using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using OTHub.BackendSync.Logging;
using OTHub.Settings;

namespace OTHub.BackendSync.Blockchain.Tasks.Misc.Children
{
    public class ClearRPCHistoryTask : TaskRunGeneric
    {
        public ClearRPCHistoryTask() : base("Clear RPC Audit History")
        {
        }

        public override async Task Execute(Source source)
        {
            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                await connection.ExecuteAsync(@"DELETE FROM rpcshistory WHERE Timestamp < @date", new
                {
                    date = DateTime.Now.AddMonths(-1)
                });
            }
        }
    }
}