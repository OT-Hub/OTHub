using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nethereum.JsonRpc.Client;
using OTHub.BackendSync.Logging;

namespace OTHub.BackendSync.Blockchain
{
    public class BlockBatcher
    {
        private readonly ulong _start;
        private readonly ulong _end;
        private ulong _batchSize;
        private readonly ProcessBlockBatchDelegate _action;

        private ulong _currentStart;
        private ulong _currentEnd;

        private ulong _batchSizedReducedCount;

        private BlockBatcher(in ulong start, in ulong end, ulong batchSize,
            ProcessBlockBatchDelegate action)
        {
            _start = _currentStart = start;
            _end = end;
            _batchSize = batchSize;
            _action = action;
        }

        public ulong BatchSize => _batchSize;

        public ulong CurrentEnd => _currentEnd;

        public async Task Execute()
        {
            if (_start > _end)
            {
                throw new NotSupportedException($"Start block {_start} is after {_end} end block for syncing.");
            }

            if (_start == _end)
                return;

            var range = _end - _start;

            if (range < (ulong)_batchSize)
            {
                _batchSize = range;
            }

            bool shouldBreak = false;

            _currentEnd = _currentStart + (ulong)_batchSize;

            while (_currentStart < _end && !shouldBreak)
            {
                startOfLoop:
                _currentEnd = _currentStart + (ulong) _batchSize;

                if (_currentEnd > _end)
                {
                    _currentEnd = _end;
                    shouldBreak = true;
                }

                bool canRetry = true;

                startOfRPC:
                try
                {

                    await _action(_currentStart, _currentEnd);
                }
                catch (RpcResponseException ex) when (ex.Message.Contains("query returned more than"))
                {
                    if (_batchSize > 50 && _batchSizedReducedCount < 20)
                    {
                        _batchSizedReducedCount++;
                        _batchSize = _batchSize / 2;
                        goto startOfLoop;
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (RpcClientUnknownException ex) when (canRetry && ex.GetBaseException().Message.Contains("Gateway")
                )
                {
                    canRetry = false;
                    goto startOfRPC;
                }

                _currentStart = _currentEnd;
            }
        }

        public static BlockBatcher Start(ulong start, ulong end, ulong batchSize, ProcessBlockBatchDelegate action)
        {
            return new BlockBatcher(start, end, batchSize, action);
        }

        public delegate Task ProcessBlockBatchDelegate(ulong start, ulong end);
    }
}