using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OTHub.BackendSync
{
    public static class LockManager
    {
        private static readonly ConcurrentDictionary<LockType, SemaphoreSlim> _lockDictionary = new ConcurrentDictionary<LockType, SemaphoreSlim>();

        public static LockRelease GetLock(LockType type)
        {
            if (!_lockDictionary.TryGetValue(type, out SemaphoreSlim lck))
            {
                lck = new SemaphoreSlim(1, 1);
                _lockDictionary[type] = lck;
            }

            return new LockRelease(lck);
        }
    }

    public class LockRelease : IDisposable
    {
        private readonly SemaphoreSlim _lck;

        public LockRelease(SemaphoreSlim lck)
        {
            _lck = lck;
        }

        public void Dispose()
        {
            _lck.Release();
        }

        public async Task<LockRelease> Lock()
        {
            await _lck.WaitAsync();

            return this;
        }
    }

    public enum LockType
    {
        PayoutInsert,
        OfferCreated,
        OfferFinalised,
        ProcessJobs
    }
}