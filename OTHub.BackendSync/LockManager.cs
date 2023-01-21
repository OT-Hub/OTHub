using AsyncKeyedLock;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace OTHub.BackendSync
{
    public static class LockManager
    {
        private static readonly AsyncKeyedLocker<LockType> _asyncKeyedLocker = new AsyncKeyedLocker<LockType>(o =>
        {
            o.PoolSize = Enum.GetNames(typeof(LockType)).Length;
            o.PoolInitialFill = 1;
        });

        public static bool IsInUse(LockType type)
        {
            return _asyncKeyedLocker.IsInUse(type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async ValueTask<IDisposable> Lock(LockType type)
        {
            return await _asyncKeyedLocker.LockAsync(type).ConfigureAwait(false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async ValueTask<IDisposable> Lock(LockType type, int milliseconds)
        {
            var releaser = (AsyncKeyedLockTimeoutReleaser<LockType>)await _asyncKeyedLocker.LockAsync(type, milliseconds).ConfigureAwait(false);
            if (!releaser.EnteredSemaphore)
            {
                releaser.Dispose();
                throw new Exception("Lock did not release in time.");
            }
            return releaser;
        }
    }

    public enum LockType
    {
        PayoutInsert,
        OfferCreated,
        OfferFinalised,
        ProcessJobs,
        OfferTask,
        IdentityCreated,
        ProfileCreated,
        TokensDeposited,
        TokensWithdrawn,
        TokensReleased
    }
}