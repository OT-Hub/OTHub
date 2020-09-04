using System;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using OTHub.BackendSync.Database.Models;
using OTHub.BackendSync.Logging;
using OTHub.Settings;

namespace OTHub.BackendSync.Ethereum.Tasks
{
    public class BlockchainSyncTask : TaskRun
    {
        public BlockchainSyncTask() : base("Blockchain Sync")
        {
            Add(new SyncProfileContractTask());
            Add(new SyncHoldingContractTask());
            Add(new SyncLitigationContractTask());
            Add(new SyncReplacementContractTask());
        }

        public override async Task Execute(Source source)
        {
            await RunChildren(source);

            using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                OTContract_Holding_OfferCreated[] offersToAdd =
                    OTContract_Holding_OfferCreated.GetUnprocessed(connection);

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
                        TokenAmountPerHolder = offerToAdd.TokenAmountPerHolder
                    };

                    OTOffer.InsertIfNotExist(connection, offer);

                    OTContract_Holding_OfferCreated.SetProcessed(connection, offerToAdd);
                }

                OTContract_Holding_OfferFinalized[] offersToFinalize =
                    OTContract_Holding_OfferFinalized.GetUnprocessed(connection);

                if (offersToFinalize.Any())
                {
                    Console.WriteLine("Found " + offersToFinalize.Length + " unprocessed offer finalized events.");
                }

                foreach (var offerToFinalize in offersToFinalize)
                {
                    OTOffer.FinalizeOffer(connection, offerToFinalize.OfferID, offerToFinalize.BlockNumber,
                        offerToFinalize.TransactionHash, offerToFinalize.Holder1, offerToFinalize.Holder2,
                        offerToFinalize.Holder3, offerToFinalize.Timestamp);

                    OTContract_Holding_OfferFinalized.SetProcessed(connection, offerToFinalize);
                }
            }
        }
    }
}