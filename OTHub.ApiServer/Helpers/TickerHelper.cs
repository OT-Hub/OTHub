using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoinpaprikaAPI.Entity;
using Microsoft.Extensions.Caching.Memory;

namespace OTHub.APIServer.Helpers
{
    public static class TickerHelper
    {
        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        public static async Task<TickerInfo> GetTickerInfo(IMemoryCache cache)
        {
            if (!cache.TryGetValue("HomeV3Ticker", out object tickerModel))
            {
                await _lock.WaitAsync(TimeSpan.FromSeconds(5));
                try
                {
                    if (!cache.TryGetValue("HomeV3Ticker", out tickerModel))
                    {
                        CoinpaprikaAPI.Client client = new CoinpaprikaAPI.Client();

                        tickerModel = (await client.GetTickerForIdAsync(@"trac-origintrail")).Value;

                        cache.Set("HomeV3Ticker", tickerModel, TimeSpan.FromMinutes(3));
                    }
                }
                finally
                {
                    _lock.Release();
                }
            }

            var tickerInfo = (TickerInfo)tickerModel;

            return tickerInfo;
        }
    }
}
