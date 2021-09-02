using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ComposableAsync;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using OTHub.Settings;
using RateLimiter;

namespace OTHub.BackendSync.Logging
{
    public static class RPCInterceptorManager
    {
        public class RPCInterceptorState
        {
            private CountByIntervalAwaitableConstraint GeneralConstraintSingle { get; set; }
            private CountByIntervalAwaitableConstraint GeneralConstraintMulti { get; set; }
            public TimeLimiter TimeConstraint { get; }
            public TimeLimiter TimeConstraintLogs { get; }

            public RPCInterceptorState()
            {
                GeneralConstraintMulti = new CountByIntervalAwaitableConstraint(2, TimeSpan.FromSeconds(1));
                GeneralConstraintSingle = new CountByIntervalAwaitableConstraint(1, TimeSpan.FromMilliseconds(400));
                TimeConstraint = TimeLimiter.Compose(GeneralConstraintSingle, GeneralConstraintMulti);
                TimeConstraintLogs = TimeLimiter.Compose(new CountByIntervalAwaitableConstraint(1, TimeSpan.FromSeconds(2)));
            }
        }

        private static readonly Dictionary<BlockchainType, RPCInterceptorState> _dictionary = new Dictionary<BlockchainType, RPCInterceptorState>();


        public static RPCInterceptorState AddOrGet(BlockchainType type)
        {
            lock (_dictionary)
            {
                if (_dictionary.TryGetValue(type, out RPCInterceptorState state))
                {
                    return state;
                }

                state = new RPCInterceptorState();

                _dictionary[type] = state;

                return state;
            }
        }
    }

    public class RPCInterceptor : RequestInterceptor
    {
        private readonly BlockchainType _type;

        public RPCInterceptor(BlockchainType type)
        {
            _type = type;

            State = RPCInterceptorManager.AddOrGet(type);
        }

        private RPCInterceptorManager.RPCInterceptorState State { get; }


        public override async Task InterceptSendRequestAsync(Func<RpcRequest, string, Task> interceptedSendRequestAsync, RpcRequest request, string route = null)
        {
            await State.TimeConstraint;

#if DEBUG
            Logger.WriteLine(Source.Misc, _type + ": " + request.Method);
#endif

            await base.InterceptSendRequestAsync(interceptedSendRequestAsync, request, route);
        }

        public override async Task<object> InterceptSendRequestAsync<T>(Func<RpcRequest, string, Task<T>> interceptedSendRequestAsync, RpcRequest request, string route = null)
        {
            await State.TimeConstraint;

            string additional = null;

            if (request.Method == "eth_getLogs")
            {
                await State.TimeConstraintLogs;
            }

            foreach (var requestRawParameter in request.RawParameters)
            {
                if (requestRawParameter is NewFilterInput raw)
                {
                    additional += " " + raw.FromBlock.BlockNumber + " to " + raw.ToBlock.BlockNumber + " on address " + raw.Address.FirstOrDefault();
                }
                else
                {
                    
                }
            }

#if DEBUG
            Logger.WriteLine(Source.Misc, _type + ": " + request.Method + additional);
#endif

            object response;
            bool hasFailedOnce = false;

            start:
            try
            {
                response = await base.InterceptSendRequestAsync(interceptedSendRequestAsync, request, route);
            }
            catch (RpcResponseException ex) when(!hasFailedOnce)
            {
                hasFailedOnce = true;

                if (ex.Message.ToLower().Contains("internal"))
                {
                    await Task.Delay(250);
                    goto start;
                }

                throw;
            }



            return response;
        }

        public override async Task<object> InterceptSendRequestAsync<T>(Func<string, string, object[], Task<T>> interceptedSendRequestAsync, string method, string route = null,
            params object[] paramList)
        {
            await State.TimeConstraint;

#if DEBUG
            Logger.WriteLine(Source.Misc, _type + ": " + method);
#endif

            return await base.InterceptSendRequestAsync(interceptedSendRequestAsync, method, route, paramList);
        }

        public override async Task InterceptSendRequestAsync(Func<string, string, object[], Task> interceptedSendRequestAsync, string method, string route = null,
            params object[] paramList)
        {
            await State.TimeConstraint;

#if DEBUG
            Logger.WriteLine(Source.Misc, _type + ": " + method);
#endif

            await base.InterceptSendRequestAsync(interceptedSendRequestAsync, method, route, paramList);
        }
    }
}