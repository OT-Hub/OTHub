using System;
using System.Threading.Tasks;
using Nethereum.JsonRpc.Client;

namespace OTHub.BackendSync.Logging
{
    public class LogRequestInterceptor : RequestInterceptor
    {
        public override Task InterceptSendRequestAsync(Func<RpcRequest, string, Task> interceptedSendRequestAsync, RpcRequest request, string route = null)
        {
#if DEBUG
            Logger.WriteLine(Source.NodeUptimeAndMisc, request.Method);
#endif

            return base.InterceptSendRequestAsync(interceptedSendRequestAsync, request, route);
        }

        public override Task<object> InterceptSendRequestAsync<T>(Func<RpcRequest, string, Task<T>> interceptedSendRequestAsync, RpcRequest request, string route = null)
        {
#if DEBUG
            Logger.WriteLine(Source.NodeUptimeAndMisc, request.Method);
#endif

            return base.InterceptSendRequestAsync(interceptedSendRequestAsync, request, route);
        }

        public override Task<object> InterceptSendRequestAsync<T>(Func<string, string, object[], Task<T>> interceptedSendRequestAsync, string method, string route = null,
            params object[] paramList)
        {
#if DEBUG
            Logger.WriteLine(Source.NodeUptimeAndMisc, method);
#endif

            return base.InterceptSendRequestAsync(interceptedSendRequestAsync, method, route, paramList);
        }

        public override Task InterceptSendRequestAsync(Func<string, string, object[], Task> interceptedSendRequestAsync, string method, string route = null,
            params object[] paramList)
        {
#if DEBUG
            Logger.WriteLine(Source.NodeUptimeAndMisc, method);
#endif

            return base.InterceptSendRequestAsync(interceptedSendRequestAsync, method, route, paramList);
        }
    }
}