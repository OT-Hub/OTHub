using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace OTHub.APIServer
{
    public class PerformanceLogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ConcurrentDictionary<DateTime, List<double>> _concurrentDictionary = new ConcurrentDictionary<DateTime, List<double>>();

        public PerformanceLogMiddleware(RequestDelegate next)
        {
            _next = next;
            return;

            Task.Run(() =>
            {
                while (true)
                {
                    DateTime date = DateTime.UtcNow;
                    date = new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, 0, DateTimeKind.Utc);

                    IEnumerable<KeyValuePair<DateTime, List<double>>> old =_concurrentDictionary.Where(c => c.Key < date);

                    foreach (var keyValuePair in old)
                    {
                        if (_concurrentDictionary.TryRemove(keyValuePair.Key, out var values))
                        {
                            try
                            {
                                //var cachet = new Cachet.NET.Cachet("https://status.othub.info/api/v1/",
                                //    "");

                                //cachet.AddMetricPoint(6, (int)values.Average(), keyValuePair.Key);
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                    }

                    Thread.Sleep(55000);
                }
            });
        }

        public async Task Invoke(HttpContext context)
        {
          //  Stopwatch stopwatch = Stopwatch.StartNew();
            await _next(context);
           // stopwatch.Stop();
            //LogToCachet(stopwatch);
        }

        private void LogToCachet(Stopwatch stopwatch)
        {
            return;

            if (stopwatch.ElapsedMilliseconds <= 1)
                return;

            DateTime date = DateTime.UtcNow;
            date = new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, 0, DateTimeKind.Utc);

            _concurrentDictionary.AddOrUpdate(date, new List<double>(), (id, num) =>
            {
                num.Add(stopwatch.ElapsedMilliseconds);
                return num;
            });
        }
    }
}