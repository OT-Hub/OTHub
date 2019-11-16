using System;
using System.Numerics;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using OTHub.APIServer.Models;
using OTHub.Settings;

namespace OTHub.APIServer
{
    public static class BlockchainHelper
    {
        private static Web3 cl = new Web3(OTHubSettings.Instance.Infura.Url);

        public static async Task<BeforePayoutResult> CanTryPayout(string identity, string offerId)
        {
            var holdingStorageAbi = Program.GetContractAbi(ContractType.HoldingStorage);

            using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var holdingStorageAddress = connection.QuerySingle<ContractAddress>(@"select Address from otcontract
where Type = 5 AND IsLatest = 1 AND IsArchived = 0").Address;

                var offerIdArray = offerId.HexToByteArray();

                var holdingStorageContract =
                    new Contract(new EthApiService(cl.Client), holdingStorageAbi,
                        holdingStorageAddress);
                var getHolderStakedAmountFunction = holdingStorageContract.GetFunction("getHolderStakedAmount");
                var getOfferStartTimeFunction = holdingStorageContract.GetFunction("getOfferStartTime");
                var getOfferHoldingTimeInMinutesFunction =
                    holdingStorageContract.GetFunction("getOfferHoldingTimeInMinutes");
                var getHolderPaidAmountFunction = holdingStorageContract.GetFunction("getHolderPaidAmount");

                var amountToTransfer = await getHolderStakedAmountFunction.CallAsync<BigInteger>(offerIdArray, identity);

                if (amountToTransfer <= 0)
                {
                    return new BeforePayoutResult
                    {
                        CanTryPayout = false,
                        Header = "Stop!",
                        Message = "The smart contract says this identity did not hold data for this job. The transaction will likely fail if you try to send this manually."
                    };
                }

                var getOfferStartTime = await getOfferStartTimeFunction.CallAsync<BigInteger>(offerIdArray);
                var getOfferHoldingTimeInMinutes = await getOfferHoldingTimeInMinutesFunction.CallAsync<BigInteger>(offerIdArray);

                var latestBlockParam = BlockParameter.CreateLatest();

                var block = await cl.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(latestBlockParam);

                DateTime blockTimestamp = UnixTimeStampToDateTime((double) block.Timestamp.Value);
                DateTime offerStartTime = UnixTimeStampToDateTime((UInt64)getOfferStartTime);

                DateTime offerEndTime = offerStartTime.AddMinutes((UInt64)getOfferHoldingTimeInMinutes);

                if (blockTimestamp < offerEndTime)
                {
                    return new BeforePayoutResult
                    {
                        CanTryPayout = false,
                        Header = "Stop!",
                        Message = "The smart contract says this job is still active. The transaction will likely fail if you try to send this manually."
                    };
                }

                var getHolderPaidAmount = await getHolderPaidAmountFunction.CallAsync<BigInteger>(offerIdArray, identity);

                if (getHolderPaidAmount != 0)
                {
                    var friendlyAmount = getHolderPaidAmount / 1000000000000000000;

                    return new BeforePayoutResult
                    {
                        CanTryPayout = false,
                        Header = "Stop!",
                        Message = "The smart contract says you have been paid " + friendlyAmount + " TRAC for this job. The transaction will likely fail if you try to send this manually."
                    };
                }

                if (OTHubSettings.Instance.Blockchain.Network == BlockchainNetwork.Testnet)
                {
                    var litigationStorageAddress = connection.QuerySingle<ContractAddress>(@"select Address from otcontract
where Type = 9 AND IsLatest = 1 AND IsArchived = 0").Address;

                    Contract storageContract = new Contract((EthApiService)cl.Eth, Program.GetContractAbi(ContractType.LitigationStorage), litigationStorageAddress);
                    Function getLitigationStatusFunction = storageContract.GetFunction("getLitigationStatus");

                    Function getLitigationTimestampFunction = storageContract.GetFunction("getLitigationTimestamp");
                    BigInteger litigationTimestampInt = await getLitigationTimestampFunction.CallAsync<BigInteger>(latestBlockParam, offerIdArray, identity);
                    DateTime litigationTimestamp = UnixTimeStampToDateTime((UInt64)litigationTimestampInt);

                    Function getOfferLitigationIntervalInMinutesFunction = holdingStorageContract.GetFunction("getOfferLitigationIntervalInMinutes");
                    BigInteger litgationInterval = await getOfferLitigationIntervalInMinutesFunction.CallAsync<BigInteger>(latestBlockParam, offerIdArray) * 60;



                    var status = await getLitigationStatusFunction.CallAsync<UInt16>(latestBlockParam, offerIdArray, identity);

                    if (status == 1) //initiated
                    {
                        if (litigationTimestampInt + (litgationInterval * 2) >= block.Timestamp.Value)
                        {
                            return new BeforePayoutResult
                            {
                                CanTryPayout = false,
                                Header = "Stop!",
                                Message = "The smart contract says 'Unanswered litigation in progress, cannot pay out'. The transaction will likely fail if you try to send this manually."
                            };
                        }
                    }
                    else if (status == 2) //answered
                    {
                        if (litigationTimestampInt + (litgationInterval) >= block.Timestamp.Value)
                        {
                            return new BeforePayoutResult
                            {
                                CanTryPayout = false,
                                Header = "Stop!",
                                Message = "The smart contract says 'Unanswered litigation in progress, cannot pay out'. The transaction will likely fail if you try to send this manually."
                            };
                        }
                    }
                    else if (status == 0) //completed
                    {
                        //Do nothing as this is fine
                    }
                    else
                    {
                        return new BeforePayoutResult
                        {
                            CanTryPayout = false,
                            Header = "Stop!",
                            Message = "The smart contract says 'Data holder is replaced or being replaced, cannot payout!'. The transaction will likely fail if you try to send this manually."
                        };
                    }
                }

                return new BeforePayoutResult
                {
                    CanTryPayout = true
                };
            }
        }

        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
    }
}