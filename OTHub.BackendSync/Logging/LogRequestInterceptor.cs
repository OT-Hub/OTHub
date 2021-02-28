using System;
using System.Threading.Tasks;
using ComposableAsync;
using Nethereum.JsonRpc.Client;
using RateLimiter;

namespace OTHub.BackendSync.Logging
{
    public class LogRequestInterceptor : RequestInterceptor
    {
        static LogRequestInterceptor()
        {
            CountByIntervalAwaitableConstraint constraint = new CountByIntervalAwaitableConstraint(4, TimeSpan.FromSeconds(1));

            
            CountByIntervalAwaitableConstraint constraint2 = new CountByIntervalAwaitableConstraint(1, TimeSpan.FromMilliseconds(150));
            
            TimeConstraint = TimeLimiter.Compose(constraint, constraint2);
        }

        private static TimeLimiter TimeConstraint { get; }

        public override async Task InterceptSendRequestAsync(Func<RpcRequest, string, Task> interceptedSendRequestAsync, RpcRequest request, string route = null)
        {
            await TimeConstraint;

#if DEBUG
            Logger.WriteLine(Source.NodeUptimeAndMisc, request.Method);
#endif

            await base.InterceptSendRequestAsync(interceptedSendRequestAsync, request, route);
        }

        public override async Task<object> InterceptSendRequestAsync<T>(Func<RpcRequest, string, Task<T>> interceptedSendRequestAsync, RpcRequest request, string route = null)
        {
            await TimeConstraint;

#if DEBUG
            Logger.WriteLine(Source.NodeUptimeAndMisc, request.Method);
#endif

            return await base.InterceptSendRequestAsync(interceptedSendRequestAsync, request, route);
        }

        public override async Task<object> InterceptSendRequestAsync<T>(Func<string, string, object[], Task<T>> interceptedSendRequestAsync, string method, string route = null,
            params object[] paramList)
        {
            await TimeConstraint;

#if DEBUG
            Logger.WriteLine(Source.NodeUptimeAndMisc, method);
#endif

            return await base.InterceptSendRequestAsync(interceptedSendRequestAsync, method, route, paramList);
        }

        public override async Task InterceptSendRequestAsync(Func<string, string, object[], Task> interceptedSendRequestAsync, string method, string route = null,
            params object[] paramList)
        {
            await TimeConstraint;

#if DEBUG
            Logger.WriteLine(Source.NodeUptimeAndMisc, method);
#endif

            await base.InterceptSendRequestAsync(interceptedSendRequestAsync, method, route, paramList);
        }
    }
}