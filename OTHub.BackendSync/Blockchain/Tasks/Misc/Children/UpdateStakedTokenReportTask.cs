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
    public class UpdateStakedTokenReportTask : TaskRunGeneric
    {
        public UpdateStakedTokenReportTask() : base("Update Staked Token Report")
        {
        }

        public override async Task Execute(Source source)
        {
			//This could be more efficient but after the first one is run, the daily run is nearly instant
            await using (var con = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                await con.ExecuteAsync(@"INSERT INTO stakedtokensbyday 
SELECT 
x.Date,
COALESCE((SELECT SUM(td.AmountDeposited) FROM otcontract_profile_tokensdeposited td 
	JOIN ethblock b ON b.BlockchainID = td.BlockchainID AND b.BlockNumber = td.BlockNumber 
	WHERE DATE(b.Timestamp) = x.Date), 0) + COALESCE((SELECT SUM(pc.InitialBalance) FROM otcontract_profile_profilecreated pc 
	JOIN ethblock b ON b.BlockchainID = pc.BlockchainID AND b.BlockNumber = pc.BlockNumber 
	WHERE DATE(b.Timestamp) = x.Date), 0) AS TokensDeposited,
	
	COALESCE((SELECT SUM(tw.AmountWithdrawn) FROM otcontract_profile_tokenswithdrawn tw
	JOIN ethblock b ON b.BlockchainID = tw.BlockchainID AND b.BlockNumber = tw.BlockNumber 
	WHERE DATE(b.Timestamp) = x.Date), 0) AS TokensWithdrawn,
	
	COALESCE((SELECT SUM(pc.InitialBalance) FROM otcontract_profile_profilecreated pc 
	JOIN ethblock b ON b.BlockchainID = pc.BlockchainID AND b.BlockNumber = pc.BlockNumber 
	WHERE Date(b.Timestamp) <= x.Date), 0)
	 +    COALESCE((SELECT SUM(td.AmountDeposited) FROM otcontract_profile_tokensdeposited td 
	JOIN ethblock b ON b.BlockchainID = td.BlockchainID AND b.BlockNumber = td.BlockNumber 
	WHERE Date(b.Timestamp) <= x.Date), 0) 
	 - 	COALESCE((SELECT SUM(tw.AmountWithdrawn) FROM otcontract_profile_tokenswithdrawn tw
	JOIN ethblock b ON b.BlockchainID = tw.BlockchainID AND b.BlockNumber = tw.BlockNumber 
	WHERE DATE(b.Timestamp) <= x.Date), 0) AS StakedTokens
FROM (
select * from 
(select ADDDATE('2010-01-01',t4.i*10000 + t3.i*1000 + t2.i*100 + t1.i*10 + t0.i) Date from
 (select 0 i union select 1 union select 2 union select 3 union select 4 union select 5 union select 6 union select 7 union select 8 union select 9) t0,
 (select 0 i union select 1 union select 2 union select 3 union select 4 union select 5 union select 6 union select 7 union select 8 union select 9) t1,
 (select 0 i union select 1 union select 2 union select 3 union select 4 union select 5 union select 6 union select 7 union select 8 union select 9) t2,
 (select 0 i union select 1 union select 2 union select 3 union select 4 union select 5 union select 6 union select 7 union select 8 union select 9) t3,
 (select 0 i union select 1 union select 2 union select 3 union select 4 union select 5 union select 6 union select 7 union select 8 union select 9) t4) v
where Date BETWEEN (SELECT MIN(ethblock.Timestamp) FROM ethblock) AND (SELECT MAX(ethblock.Timestamp) FROM ethblock)
AND DATE NOT IN (SELECT DATE FROM stakedtokensbyday) AND DATE < DATE(NOW())
) X 
GROUP BY x.Date", commandTimeout:500);
            }
        }
    }
}