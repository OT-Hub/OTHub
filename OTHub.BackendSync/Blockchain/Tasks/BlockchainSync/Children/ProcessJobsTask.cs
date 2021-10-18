
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using OTHub.BackendSync.Database.Models;
using OTHub.BackendSync.Logging;
using OTHub.BackendSync.Messaging;
using OTHub.Messaging;
using OTHub.Settings;
using OTHub.Settings.Constants;

namespace OTHub.BackendSync.Blockchain.Tasks.BlockchainSync.Children
{
    public class ProcessJobsTask : TaskRunBlockchain
    {
        public ProcessJobsTask() : base(TaskNames.ProcessJobs)
        {
        }

        public override async Task<bool> Execute(Source source, BlockchainType blockchain, BlockchainNetwork network)
        {
            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                int blockchainID = await GetBlockchainID(connection, blockchain, network);
                await Execute(connection, blockchainID, blockchain, network);
            }

            return true;
        }


        private const int MinutesInDay = 60 * 24;
        private const int BasePayoutGas = 150000;

        private static (decimal lambda, int confidence) GetPriceFactor(ulong offerHoldingTimeInMinutes,
            decimal offerTokenAmountPerHolder, ulong gasPrice, ulong offerDataSetSizeInBytes)
        {
            double holdingTimeInDays = (double)offerHoldingTimeInMinutes / MinutesInDay;
            double dataSizeInMB = (double)offerDataSetSizeInBytes / 1000000;

            decimal tracInBaseCurrency = 0.4m; //This is hard coded for xdai and polygon as seen in otnode config.json. Parachain has a different value as a warning
            decimal gasPriceInGwei = (decimal)gasPrice / 1000000000;

            decimal basePayoutInBaseCurrency = (BasePayoutGas * gasPriceInGwei) / 1000000000m;
            decimal basePayoutCostInTrac = basePayoutInBaseCurrency / tracInBaseCurrency;

            decimal sqrt = (decimal)Math.Sqrt(2 * holdingTimeInDays * dataSizeInMB);

            decimal[,,] priceFactorResults = new decimal[10, 10, 9];


            bool canBreak = false;
            //Try all price factors from 0 -> 10 using 0.01 increments e.g. 0.01, 0.01, 0.02, 0.03
            for (int i = 0; i < 10; i++)
            {
                if (canBreak)
                    break;

                for (int j = 0; j < 10; j++)
                {
                    if (canBreak)
                        break;

                    for (int k = 1; k < 10; k++)
                    {
                        string strPriceFactor = $"{i}.{j}{k}";
                        decimal priceFactor = decimal.Parse(strPriceFactor);
                        decimal price = (2m * basePayoutCostInTrac) + (priceFactor * sqrt);
                        priceFactorResults[i, j, k - 1] = price;

                        if (price > offerTokenAmountPerHolder)
                        {
                            canBreak = true;
                            break;
                        }
                    }

                }
            }

            (int x, int y, int z) currentMatchedPriceFactorIndex = default;
            decimal bestDiff = decimal.MaxValue;

            canBreak = false;
            //Try find the closet matched price factor
            for (int i = 0; i < 10; i++)
            {
                if (canBreak)
                    break;

                for (int j = 0; j < 10; j++)
                {
                    for (int k = 0; k < 9; k++)
                    {
                        decimal value = priceFactorResults[i, j, k];

                        if (value == 0)
                        {
                            canBreak = true;
                            break;
                        }

                        decimal diff = Math.Abs(value - offerTokenAmountPerHolder);

                        if (diff < bestDiff)
                        {
                            bestDiff = diff;
                            currentMatchedPriceFactorIndex = (i, j, k);
                        }
                    }
                }
            }

            var priceFactorAmount = priceFactorResults[currentMatchedPriceFactorIndex.x,
                currentMatchedPriceFactorIndex.y, currentMatchedPriceFactorIndex.z];
            var match = Math.Round(
                Math.Abs(((priceFactorAmount -
                           offerTokenAmountPerHolder) / offerTokenAmountPerHolder) * 100),
                MidpointRounding.ToEven);

            match = Math.Round((decimal)Math.Sqrt((double)match * 2), MidpointRounding.ToEven);

            return (Decimal.Parse(currentMatchedPriceFactorIndex.x + "." + currentMatchedPriceFactorIndex.y + (currentMatchedPriceFactorIndex.z + 1)), 100 - (int)match);
        }

