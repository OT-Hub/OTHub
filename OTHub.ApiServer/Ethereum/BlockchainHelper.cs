using System;
using System.Numerics;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using OTHub.APIServer.Sql;
using OTHub.APIServer.Sql.Models.Contracts;
using OTHub.APIServer.Sql.Models.Nodes;
using OTHub.Settings;
using OTHub.Settings.Abis;
using OTHub.Settings.Helpers;

namespace OTHub.APIServer.Ethereum
{
    public static class BlockchainHelper
    {
        public static async Task<BeforePayoutResult> CanTryPayout(string identity, string offerId, string holdingAddress, string holdingStorageAddress, string litigationStorageAddress)
        {
            using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var holdingStorageAddressModel = connection.QueryFirstOrDefault<ContractAddress>(ContractsSql.GetHoldingStorageAddressByAddress, new
                {
                    holdingStorageAddress = holdingStorageAddress
                });

                int blockchainID = holdingStorageAddressModel.BlockchainID;
                holdingStorageAddress = holdingStorageAddressModel.Address;

                if (holdingStorageAddress == null)
                {
                    return new BeforePayoutResult
                    {
                        CanTryPayout = false,
                        Header = "Stop!",
                        Message = "OT Hub is not familiar with this holding storage smart contract address. Unknown addresses can not be used."
                    };
                }

                var blockchainRow = connection.QueryFirst("SELECT * FROM blockchains where id = @id", new {id = blockchainID});
                string blockchainName = blockchainRow.BlockchainName;
                string networkName = blockchainRow.NetworkName;

                BlockchainType blockchainEnum = Enum.Parse<BlockchainType>(blockchainName);
                BlockchainNetwork networkNameEnum = Enum.Parse<BlockchainNetwork>(networkName);

                string nodeUrl = connection.ExecuteScalar<string>(@"SELECT BlockchainNodeUrl FROM blockchains WHERE id = @id", new
                {
                    id = blockchainID
                });

                var cl = new Web3(nodeUrl);

                var holdingStorageAbi = AbiHelper.GetContractAbi(ContractTypeEnum.HoldingStorage, blockchainEnum, networkNameEnum);

                holdingAddress = connection.QueryFirstOrDefault<ContractAddress>(ContractsSql.GetHoldingAddressByAddress, new
                {
                    holdingAddress = holdingAddress
                })?.Address;

                if (holdingAddress == null)
                {
                    return new BeforePayoutResult
                    {
                        CanTryPayout = false,
                        Header = "Stop!",
                        Message = "OT Hub is not familiar with this holding smart contract address. Unknown addresses can not be used."
                    };
                }

                var offerIdArray = offerId.HexToByteArray();

                var holdingStorageContract =
                    new Contract(new EthApiService(cl.Client), holdingStorageAbi,
                        holdingStorageAddress);
                var getHolderStakedAmountFunction = holdingStorageContract.GetFunction("getHolderStakedAmount");
                //var getOfferStartTimeFunction = holdingStorageContract.GetFunction("getOfferStartTime");
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

                //var getOfferStartTime = await getOfferStartTimeFunction.CallAsync<BigInteger>(offerIdArray);
                var getOfferHoldingTimeInMinutes = await getOfferHoldingTimeInMinutesFunction.CallAsync<BigInteger>(offerIdArray);

                var latestBlockParam = BlockParameter.CreateLatest();

                var block = await cl.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(latestBlockParam);

                //DateTime blockTimestamp = UnixTimeStampToDateTime((double) block.Timestamp.Value);
                //DateTime offerStartTime = UnixTimeStampToDateTime((UInt64)getOfferStartTime);

                //DateTime offerEndTime = offerStartTime.AddMinutes((UInt64)getOfferHoldingTimeInMinutes);

                //if (blockTimestamp < offerEndTime)
                //{
                //    return new BeforePayoutResult
                //    {
                //        CanTryPayout = false,
                //        Header = "Stop!",
                //        Message = "The smart contract says this job is still active. The transaction will likely fail if you try to send this manually."
                //    };
                //}

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

                if (!String.IsNullOrWhiteSpace(litigationStorageAddress))
                {
                    //if (OTHubSettings.Instance.Blockchain.Network == BlockchainNetwork.Testnet)
                    //{
                    litigationStorageAddress = connection.QueryFirstOrDefault<ContractAddress>(@"select Address from otcontract
where Type = 9 AND Address = @litigationStorageAddress", new
                    {
                        litigationStorageAddress = litigationStorageAddress
                    })?.Address;

                    if (litigationStorageAddress == null)
                    {
                        return new BeforePayoutResult
                        {
                            CanTryPayout = false,
                            Header = "Stop!",
                            Message = "OT Hub is not familiar with this litigation storage smart contract address. Unknown addresses can not be used."
                        };
                    }

                    Contract storageContract = new Contract((EthApiService) cl.Eth,
                        AbiHelper.GetContractAbi(ContractTypeEnum.LitigationStorage, blockchainEnum, networkNameEnum), litigationStorageAddress);
                    Function getLitigationStatusFunction = storageContract.GetFunction("getLitigationStatus");

                    Function getLitigationTimestampFunction = storageContract.GetFunction("getLitigationTimestamp");
                    BigInteger litigationTimestampInt =
                        await getLitigationTimestampFunction.CallAsync<BigInteger>(latestBlockParam, offerIdArray,
                            identity);
                    DateTime litigationTimestamp = TimestampHelper.UnixTimeStampToDateTime((UInt64) litigationTimestampInt);

                    Function getOfferLitigationIntervalInMinutesFunction =
                        holdingStorageContract.GetFunction("getOfferLitigationIntervalInMinutes");
                    BigInteger litgationInterval =
                        await getOfferLitigationIntervalInMinutesFunction.CallAsync<BigInteger>(latestBlockParam,
                            offerIdArray) * 60;



                    var status =
                        await getLitigationStatusFunction.CallAsync<UInt16>(latestBlockParam, offerIdArray, identity);

                    if (status == 1) //initiated
                    {
                        if (litigationTimestampInt + (litgationInterval * 2) >= block.Timestamp.Value)
                        {
                            return new BeforePayoutResult
                            {
                                CanTryPayout = false,
                                Header = "Stop!",
                                Message =
                                    "The smart contract says 'Unanswered litigation in progress, cannot pay out'. The transaction will likely fail if you try to send this manually."
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
                                Message =
                                    "The smart contract says 'Unanswered litigation in progress, cannot pay out'. The transaction will likely fail if you try to send this manually."
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
                            Message =
                                "The smart contract says 'Data holder is replaced or being replaced, cannot payout!'. The transaction will likely fail if you try to send this manually."
                        };
                    }
                }
                //}

                return new BeforePayoutResult
                {
                    CanTryPayout = true
                };
            }
        }
    }
}