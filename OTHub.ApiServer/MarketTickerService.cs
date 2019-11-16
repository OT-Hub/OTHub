//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using CoinGecko.Entities.Response.Simple;
//using CoinpaprikaAPI.Entity;
//using Dapper;
//using Microsoft.EntityFrameworkCore.Internal;
//using Microsoft.Extensions.Hosting;
//using MySql.Data.MySqlClient;

//namespace OTHubApi
//{
//    public class MarketTickerService : IHostedService, IDisposable
//    {
//        private Timer _timer;
//        private bool isRunning;

//        public async Task StartAsync(CancellationToken cancellationToken)
//        {
//            _timer = new Timer(DoWork, null, TimeSpan.FromSeconds(0),
//                TimeSpan.FromMinutes(15));


//        }

//        private void DoWork(object state)
//        {
//            if (isRunning)
//                return;

//            isRunning = true;

//            try
//            {
//                SyncTrac();
//                SyncEth();
//                Console.WriteLine("Finished syncing markets");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex);
//            }
//            finally
//            {
//                isRunning = false;
//            }
//        }

//        private void SyncTrac()
//        {
//            Console.WriteLine("Syncing TRAC Market");

//            DateTime now = DateTime.UtcNow;
//            DateTime latestTimestamp;

//            using (var connection = new MySqlConnection(Program.ConString))
//            {
//                latestTimestamp = connection.ExecuteScalar<DateTime?>(@"select max(ticker_trac.Timestamp) from ticker_trac") ?? connection.ExecuteScalar<DateTime>(@"SELECT Min(b.Timestamp) FROM ethblock b
//                    where b.Timestamp >= COALESCE((select max(ticker_trac.Timestamp) from ticker_trac), (SELECT Min(b.Timestamp) FROM ethblock b))");
//            }


//            if ((now - latestTimestamp).TotalMinutes <= 5)
//                return;

//            CoinpaprikaAPI.Client client = new CoinpaprikaAPI.Client();
//            Stopwatch watch = new Stopwatch();

//            using (var connection = new MySqlConnection(Program.ConString))
//            {
//                connection.Open();



//                for (DateTime date = latestTimestamp.Date; date.Date <= now; date = date.AddDays(1))
//                {
//                    if (date > now)
//                        break;

//                    watch.Restart();

//                    try
//                    {
//                        var tickers = client.GetHistoricalTickerForIdAsync("trac-origintrail",
//                                date,
//                                date.AddDays(1), 1000, "USD",
//                                TickerInterval.FiveMinutes)
//                            .Result;

//                        DataTable rawData = new DataTable("data");
//                        rawData.Columns.Add("Timestamp", typeof(DateTime));
//                        rawData.Columns.Add("Price", typeof(decimal));

//                        foreach (var ticker in tickers.Value)
//                        {
//                            if (ticker.Timestamp.UtcDateTime <= latestTimestamp)
//                                continue;

//                            var row = rawData.NewRow();

//                            row["Timestamp"] = ticker.Timestamp.UtcDateTime;
//                            row["Price"] = ticker.Price;

//                            rawData.Rows.Add(row);
//                        }

//                        if (!rawData.Rows.Any())
//                            return;

//                        using (MySqlTransaction tran =
//                            connection.BeginTransaction(System.Data.IsolationLevel.Serializable))
//                        {
//                            using (MySqlCommand cmd = new MySqlCommand())
//                            {
//                                cmd.Connection = connection;
//                                cmd.Transaction = tran;
//                                cmd.CommandText = "SELECT * FROM ticker_trac";
//                                using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
//                                {
//                                    //da.UpdateBatchSize = 1000;
//                                    using (MySqlCommandBuilder cb = new MySqlCommandBuilder(da))
//                                    {
//                                        da.Update(rawData);
//                                        tran.Commit();

//                                        var max = tickers.Value.Max(v => v.Timestamp.UtcDateTime);
//                                        if (max > latestTimestamp)
//                                        {
//                                            latestTimestamp = max;
//                                        }
//                                    }
//                                }
//                            }
//                        }
//                    }
//                    finally
//                    {
//                        watch.Stop();
//                        if (watch.ElapsedMilliseconds <= 120)
//                        {
//                            Thread.Sleep(75);
//                        }
//                    }
//                }
//            }
//        }

//        private void SyncEth()
//        {
//            Console.WriteLine("Syncing ETH Market");

//            DateTime now = DateTime.UtcNow;
//            DateTime latestTimestamp;

//            using (var connection = new MySqlConnection(Program.ConString))
//            {
//                latestTimestamp = connection.ExecuteScalar<DateTime?>(@"select max(ticker_eth.Timestamp) from ticker_eth") ?? connection.ExecuteScalar<DateTime>(@"SELECT Min(b.Timestamp) FROM ethblock b
//                    where b.Timestamp >= COALESCE((select max(ticker_eth.Timestamp) from ticker_eth), (SELECT Min(b.Timestamp) FROM ethblock b))");
//            }


//            if ((now - latestTimestamp).TotalMinutes <= 5)
//                return;

//            CoinpaprikaAPI.Client client = new CoinpaprikaAPI.Client();
//            Stopwatch watch = new Stopwatch();

//            using (var connection = new MySqlConnection(Program.ConString))
//            {
//                connection.Open();



//                for (DateTime date = latestTimestamp.Date; date.Date <= now; date = date.AddDays(1))
//                {
//                    if (date > now)
//                        break;

//                    watch.Restart();

//                    try
//                    {
//                        var tickers = client.GetHistoricalTickerForIdAsync("eth-ethereum",
//                                date,
//                                date.AddDays(1), 1000, "USD",
//                                TickerInterval.FiveMinutes)
//                            .Result;

//                        DataTable rawData = new DataTable("data");
//                        rawData.Columns.Add("Timestamp", typeof(DateTime));
//                        rawData.Columns.Add("Price", typeof(decimal));

//                        foreach (var ticker in tickers.Value)
//                        {
//                            if (ticker.Timestamp.UtcDateTime <= latestTimestamp)
//                                continue;

//                            var row = rawData.NewRow();

//                            row["Timestamp"] = ticker.Timestamp.UtcDateTime;
//                            row["Price"] = ticker.Price;

//                            rawData.Rows.Add(row);
//                        }

//                        if (!rawData.Rows.Any())
//                            continue;


//                        using (MySqlTransaction tran =
//                            connection.BeginTransaction(System.Data.IsolationLevel.Serializable))
//                        {
//                            using (MySqlCommand cmd = new MySqlCommand())
//                            {
//                                cmd.Connection = connection;
//                                cmd.Transaction = tran;
//                                cmd.CommandText = "SELECT * FROM ticker_eth";
//                                using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
//                                {
//                                    //da.UpdateBatchSize = 1000;
//                                    using (MySqlCommandBuilder cb = new MySqlCommandBuilder(da))
//                                    {
//                                        da.Update(rawData);
//                                        tran.Commit();

//                                        var max = tickers.Value.Max(v => v.Timestamp.UtcDateTime);
//                                        if (max > latestTimestamp)
//                                        {
//                                            latestTimestamp = max;
//                                        }
//                                    }
//                                }
//                            }
//                        }
//                    }
//                    finally
//                    {
//                        watch.Stop();
//                        if (watch.ElapsedMilliseconds <= 120)
//                        {
//                            Thread.Sleep(75);
//                        }
//                    }
//                }
//            }
//        }

//        public async Task StopAsync(CancellationToken cancellationToken)
//        {
//            _timer.Dispose();
//        }

//        public void Dispose()
//        {

//        }
//    }
//}