using Dapper;
using MySql.Data.MySqlClient;
using OTHub.Settings;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OTHub.BackendSync.Tasks
{
    public class OptimiseDatabaseTask : TaskRun
    {
        public OptimiseDatabaseTask() : base("Optimise Database")
        {

        }

        public async override Task Execute(Source source)
        {
            using (var connection =
            new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                await connection.ExecuteAsync(@"DELETE from otnode_onlinecheck c                      
WHERE c.TIMESTAMP < DATE_ADD(NOW(), INTERVAL -1 MONTH)", commandTimeout: (int)TimeSpan.FromMinutes(60).TotalSeconds);

                await connection.ExecuteAsync(@"delete from otnode_history
where timestamp <= DATE_ADD(NOW(), INTERVAL -8 DAY)", commandTimeout: (int)TimeSpan.FromMinutes(60).TotalSeconds);
            }
        }
    }
}