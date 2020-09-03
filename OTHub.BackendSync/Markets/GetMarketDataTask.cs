using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CoinMarketCap;
using CoinpaprikaAPI.Entity;
using Dapper;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using OTHub.Settings;

namespace OTHub.BackendSync.Tasks
{
    public class GetMarketDataTask : TaskRun
    {
        public override async Task Execute(Source source)
        {
            try
            {
                Console.WriteLine("Syncing TRAC Market (USD)");

                DateTime now = DateTime.UtcNow;
                DateTime latestTimestamp;

                using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                {
                    latestTimestamp = connection.ExecuteScalar<DateTime?>(@"select max(ticker_trac.Timestamp) from ticker_trac") ?? connection.ExecuteScalar<DateTime>(@"SELECT Min(b.Timestamp) FROM ethblock b
                    where b.Timestamp >= COALESCE((select max(ticker_trac.Timestamp) from ticker_trac), (SELECT Min(b.Timestamp) FROM ethblock b))");
                }

                if ((now - latestTimestamp).TotalHours > 6)
                {

                    CoinpaprikaAPI.Client client = new CoinpaprikaAPI.Client();

                    using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                    {
                        connection.Open();

                        for (DateTime date = latestTimestamp.Date; date.Date <= now; date = date.AddDays(1))
                        {
                            if (date > now)
                                break;

                            Thread.Sleep(400);

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
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                CachetLogger.FailComponent(9);
            }
                

            await ExecuteEth(source);
        }

        public class RootObject
        {
            public List<List<double>> prices { get; set; }
            public List<List<double>> market_caps { get; set; }
            public List<List<double>> total_volumes { get; set; }
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public async Task ExecuteEth(Source source)
        {
            try
            {
                Console.WriteLine("Syncing TRAC Market (ETH)");

                DateTime now = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0, DateTimeKind.Utc);
                DateTime latestTimestamp;

                using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                {
                    latestTimestamp = connection.ExecuteScalar<DateTime?>(@"select max(ticker_eth.Timestamp) from ticker_eth") ?? connection.ExecuteScalar<DateTime>(@"SELECT Min(b.Timestamp) FROM ethblock b
                    where b.Timestamp >= COALESCE((select max(ticker_eth.Timestamp) from ticker_eth), (SELECT Min(b.Timestamp) FROM ethblock b))");
                }

                var startDate = new DateTime(2019, 12, 26, 17, 0, 0, DateTimeKind.Utc);

                if (latestTimestamp < startDate)
                {
                    latestTimestamp = startDate;
                }

                if ((now - latestTimestamp).TotalHours < 1)
                    return;

                //CoinpaprikaAPI.Client client = new CoinpaprikaAPI.Client();

                using (var wc = new WebClient())
                {



                    using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                    {
                        connection.Open();

                        for (DateTime date = latestTimestamp.Date; date.Date <= now; date = date.AddDays(1))
                        {
                            if (date > now)
                                break;


                            DataTable rawData = new DataTable();
                            rawData.Columns.Add("Timestamp", typeof(DateTime));
                            rawData.Columns.Add("Price", typeof(decimal));

                            RootObject obj = null;

                            for (int i = 0; i < 24; i++)
                            {
                                Thread.Sleep(250);

                                Int32 unixStartTimestamp = (Int32)(date.AddHours(i).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                                Int32 unixEndTimestamp = (Int32)(date.AddHours(i).AddHours(1).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                                var data = wc.DownloadString(
                                    $"https://api.coingecko.com/api/v3/coins/origintrail/market_chart/range?vs_currency=eth&from={unixStartTimestamp}&to={unixEndTimestamp}");

                                obj = JsonConvert.DeserializeObject<RootObject>(data);

                      
                                if (obj?.prices == null)
                                    continue;

                                foreach (List<double> ticker in obj.prices)
                                {
                                    DateTime tickerTime = UnixTimeStampToDateTime(Convert.ToDouble(ticker[0].ToString().Substring(0, 10)));

                                    if (tickerTime <= latestTimestamp)
                                        continue;

                                    var row = rawData.NewRow();


                                    row["Timestamp"] = tickerTime;
                                    row["Price"] = ticker[1];
                                    rawData.Rows.Add(row);

                                    break;
                                }
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
                                    cmd.CommandText = "SELECT * FROM ticker_eth";
                                    using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                                    {
                                        //da.UpdateBatchSize = 1000;
                                        using (MySqlCommandBuilder cb = new MySqlCommandBuilder(da))
                                        {
                                            da.Update(rawData);
                                            tran.Commit();

                                            if (obj != null)
                                            {
                                                var max = obj.prices.Max(v => UnixTimeStampToDateTime(Convert.ToDouble(v[0].ToString().Substring(0, 10))));
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