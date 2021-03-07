using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using OTHub.BackendSync.Logging;
using OTHub.Settings;

namespace OTHub.BackendSync.Blockchain.Tasks.Misc.Children
{
    public class UpdateHomeJobHistoryChartDataTask : TaskRunGeneric
    {
        public UpdateHomeJobHistoryChartDataTask() : base("Update Job History Chart Data")
        {
        }

        public override async Task Execute(Source source)
        {
            using (var con = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                con.Execute(@"INSERT INTO jobhistorybyday 
SELECT 
x.Date,
COUNT(O.OfferId) NewJobs,
(
	SELECT COUNT(OI.OfferId) FROM OTOffer OI 
	WHERE 
	OI.IsFinalized = 1
	AND 
	DATE(DATE_Add(OI.FinalizedTimeStamp, INTERVAL + OI.HoldingTimeInMinutes MINUTE)) = x.Date
	)
	as CompletedJobs
FROM (
select * from 
(select ADDDATE('2010-01-01',t4.i*10000 + t3.i*1000 + t2.i*100 + t1.i*10 + t0.i) Date from
 (select 0 i union select 1 union select 2 union select 3 union select 4 union select 5 union select 6 union select 7 union select 8 union select 9) t0,
 (select 0 i union select 1 union select 2 union select 3 union select 4 union select 5 union select 6 union select 7 union select 8 union select 9) t1,
 (select 0 i union select 1 union select 2 union select 3 union select 4 union select 5 union select 6 union select 7 union select 8 union select 9) t2,
 (select 0 i union select 1 union select 2 union select 3 union select 4 union select 5 union select 6 union select 7 union select 8 union select 9) t3,
 (select 0 i union select 1 union select 2 union select 3 union select 4 union select 5 union select 6 union select 7 union select 8 union select 9) t4) v
where Date BETWEEN (SELECT MIN(ethblock.Timestamp) FROM ethblock) AND (SELECT MAX(ethblock.Timestamp) FROM ethblock)
AND DATE NOT IN (SELECT DATE FROM jobhistorybyday) AND DATE < DATE(NOW())
) x 
LEFT JOIN OTOffer O on O.IsFinalized = 1 AND x.Date = DATE(O.FinalizedTimestamp)
GROUP BY x.Date");
            }
        }
    }
}
