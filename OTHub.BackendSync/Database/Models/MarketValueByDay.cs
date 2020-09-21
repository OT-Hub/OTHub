using System;
using Dapper;
using MySqlConnector;
using OTHub.BackendSync.Markets.Models;

namespace OTHub.BackendSync.Database.Models
{
    public class MarketValueByDay
    {
        public DateTime Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public int Volume { get; set; }
        public int MarketCap { get; set; }

        public static void InsertIfNotExist(MySqlConnection connection, MarketValueByDayJson jsonModel)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM MarketValueByDay WHERE Date = @Date", new
            {
                jsonModel.time_open.Date
            });

            if (count == 0)
            {
                var model = new MarketValueByDay
                {
                    Date = jsonModel.time_open.Date,
                    Open = jsonModel.open,
                    MarketCap = jsonModel.market_cap,
                    Close = jsonModel.close,
                    Low = jsonModel.low,
                    High = jsonModel.high,
                    Volume = jsonModel.volume
                };

                connection.Execute(
                    @"INSERT INTO MarketValueByDay(Date, Open, High, Low, Close, Volume, MarketCap)
VALUES(@Date, @Open, @High, @Low, @Close, @Volume, @MarketCap)",
                    new
                    {
                        model.Date,
                        model.Open,
                        model.High,
                        model.Low,
                        model.Close,
                        model.Volume,
                        model.MarketCap
                    });

                connection.Execute(@"UPDATE otcontract_holding_paidout
SET AmountInUSD = Amount * @usdValue
WHERE Date(Timestamp) = @date", new
                {
                    date = model.Date,
                    usdValue = model.Close
                });
            }
        }
    }
}