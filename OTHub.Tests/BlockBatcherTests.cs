using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OTHub.BackendSync.Blockchain;
using OTHub.Settings.Abis;
using Xunit;

namespace OTHub.Tests
{
    public class BlockBatcherTests
    {
        [Theory]
        [InlineData(0, 0, 0, 0, 0)]
        [InlineData(0, 1, 1, 1, 1)]
        [InlineData(0, 1, 100, 1, 1)]
        [InlineData(1000, 2000, 100, 10, 100)]
        [InlineData(1001, 2000, 100, 10, 100)]
        [InlineData(1001, 1999, 100, 10, 100)]
        public async Task Test(ulong start, ulong end, ulong batchSize, ulong batchHitTimes, ulong adjustedBatchSizeResult)
        {
            var batch = BlockBatcher.Start(start, end, batchSize, ExecuteBatch);

            await batch.Execute();

            Assert.Equal(batchHitTimes, _counter);
            Assert.Equal(adjustedBatchSizeResult, batch.BatchSize);
            Assert.Equal(end, batch.CurrentEnd);
        }
        
        private ulong _counter;

        private async Task ExecuteBatch(ulong start, ulong end)
        {
            _counter++;
        }
    }
}
