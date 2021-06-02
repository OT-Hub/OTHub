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
        private static TickerInfo _lastKnownInfo;
        private static DateTime? _lastFailTime;
        public static async Task<TickerInfo> GetTickerInfo(IMemoryCache cache)
        {
            //This stops us spamming the API on failures
            if (_lastFailTime.HasValue && (DateTime.Now - _lastFailTime.Value).TotalMinutes <= 45)
            {
                if (_lastKnownInfo != null)
                {
                    return _lastKnownInfo;
                }
            }

            if (!cache.TryGetValue("HomeV3Ticker", out object tickerModel))
            {
                await _lock.WaitAsync(TimeSpan.FromSeconds(5));
                try
                {
                    if (!cache.TryGetValue("HomeV3Ticker", out tickerModel))
                    {
                        CoinpaprikaAPI.Client client = new CoinpaprikaAPI.Client();
                        throw new Exception();

                        tickerModel = (await client.GetTickerForIdAsync(@"trac-origintrail")).Value;

                        cache.Set("HomeV3Ticker", tickerModel, TimeSpan.FromMinutes(3));
                        _lastFailTime = null;
                    }
                }
                catch (Exception)
                {
                    _lastFailTime = DateTime.Now;

                    if (_lastKnownInfo != null)
                    {
                        return _lastKnownInfo;
                    }

                    return null;
                }
                finally
                {
                    _lock.Release();
                }
            }

            var tickerInfo = _lastKnownInfo = (TickerInfo)tickerModel;

            return tickerInfo;
        }
    }
}