        public static async Task Execute(MySqlConnection connection, int blockchainID, BlockchainType blockchain, BlockchainNetwork network)
        {
            using (await LockManager.GetLock(LockType.ProcessJobs).Lock())
            {
                if (blockchain == BlockchainType.xDai || blockchain == BlockchainType.Polygon)
                {
                    OTContract_Holding_OfferCreated[] offersToCalcPriceFactor =
                        OTContract_Holding_OfferCreated.GetWithoutEstimatedPriceFactor(connection, blockchainID);

                    foreach (OTContract_Holding_OfferCreated offer in offersToCalcPriceFactor)
                    {
                        (decimal lambda, int confidence) priceFactor = GetPriceFactor(offer.HoldingTimeInMinutes, offer.TokenAmountPerHolder, offer.GasPrice, offer.DataSetSizeInBytes);

                        await OTOffer.UpdateLambda(connection, offer.BlockchainID, offer.OfferID, priceFactor.lambda, priceFactor.confidence);
                    }
                }

                OTContract_Holding_OfferCreated[] offersToAdd =
                    OTContract_Holding_OfferCreated.GetUnprocessed(connection, blockchainID);

                if (offersToAdd.Any())
                {
                    Console.WriteLine("Found " + offersToAdd.Length + " unprocessed offer created events.");
                }

                foreach (var offerToAdd in offersToAdd)
                {
                    OTOffer offer = new OTOffer
                    {
                        CreatedTimestamp = offerToAdd.Timestamp,
                        OfferID = offerToAdd.OfferID,
                        CreatedTransactionHash = offerToAdd.TransactionHash,
                        CreatedBlockNumber = offerToAdd.BlockNumber,
                        DCNodeId = offerToAdd.DCNodeId,
                        DataSetId = offerToAdd.DataSetId,
                        DataSetSizeInBytes = offerToAdd.DataSetSizeInBytes,
                        HoldingTimeInMinutes = offerToAdd.HoldingTimeInMinutes,
                        IsFinalized = false,
                        LitigationIntervalInMinutes = offerToAdd.LitigationIntervalInMinutes,
                        TransactionIndex = offerToAdd.TransactionIndex,
                        TokenAmountPerHolder = offerToAdd.TokenAmountPerHolder,
                        BlockchainID = blockchainID
                    };


                    //ETH has dynamic tracInBaseCurrency which is more annoying to implement
                    if (blockchain == BlockchainType.xDai || blockchain == BlockchainType.Polygon)
                    {
                        (decimal lambda, int confidence) priceFactor = GetPriceFactor(offer.HoldingTimeInMinutes, offer.TokenAmountPerHolder, offerToAdd.GasPrice, offer.DataSetSizeInBytes);

                        offer.EstimatedLambda = priceFactor.lambda;
                        offer.EstimatedLambdaConfidence = priceFactor.confidence;
                    }

                    OTOffer.InsertIfNotExist(connection, offer);

                    OTContract_Holding_OfferCreated.SetProcessed(connection, offerToAdd);
                }

                OTContract_Holding_OfferFinalized[] offersToFinalize =
                    OTContract_Holding_OfferFinalized.GetUnprocessed(connection, blockchainID);

                if (offersToFinalize.Any())
                {
                    Console.WriteLine("Found " + offersToFinalize.Length + " unprocessed offer finalized events.");
                }

                foreach (OTContract_Holding_OfferFinalized offerToFinalize in offersToFinalize)
                {
                    await OTOffer.FinalizeOffer(connection, offerToFinalize.OfferID, offerToFinalize.BlockNumber,
                        offerToFinalize.TransactionHash, offerToFinalize.Holder1, offerToFinalize.Holder2,
                        offerToFinalize.Holder3, offerToFinalize.Timestamp, blockchainID);

                    OTContract_Holding_OfferFinalized.SetProcessed(connection, offerToFinalize);

                }

                if (offersToFinalize.Any())
                {
                    RabbitMqService.OfferFinalized(offersToFinalize.Select(offerToFinalize =>
                        new OfferFinalizedMessage
                        {
                            OfferID = offerToFinalize.OfferID,
                            BlockchainID = blockchainID,
                            Timestamp = offerToFinalize.Timestamp,
                            Holder1 = offerToFinalize.Holder1,
                            Holder2 = offerToFinalize.Holder2,
                            Holder3 = offerToFinalize.Holder3
                        }));
                }
            }
        }
    }
}