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
        public static async Task<BeforePayoutResult> CanTryPayout(string nodeID, string offerId, string holdingAddress,
            string holdingStorageAddress, string litigationStorageAddress, string identity, int? blockchainID)
        {
            if (nodeID == null || offerId == null || holdingAddress == null || holdingStorageAddress == null || litigationStorageAddress == null || identity == null || blockchainID == null)
            {
                return new BeforePayoutResult
                {
                    CanTryPayout = false,
                    Header = "Stop!",
                    Message = "Missing data in request."
                };
            }

            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var holdingStorageAddressModel = await connection.QueryFirstOrDefaultAsync<ContractAddress>(ContractsSql.GetHoldingStorageAddressByAddress, new
                {
                    holdingStorageAddress = holdingStorageAddress,
                    blockchainID
                });

                holdingStorageAddress = holdingStorageAddressModel?.Address;

                if (holdingStorageAddress == null)
                {
                    return new BeforePayoutResult
                    {
                        CanTryPayout = false,
                        Header = "Stop!",
                        Message = "OT Hub is not familiar with this holding storage smart contract address for this blockchain id " + blockchainID
                    };
                }

                var blockchainRow = await connection.QueryFirstAsync("SELECT * FROM blockchains where id = @id", new {id = blockchainID});
                string blockchainName = blockchainRow.BlockchainName;
                string networkName = blockchainRow.NetworkName;
                string explorerTransactionUrl = blockchainRow.TransactionUrl;

                BlockchainType blockchainEnum = Enum.Parse<BlockchainType>(blockchainName);
                BlockchainNetwork networkNameEnum = Enum.Parse<BlockchainNetwork>(networkName);

                string nodeUrl = connection.ExecuteScalar<string>(@"SELECT BlockchainNodeUrl FROM blockchains WHERE id = @id", new
                {
                    id = blockchainID
                });

                var cl = new Web3(nodeUrl);

                var holdingStorageAbi = AbiHelper.GetContractAbi(ContractTypeEnum.HoldingStorage, blockchainEnum, networkNameEnum);

                holdingAddress = (await connection.QueryFirstOrDefaultAsync<ContractAddress>(ContractsSql.GetHoldingAddressByAddress, new
                {
                    holdingAddress = holdingAddress,
                    blockchainID
                }))?.Address;

                if (holdingAddress == null)
                {
                    return new BeforePayoutResult
                    {
                        CanTryPayout = false,
                        Header = "Stop!",
                        Message = "OT Hub is not familiar with this holding smart contract address for this blockchain id " + blockchainID
                    };
                }

                var offerIdArray = offerId.HexToByteArray();

                var holdingStorageContract =
                    new Contract(new EthApiService(cl.Client), holdingStorageAbi,
                        holdingStorageAddress);
                var getHolderStakedAmountFunction = holdingStorageContract.GetFunction("getHolderStakedAmount");
                var holderStakedAmount = await getHolderStakedAmountFunction.CallAsync<BigInteger>(offerIdArray, identity);

                var getHolderPaymentTimestampFunction = holdingStorageContract.GetFunction("getHolderPaymentTimestamp");
                var holderPaymentTimestamp = await getHolderPaymentTimestampFunction.CallAsync<BigInteger>(offerIdArray, identity);

                var getOfferHoldingTimeInMinutesFunction = holdingStorageContract.GetFunction("getOfferHoldingTimeInMinutes");
                var offerHoldingTimeInMinutes = await getOfferHoldingTimeInMinutesFunction.CallAsync<BigInteger>(offerIdArray);

                var getHolderPaidAmountFunction = holdingStorageContract.GetFunction("getHolderPaidAmount");
                var holderPaidAmount = await getHolderPaidAmountFunction.CallAsync<BigInteger>(offerIdArray, identity);



                if (holderStakedAmount <= 0)
                {
                    return new BeforePayoutResult
                    {
                        CanTryPayout = false,
                        Header = "Stop!",
                        Message = "The smart contract says this identity did not hold data for this job. The transaction will likely fail if you try to send this manually."
                    };
                }


                //long holdingTime = await connection.ExecuteScalarAsync<long>(
                //    @"select HoldingTimeInMinutes from otoffer where OfferID = @offerID and blockchainID = @blockchainID",
                //    new
                //    {
                //        offerId,
                //        blockchainID
                //    });

                var latestBlockParam = BlockParameter.CreateLatest();

                var block = await cl.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(latestBlockParam);


                //var amountToTransfer = holderStakedAmount;
                //amountToTransfer = amountToTransfer * (block.Timestamp - holderPaymentTimestamp);
                //amountToTransfer = amountToTransfer / (offerHoldingTimeInMinutes * 60);

                //if (amountToTransfer + holderPaidAmount >= holderStakedAmount)
                //{

                //}

                //var tt = Web3.Convert.FromWei(amountToTransfer);



                if (holderPaidAmount == holderStakedAmount)
                {
                    var friendlyAmount = Web3.Convert.FromWei(holderPaidAmount);

                    return new BeforePayoutResult
                    {
                        CanTryPayout = false,
                        Header = "Stop!",
                        Message = "The smart contract says you have been paid " + friendlyAmount +
                                  " TRAC for this job. The transaction will likely fail if you try to send this manually."
                    };
                }

                if (!String.IsNullOrWhiteSpace(litigationStorageAddress))
                {

                    litigationStorageAddress = (await connection.QueryFirstOrDefaultAsync<ContractAddress>(@"select Address from otcontract
where Type = 9 AND Address = @litigationStorageAddress AND blockchainID = @blockchainID", new
                    {
                        litigationStorageAddress = litigationStorageAddress,
                        blockchainID
                    }))?.Address;

                    if (litigationStorageAddress == null)
                    {
                        return new BeforePayoutResult
                        {
                            CanTryPayout = false,
                            Header = "Stop!",
                            Message = "OT Hub is not familiar with this litigation storage smart contract address for this blockchain id " + blockchainID
                        };
                    }

                    Contract storageContract = new Contract((EthApiService)cl.Eth,
                        AbiHelper.GetContractAbi(ContractTypeEnum.LitigationStorage, blockchainEnum, networkNameEnum), litigationStorageAddress);
                    Function getLitigationStatusFunction = storageContract.GetFunction("getLitigationStatus");

                    Function getLitigationTimestampFunction = storageContract.GetFunction("getLitigationTimestamp");
                    BigInteger litigationTimestampInt =
                        await getLitigationTimestampFunction.CallAsync<BigInteger>(latestBlockParam, offerIdArray,
                            identity);

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

                return new BeforePayoutResult
                {
                    CanTryPayout = true,
                    BlockchainExplorerUrlFormat = explorerTransactionUrl
                };
            }
        }
    }
}