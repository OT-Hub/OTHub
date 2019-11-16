using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoinpaprikaAPI.Entity;
using Dapper;
using MySql.Data.MySqlClient;
using Nethereum.JsonRpc.Client;
using Nethereum.Web3;
using Newtonsoft.Json;
using OTHelperNetStandard.Models.Json;
using OTHub.Settings;

namespace OTHelperNetStandard.Tasks
{
    public class GetMarketDataTask : TaskRun
    {
        public override async Task Execute(Source source)
        {
            try
            {
                Console.WriteLine("Syncing TRAC Market");

                DateTime now = DateTime.UtcNow;
                DateTime latestTimestamp;

                using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                {
                    latestTimestamp = connection.ExecuteScalar<DateTime?>(@"select max(ticker_trac.Timestamp) from ticker_trac") ?? connection.ExecuteScalar<DateTime>(@"SELECT Min(b.Timestamp) FROM ethblock b
                    where b.Timestamp >= COALESCE((select max(ticker_trac.Timestamp) from ticker_trac), (SELECT Min(b.Timestamp) FROM ethblock b))");
                }

                if ((now - latestTimestamp).TotalHours < 6)
                    return;

                CoinpaprikaAPI.Client client = new CoinpaprikaAPI.Client();
                
                using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                {
                    connection.Open();

                    for (DateTime date = latestTimestamp.Date; date.Date <= now; date = date.AddDays(1))
                    {
                        if (date > now)
                            break;

                        Thread.Sleep(500);

                        var tickers = client.GetHistoricalTickerForIdAsync("trac-origintrail",
                                date,
                                date.AddDays(1), 1000, "USD",
                                TickerInterval.SixHours)
                            .Result;
                        
                        DataTable rawData = new DataTable();
                        rawData.Columns.Add("Timestamp", typeof(DateTime));
                        rawData.Columns.Add("Price", typeof(decimal));

                        if (tickers?.Value == null)
                            continue;

                        foreach (var ticker in tickers.Value)
                        {
                            if (ticker.Timestamp.UtcDateTime <= latestTimestamp)
                                continue;

                            var row = rawData.NewRow();

                            
                            row["Timestamp"] = ticker.Timestamp.UtcDateTime;
                            row["Price"] = ticker.Price;
                            rawData.Rows.Add(row);

                        }

                        if (rawData.Rows.Count == 0)
                            continue;
                        

                        using (MySqlTransaction tran =
                            connection.BeginTransaction(System.Data.IsolationLevel.Serializable))
                        {
                            using (MySqlCommand cmd = new MySqlCommand())
                            {
                                cmd.Connection = connection;
                                cmd.Transaction = tran;
                                cmd.CommandText = "SELECT * FROM ticker_trac";
                                using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                                {
                                    //da.UpdateBatchSize = 1000;
                                    using (MySqlCommandBuilder cb = new MySqlCommandBuilder(da))
                                    {
                                        da.Update(rawData);
                                        tran.Commit();

                                        var max = tickers.Value.Max(v => v.Timestamp.UtcDateTime);
                                        if (max > latestTimestamp)
                                        {
                                            latestTimestamp = max;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                CachetLogger.FailComponent(9);
            }
        }

        public GetMarketDataTask() : base("Get Market Data")
        {
        }
    }

}