using System;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using OTHub.BackendSync.Database.Models;
using OTHub.Settings.Helpers;

namespace OTHub.BackendSync.Blockchain
{
    public static class BlockHelper
    {
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        public static async Task<EthBlock> GetBlock(MySqlConnection connection, string blockHash, HexBigInteger blockNumber, Web3 cl, int blockchainID)
        {
            var block = await EthBlock.GetByNumber(connection, (UInt64)blockNumber.Value, blockchainID);

            if (block == null)
            {
                await _semaphore.WaitAsync();
                try
                {
                    block = await EthBlock.GetByNumber(connection, (UInt64)blockNumber.Value, blockchainID);

                    if (block == null)
                    {
                        var apiBlock = await cl.Eth.Blocks.GetBlockWithTransactionsHashesByNumber.SendRequestAsync(blockNumber);
                        block = new EthBlock
                        {
                            BlockHash = blockHash,
                            BlockNumber = (UInt64)blockNumber.Value,
                            Timestamp = TimestampHelper.UnixTimeStampToDateTime((double)apiBlock.Timestamp.Value),
                            BlockchainID = blockchainID
                        };

                        EthBlock.Insert(connection, block);
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            return block;
        }
    }
}
