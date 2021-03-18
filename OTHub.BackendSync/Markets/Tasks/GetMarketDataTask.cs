using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CoinpaprikaAPI.Entity;
using ComposableAsync;
using Dapper;
using MySqlConnector;
using Newtonsoft.Json;
using OTHub.BackendSync.Logging;
using OTHub.Settings;
using OTHub.Settings.Helpers;
using RateLimiter;

namespace OTHub.BackendSync.Markets.Tasks
{
    public class GetMarketDataTask : TaskRunGeneric
    {
        static GetMarketDataTask()
        {
            CountByIntervalAwaitableConstraint constraint = new CountByIntervalAwaitableConstraint(3, TimeSpan.FromSeconds(1));


            CountByIntervalAwaitableConstraint constraint2 = new CountByIntervalAwaitableConstraint(1, TimeSpan.FromMilliseconds(600));

            TimeConstraint = TimeLimiter.Compose(constraint, constraint2);
        }

        public static TimeLimiter TimeConstraint { get; set; }

        public override async Task Execute(Source source)
        {
            Logger.WriteLine(source, "Syncing TRAC Market (USD)");

            DateTime now = DateTime.UtcNow;
            DateTime latestTimestamp;

            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                latestTimestamp =
                    connection.ExecuteScalar<DateTime?>(@"select max(ticker_trac.Timestamp) from ticker_trac") ??
                    connection.ExecuteScalar<DateTime>(@"SELECT Min(b.Timestamp) FROM ethblock b
                    where b.Timestamp >= COALESCE((select max(ticker_trac.Timestamp) from ticker_trac), (SELECT Min(b.Timestamp) FROM ethblock b))");
            }

            if ((now - latestTimestamp).TotalHours > 6)
            {

                CoinpaprikaAPI.Client client = new CoinpaprikaAPI.Client();

                await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                {
                    await connection.OpenAsync();

                    for (DateTime date = latestTimestamp.Date; date.Date <= now; date = date.AddDays(1))
                    {
                        if (date > now)
                            break;

                        await TimeConstraint;

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


                        await using (MySqlTransaction tran =
                            await connection.BeginTransactionAsync(global::System.Data.IsolationLevel.Serializable))
                        {
                            await using (MySqlCommand cmd = new MySqlCommand())
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
                                        await tran.CommitAsync();

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



            await ExecuteEth(source);
        }

        public class RootObject
        {
            public List<List<double>> prices { get; set; }
            public List<List<double>> market_caps { get; set; }
            public List<List<double>> total_volumes { get; set; }
        }

        public async Task ExecuteEth(Source source)
        {
            Logger.WriteLine(source, "Syncing TRAC Market (ETH)");

            DateTime now = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day,
                DateTime.UtcNow.Hour, 0, 0, DateTimeKind.Utc);
            DateTime latestTimestamp;

            using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                latestTimestamp =
                    connection.ExecuteScalar<DateTime?>(@"select max(ticker_eth.Timestamp) from ticker_eth") ??
                    connection.ExecuteScalar<DateTime>(@"SELECT Min(b.Timestamp) FROM ethblock b
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



                await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                {
                    await connection.OpenAsync();

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

                            Int32 unixStartTimestamp =
                                (Int32)(date.AddHours(i).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                            Int32 unixEndTimestamp =
                                (Int32)(date.AddHours(i).AddHours(1).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                            await TimeConstraint;

                            var data = wc.DownloadString(
                                $"https://api.coingecko.com/api/v3/coins/origintrail/market_chart/range?vs_currency=eth&from={unixStartTimestamp}&to={unixEndTimestamp}");

                            obj = JsonConvert.DeserializeObject<RootObject>(data);


                            if (obj?.prices == null)
                                continue;

                            foreach (List<double> ticker in obj.prices)
                            {
                                DateTime tickerTime =
                                    TimestampHelper.UnixTimeStampToDateTime(Convert.ToDouble(ticker[0].ToString().Substring(0, 10)));

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


                        await using (MySqlTransaction tran =
                            await connection.BeginTransactionAsync(global::System.Data.IsolationLevel.Serializable))
                        {
                            await using (MySqlCommand cmd = new MySqlCommand())
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
                                        await tran.CommitAsync();

                                        if (obj != null && obj.prices != null && obj.prices.Any())
                                        {
                                            var max = obj.prices.Max(v =>
                                                TimestampHelper.UnixTimeStampToDateTime(
                                                    Convert.ToDouble(v[0].ToString().Substring(0, 10))));
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

        public GetMarketDataTask() : base("Get Market Data")
        {
        }
    }
}