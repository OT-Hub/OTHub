using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Nethereum.Web3;
using OTHub.BackendSync.Logging;
using OTHub.Settings;

namespace OTHub.BackendSync.Blockchain.Tasks.BlockchainMaintenance
{
    public class BlockchainSyncTimeAdjustorTask : TaskRunBlockchain
    {
        public BlockchainSyncTimeAdjustorTask() : base("Blockchain Sync Frequency Algorithm")
        {
        }

        //Not my best work but I'm ill and didn't want to design a system right now for sharing/passing data between independent tasks
        public static TaskController.TaskControllerItem[] BlockchainSyncTaskControllers { get; set; }

        private const string _sql = @"SELECT 
AVG(diff)
 FROM (
SELECT  Sec_to_time(@diff)                               AS starttime,
            p.Timestamp                                        endtime,
            IF(@diff = 0, 0, TIME_TO_SEC(p.Timestamp) - @diff) AS diff,
            @diff := TIME_TO_SEC(p.TIMESTAMP),
          @i:=@i+1 AS iterator
     FROM   (
 SELECT MIN(DATE_ADD(DATE(TIMESTAMP), INTERVAL 1 SECOND)) Timestamp FROM ethblock WHERE blockchainid = @blockchainID AND TIMESTAMP > DATE_ADD(DATE(NOW()), INTERVAL -@days DAY)
 UNION
 	  SELECT Timestamp FROM ethblock WHERE blockchainid = @blockchainID AND TIMESTAMP > DATE_ADD(DATE(NOW()), INTERVAL -@days DAY) AND TIMESTAMP <= DATE_ADD(DATE(NOW()), INTERVAL -(@days - 1) DAY)
 	  UNION
SELECT LEAST(NOW(), (SELECT DATE_ADD(DATE_ADD(DATE(MAX(TIMESTAMP)), INTERVAL 1 DAY), INTERVAL -1 SECOND) Timestamp FROM ethblock WHERE blockchainid = @blockchainID AND TIMESTAMP > DATE_ADD(DATE(NOW()), INTERVAL -@days DAY)  AND TIMESTAMP <= DATE_ADD(DATE(NOW()), INTERVAL -(@days - 1) DAY)))
	  ) p,
            (SELECT @diff := 0) AS x,
             (SELECT @i:=0) AS foo
     ORDER BY TIMESTAMP) z
     WHERE z.iterator > 1
     ORDER BY iterator";

        public override TimeSpan GetExecutingInterval(BlockchainType type)
        {
            return TimeSpan.FromMinutes(10);
        }

        public override async Task<bool> Execute(Source source, BlockchainType blockchain, BlockchainNetwork network, IWeb3 cl, int blockchainID)
        {
            TaskController.TaskControllerItem controllerItem = BlockchainSyncTaskControllers?.FirstOrDefault(c =>
                c.BlockchainType == blockchain && c.BlockchainNetwork == network);

            if (controllerItem == null)
                return false;

            await using (var connection =
                new MySqlConnector.MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                decimal? averageBlockTimeInSecondsWithData = await connection.ExecuteScalarAsync<decimal?>(_sql, new
                {
                    blockchainID = blockchainID,
                    days = 0
                });

                if (averageBlockTimeInSecondsWithData == null)
                {
                    averageBlockTimeInSecondsWithData = await connection.ExecuteScalarAsync<decimal?>(_sql, new
                    {
                        blockchainID = blockchainID,
                        days = 1
                    });
                }

                if (averageBlockTimeInSecondsWithData == null)
                {
                    averageBlockTimeInSecondsWithData = (int) TimeSpan.FromHours(2).TotalSeconds;
                }

                int minutes = (int) (averageBlockTimeInSecondsWithData / 60);

                if (minutes <= 3)
                {
                    minutes = 3;
                }
                else if (minutes >= TimeSpan.FromHours(4).TotalMinutes)
                {
                    minutes = (int) TimeSpan.FromHours(4).TotalMinutes;
                }

                controllerItem.SetInterval(minutes);


                await TaskRunBlockchain.RefreshRPCs(connection, blockchainID);
            }

            return true;
        }
    }
}