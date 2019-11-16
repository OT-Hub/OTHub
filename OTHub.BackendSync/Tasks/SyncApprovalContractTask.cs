using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using OTHelperNetStandard.Models.Database;
using OTHub.Settings;

namespace OTHelperNetStandard.Tasks
{
    public class SyncApprovalContractTask : TaskRun
    {
        public override async Task Execute(Source source)
        {
            if (OTHubSettings.Instance.Blockchain.Network == BlockchainNetwork.Testnet)
            {
                return;
            }

            ClientBase.ConnectionTimeout = new TimeSpan(0, 0, 5, 0);

            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {

                foreach (var contract in OTContract.GetByType(connection, (int) ContractType.Approval))
                {
                    Logger.WriteLine(source, "     Using contract: " + contract.Address);
                    var approvalContract = new Contract(eth, Constants.GetContractAbi(ContractType.Approval), contract.Address);
                    var nodeApprovedEvent = approvalContract.GetEvent("NodeApproved");
                    var nodeRemovedEvent = approvalContract.GetEvent("NodeRemoved");

            
                    var toBlock = new BlockParameter(LatestBlockNumber);

                    var nodeApprovedEvents = await nodeApprovedEvent.GetAllChangesDefault(
                        nodeApprovedEvent.CreateFilterInput(new BlockParameter(contract.SyncBlockNumber), toBlock));

              
                    var nodeRemovedEvents = await nodeRemovedEvent.GetAllChangesDefault(
                        nodeRemovedEvent.CreateFilterInput(new BlockParameter(contract.SyncBlockNumber), toBlock));

                    foreach (EventLog<List<ParameterOutput>> eventLog in nodeApprovedEvents)
                    {
                        var block = await Program.GetEthBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber,
                            cl);

                        string nodeId = HexHelper.ByteArrayToString((byte[]) eventLog.Event
                            .FirstOrDefault(p => p.Parameter.Name == "nodeId").Result, false);

                        var model = new OTContract_Approval_NodeApproved
                        {
                            BlockNumber = (UInt64) eventLog.Log.BlockNumber.Value,
                            TransactionHash = eventLog.Log.TransactionHash,
                            Timestamp = block.Timestamp,
                            ContractAddress = contract.Address,
                            NodeId = nodeId
                        };

                        OTContract_Approval_NodeApproved.InsertIfNotExist(connection, model);
                    }

                    foreach (EventLog<List<ParameterOutput>> eventLog in nodeRemovedEvents)
                    {
                        var block = await Program.GetEthBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber,
                            cl);

                        string nodeId = HexHelper.ByteArrayToString((byte[]) eventLog.Event
                            .FirstOrDefault(p => p.Parameter.Name == "nodeId").Result, false);

                        OTContract_Approval_NodeRemoved.InsertIfNotExist(connection,
                            new OTContract_Approval_NodeRemoved
                            {
                                BlockNumber = (UInt64) eventLog.Log.BlockNumber.Value,
                                TransactionHash = eventLog.Log.TransactionHash,
                                Timestamp = block.Timestamp,
                                ContractAddress = contract.Address,
                                NodeId = nodeId
                            });
                    }

                    contract.LastSyncedTimestamp = DateTime.Now;
                    contract.SyncBlockNumber = (ulong) toBlock.BlockNumber.Value;

                    OTContract.Update(connection, contract, false, false);
                }
            }
        }

        public SyncApprovalContractTask() : base("Sync Approval Contract")
        {
        }
    }
}