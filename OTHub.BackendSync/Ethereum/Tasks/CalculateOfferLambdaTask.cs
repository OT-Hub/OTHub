using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using OTHub.BackendSync.Logging;
using OTHub.Settings;

namespace OTHub.BackendSync.Ethereum.Tasks
{
    public class CalculateOfferLambdaTask : TaskRun
    {
        public CalculateOfferLambdaTask() : base("Calculate New Offers Estimated Lambda")
        {
        }

        public override async Task Execute(Source source)
        {
            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var rows = connection.Query(
                    @"select OfferID, CreatedTimestamp, DataSetSizeInBytes, TokenAmountPerHolder, HoldingTimeInMinutes  from otoffer
where CreatedTimestamp >= '2019/12/26 17:00' AND EstimatedLambda IS NULL").ToArray();

                foreach (var row in rows)
                {
                    string offerID = row.OfferID;
                    DateTime createdTimestamp = row.CreatedTimestamp;
                    double dataSetSize = row.DataSetSizeInBytes;
                    decimal tokenAmount = row.TokenAmountPerHolder;
                    long holdingTime = row.HoldingTimeInMinutes;

                    var days = TimeSpan.FromMinutes(holdingTime).TotalDays;

                    dataSetSize = (dataSetSize / 1024) / 1024;

                    var ethPrice = connection.QueryFirstOrDefault<ClosestTime>(
                            @"select Timestamp, Price, ABS(TIMESTAMPDIFF(SECOND, @date, Timestamp)) as DiffInSeconds from ticker_eth
WHERE ABS(TIMESTAMPDIFF(SECOND, @date, Timestamp)) < 7100
ORDER BY ABS(TIMESTAMPDIFF(SECOND, @date, Timestamp))
LIMIT 1", new
                            {
                                date = createdTimestamp
                            });

                    if (ethPrice == null)
                        continue;

                    Dictionary<double, decimal> factorToAmount = new Dictionary<double, decimal>();

                    double factor = 6;

                    factorToAmount[factor] =
                        Convert.ToDecimal(
                            Math.Round(2 * (0.00075 / ethPrice.Price) + factor * Math.Sqrt(2 * days * dataSetSize)));

                    if (factorToAmount[factor] > tokenAmount)
                    {
                        double loopFactor = factor;

                        while (factorToAmount[loopFactor] > tokenAmount)
                        {
                            loopFactor -= 1;

                            factorToAmount[loopFactor] = Convert.ToDecimal(Math.Round(2 * (0.00075 / ethPrice.Price) + loopFactor * Math.Sqrt(2 * days * dataSetSize)));
                        }
                    }
                    else if (factorToAmount[factor] < tokenAmount)
                    {
                        double loopFactor = factor;

                        while (factorToAmount[loopFactor] < tokenAmount)
                        {
                            loopFactor += 1;

                            factorToAmount[loopFactor] = Convert.ToDecimal(Math.Round(2 * (0.00075 / ethPrice.Price) + loopFactor * Math.Sqrt(2 * days * dataSetSize)));
                        }
                    }

                    foreach (KeyValuePair<double, decimal> keyValuePair in factorToAmount.OrderBy(f => f.Key))
                    {
                        double loopFactor = keyValuePair.Key;

                        for (int i = 1; i <= 9; i++)
                        {
                            loopFactor += 0.10;

                            factorToAmount[loopFactor] = Convert.ToDecimal(Math.Round(2 * (0.00075 / ethPrice.Price) + loopFactor * Math.Sqrt(2 * days * dataSetSize)));
                        }
                    }

                    var data = factorToAmount.OrderBy(i => Math.Abs(i.Value - tokenAmount)).FirstOrDefault();

                    if (data.Value != 0 && data.Key != 0)
                    {
                        connection.Execute(@"UPDATE OTOffer SET EstimatedLambda = @lambda WHERE OfferID = @offerID", new
                        {
                            offerID = offerID,
                            lambda = data.Key
                        });
                    }
                }
            }

            //Math.round(2 * (0.00075 / this.config.blockchain.trac_price_in_eth) + this.config.blockchain.dh_price_factor * Math.sqrt(2 * 180 * 1))

            //var test = Math.Round(2 * (0.00075 / 7.75e-05) + 5 * Math.Sqrt(2 * 180 * 0.23601));
        }

        public enum SizeUnits
        {
            Byte, KB, MB, GB, TB, PB, EB, ZB, YB
        }

        public string ToSize(Int64 value, SizeUnits unit)
        {
            return (value / (double)Math.Pow(1024, (Int64)unit)).ToString("0.00");
        }
    }

    public class ClosestTime
    {
        public DateTime Timestamp { get; set; }
        public double Price { get; set; }
        public int DiffInSeconds { get; set; }
    }
}
