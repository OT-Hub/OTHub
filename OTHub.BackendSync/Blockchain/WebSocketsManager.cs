
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.Contracts;
using Nethereum.JsonRpc.Client;
using Nethereum.JsonRpc.WebSocketStreamingClient;
using Nethereum.RPC;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Reactive.Eth;
using Nethereum.RPC.Reactive.Eth.Subscriptions;
using Nethereum.Web3;
using OTHub.BackendSync.Blockchain.Tasks.BlockchainSync.Children;
using OTHub.BackendSync.Database.Models;
using OTHub.BackendSync.Logging;
using OTHub.Settings;
using OTHub.Settings.Abis;
using OTHub.Settings.Constants;

namespace OTHub.BackendSync.Blockchain
{
    public static class WebSocketsManager
    {
        private static readonly ConcurrentDictionary<int, Subscription> _dictionary = new ConcurrentDictionary<int, Subscription>();
        public static async Task Start(int blockchainID, string webSocketsUrl, string rpcUrl,
            BlockchainType blockchainType, BlockchainNetwork blockchainNetwork)
        {
            bool failed = await Run(blockchainID, webSocketsUrl, rpcUrl, blockchainType, blockchainNetwork);

            if (failed)
            {
                _ = Task.Run(() => Start(blockchainID, webSocketsUrl, rpcUrl, blockchainType, blockchainNetwork));
            }
        }

        private static async Task<bool> Run(int blockchainID, string webSocketsUrl, string rpcUrl, BlockchainType blockchainType,
            BlockchainNetwork blockchainNetwork)
        {
            bool hasFailed = false;

            using (var client = new StreamingWebSocketClient(webSocketsUrl))
            {
                EthLogsObservableSubscription logsSubscription = new EthLogsObservableSubscription(client);

                Web3 cl = new Web3(rpcUrl);

                RequestInterceptor r = new LogRequestInterceptor();
                cl.Client.OverridingRequestInterceptor = r;
                EthApiService eth = new EthApiService(cl.Client);

                logsSubscription.GetSubscriptionDataResponsesAsObservable().Subscribe(async filterLog =>
                {
                    FilterLog transaction = filterLog;

                    if (transaction.Removed)
                        return;

                    if (SmartContractManager.TryGetAddress(blockchainID, filterLog.Address, out ContractTypeEnum type))
                    {
                        await ProcessSmartContractEvent(blockchainID, blockchainType, blockchainNetwork, type, eth, filterLog,
                            transaction, cl);
                    }
                });

                _dictionary[blockchainID] = new Subscription(client, logsSubscription);

                await client.StartAsync();

                client.Error += Client_Error;

                while (!client.IsStarted)
                {
                    await Task.Delay(1000);
                }

                await logsSubscription.SubscribeAsync();

                while (!hasFailed)
                {
                    try
                    {
                        var handler = new EthBlockNumberObservableHandler(client);
                        handler.GetResponseAsObservable().Subscribe(integer => { });
                        await handler.SendRequestAsync();
                        SystemStatus status = new SystemStatus(TaskNames.WebSockets, blockchainID);
                        await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                        {
                            await status.InsertOrUpdate(connection, true, null, false, "Blockchain Sync");
                        }
                    }
                    catch (Exception ex)
                    {
                        hasFailed = true;
                        _dictionary.Remove(blockchainID, out _);
                        client.Dispose();
                        //try
                        //{
                        //    await client.StopAsync();
                        //    await client.StartAsync();
                        //}
                        //catch (Exception)
                        //{
                        //    Logger.WriteLine(Source.BlockchainSync, ex.ToString());
                        //}
                    }

                    await Task.Delay(120000);
                }
            }

            return !hasFailed;
        }

        private static async Task ProcessSmartContractEvent(int blockchainID, BlockchainType blockchainType,
            BlockchainNetwork blockchainNetwork, ContractTypeEnum type, EthApiService eth, FilterLog filterLog,
            FilterLog transaction, Web3 cl)
        {
            Logger.WriteLine(Source.BlockchainSync, "WebSockets: Processing " + type + " event on " + blockchainType + " " + transaction.TransactionHash);

            if (type == ContractTypeEnum.Holding)
            {
                await ProcessHoldingSmartContractEvent(blockchainID, blockchainType, blockchainNetwork, eth, filterLog, transaction, cl);
            }
            else if (type == ContractTypeEnum.Profile)
            {
                await ProcessProfileSmartContractEvent(blockchainID, blockchainType, blockchainNetwork, eth, filterLog, transaction, cl);
            }
            else if (type == ContractTypeEnum.Litigation)
            {
                await ProcessLitigationSmartContractEvent(blockchainID, blockchainType, blockchainNetwork, eth, filterLog, transaction, cl);
            }
        }

        private static async Task ProcessLitigationSmartContractEvent(int blockchainID, BlockchainType blockchainType,
            BlockchainNetwork blockchainNetwork, EthApiService eth, FilterLog filterLog, FilterLog transaction, Web3 cl)
        {
            var contract = new Contract(eth,
                AbiHelper.GetContractAbi(ContractTypeEnum.Litigation, blockchainType, blockchainNetwork),
                filterLog.Address);

            var litigationInitiatedEvent = contract.GetEvent("LitigationInitiated");
            var litigationAnsweredEvent = contract.GetEvent("LitigationAnswered");
            var litigationTimedOutEvent = contract.GetEvent("LitigationTimedOut");
            var litigationCompletedEvent = contract.GetEvent("LitigationCompleted");
            var replacementStartedEvent = contract.GetEvent("ReplacementStarted");

            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                try
                {
                    if (litigationInitiatedEvent.IsLogForEvent(transaction))
                    {
                        var events =
                            litigationInitiatedEvent.DecodeAllEventsDefaultForEvent(
                                new[] { filterLog });

                        foreach (EventLog<List<ParameterOutput>> eventLog in events)
                        {
                            await SyncLitigationContractTask.ProcessLitigationInitiated(connection,
                                blockchainID, cl,
                                eventLog, eth);
                        }
                    }

                    if (litigationAnsweredEvent.IsLogForEvent(transaction))
                    {
                        var events =
                            litigationAnsweredEvent.DecodeAllEventsDefaultForEvent(
                                new[] { filterLog });

                        foreach (EventLog<List<ParameterOutput>> eventLog in events)
                        {
                            await SyncLitigationContractTask.ProcessLitigationAnswered(connection,
                                blockchainID, cl,
                                eventLog, eth);
                        }
                    }

                    if (litigationTimedOutEvent.IsLogForEvent(transaction))
                    {
                        var events =
                            litigationTimedOutEvent.DecodeAllEventsDefaultForEvent(
                                new[] { filterLog });

                        foreach (EventLog<List<ParameterOutput>> eventLog in events)
                        {
                            await SyncLitigationContractTask.ProcessLitigationTimedOut(connection,
                                blockchainID, cl,
                                eventLog, eth);
                        }
                    }

                    if (litigationCompletedEvent.IsLogForEvent(transaction))
                    {
                        var events =
                            litigationCompletedEvent.DecodeAllEventsDefaultForEvent(
                                new[] { filterLog });

                        foreach (EventLog<List<ParameterOutput>> eventLog in events)
                        {
                            await SyncLitigationContractTask.ProcessLitigationCompleted(connection,
                                blockchainID, cl,
                                eventLog, eth);
                        }
                    }

                    if (replacementStartedEvent.IsLogForEvent(transaction))
                    {
                        var events =
                            replacementStartedEvent.DecodeAllEventsDefaultForEvent(
                                new[] { filterLog });

                        foreach (EventLog<List<ParameterOutput>> eventLog in events)
                        {
                            await SyncLitigationContractTask.ProcessLitigationCompleted(connection,
                                blockchainID, cl,
                                eventLog, eth);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLine(Source.BlockchainSync, ex.ToString());
                }
            }
        }

        private static async Task ProcessProfileSmartContractEvent(int blockchainID, BlockchainType blockchainType,
            BlockchainNetwork blockchainNetwork, EthApiService eth, FilterLog filterLog, FilterLog transaction, Web3 cl)
        {
            var contract = new Contract(eth,
                AbiHelper.GetContractAbi(ContractTypeEnum.Profile, blockchainType, blockchainNetwork),
                filterLog.Address);

            var identityCreatedEvent = contract.GetEvent("IdentityCreated");
            var profileCreatedEvent = contract.GetEvent("ProfileCreated");
            var tokensDepositedEvent = contract.GetEvent("TokensDeposited");
            var tokensReleasedEvent = contract.GetEvent("TokensReleased");
            var tokensWithdrawnEvent = contract.GetEvent("TokensWithdrawn");

            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                try
                {
                    if (identityCreatedEvent.IsLogForEvent(transaction))
                    {
                        var events =
                            identityCreatedEvent.DecodeAllEventsDefaultForEvent(
                                new[] { filterLog });

                        foreach (EventLog<List<ParameterOutput>> eventLog in events)
                        {
                            await SyncProfileContractTask.ProcessIdentityCreated(connection, filterLog.Address,
                                blockchainID, cl,
                                eventLog, eth);
                        }
                    }

                    if (profileCreatedEvent.IsLogForEvent(transaction))
                    {
                        Function createProfileFunction = contract.GetFunction("createProfile");

                        var events =
                            profileCreatedEvent.DecodeAllEventsDefaultForEvent(
                                new[] { filterLog });

                        foreach (EventLog<List<ParameterOutput>> eventLog in events)
                        {
                            await SyncProfileContractTask.ProcessProfileCreated(connection, filterLog.Address, createProfileFunction,
                                blockchainID, cl,
                                eventLog, eth);
                        }
                    }

                    if (tokensDepositedEvent.IsLogForEvent(transaction))
                    {
                        var events =
                            tokensDepositedEvent.DecodeAllEventsDefaultForEvent(
                                new[] { filterLog });

                        foreach (var eventLog in events.GroupBy(t => t.Log.TransactionHash))
                        {
                            await SyncProfileContractTask.ProcessTokensDeposited(connection, filterLog.Address,
                                blockchainID, cl,
                                eventLog, eth);
                        }
                    }

                    if (tokensReleasedEvent.IsLogForEvent(transaction))
                    {
                        var events =
                            tokensReleasedEvent.DecodeAllEventsDefaultForEvent(
                                new[] { filterLog });

                        foreach (var eventLog in events.GroupBy(t => t.Log.TransactionHash))
                        {
                            await SyncProfileContractTask.ProcessTokensReleased(connection, filterLog.Address,
                                blockchainID, cl,
                                eventLog, eth);
                        }
                    }

                    if (tokensWithdrawnEvent.IsLogForEvent(transaction))
                    {
                        var events =
                            tokensWithdrawnEvent.DecodeAllEventsDefaultForEvent(
                                new[] { filterLog });

                        foreach (var eventLog in events.GroupBy(t => t.Log.TransactionHash))
                        {
                            await SyncProfileContractTask.ProcessTokensWithdrawn(connection, filterLog.Address,
                                blockchainID, cl,
                                eventLog, eth);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLine(Source.BlockchainSync, ex.ToString());
                }
            }
        }

        private static async Task ProcessHoldingSmartContractEvent(int blockchainID, BlockchainType blockchainType,
            BlockchainNetwork blockchainNetwork, EthApiService eth, FilterLog filterLog, FilterLog transaction, Web3 cl)
        {
            var contract = new Contract(eth,
                AbiHelper.GetContractAbi(ContractTypeEnum.Holding, blockchainType, blockchainNetwork), filterLog.Address);

            var offerCreatedEvent = contract.GetEvent("OfferCreated");
            var offerFinalizedEvent = contract.GetEvent("OfferFinalized");
            var offerTaskEvent = contract.GetEvent("OfferTask");
            var paidOutEvent = contract.GetEvent("PaidOut");

            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                try
                {
                    if (offerCreatedEvent.IsLogForEvent(transaction))
                    {
                        var events =
                            offerCreatedEvent.DecodeAllEventsForEvent<Models.Program.OfferCreated>(
                                new[] {filterLog});

                        foreach (EventLog<Models.Program.OfferCreated> eventLog in events)
                        {
                            await SyncHoldingContractTask.ProcessOfferCreated(connection, blockchainID, cl,
                                filterLog.Address, eventLog);
                        }

                        await ProcessJobsTask.Execute(connection, blockchainID, blockchainType, blockchainNetwork);
                    }

                    if (offerTaskEvent.IsLogForEvent(transaction))
                    {
                        var events =
                            offerTaskEvent.DecodeAllEventsDefaultForEvent(
                                new[] { filterLog });

                        foreach (EventLog<List<ParameterOutput>> eventLog in events)
                        {
                            await SyncHoldingContractTask.ProcessOfferTasks(connection, blockchainID, cl,
                                filterLog.Address, eventLog, eth);
                        }
                    }

                    if (offerFinalizedEvent.IsLogForEvent(transaction))
                    {
                        var events =
                            offerFinalizedEvent.DecodeAllEventsDefaultForEvent(new[] {filterLog});

                        foreach (EventLog<List<ParameterOutput>> eventLog in events)
                        {
                            await SyncHoldingContractTask.ProcessOfferFinalised(connection, blockchainID, cl,
                                filterLog.Address,
                                eventLog);
                        }

                        await ProcessJobsTask.Execute(connection, blockchainID, blockchainType, blockchainNetwork);
                    }
                    if (paidOutEvent.IsLogForEvent(transaction))
                    {
                        var events =
                            paidOutEvent.DecodeAllEventsDefaultForEvent(new[] {filterLog});

                        foreach (EventLog<List<ParameterOutput>> eventLog in events)
                        {
                            await SyncHoldingContractTask.ProcessPayout(connection, blockchainID, cl,
                                filterLog.Address, eventLog);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Logger.WriteLine(Source.BlockchainSync, ex.ToString());
                }
            }
        }

        private static async void Client_Error(object sender, Exception ex)
        {
            var found = _dictionary.FirstOrDefault(d => d.Value.Client == sender);
            if (found.Key > 0)
            {
                SystemStatus status = new SystemStatus(TaskNames.WebSockets, found.Key);
                await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                {
                    await status.InsertOrUpdate(connection, false, null, false, "Blockchain Sync");
                }
            }
        }
    }

    public class Subscription
    {
        public Subscription(StreamingWebSocketClient client, EthLogsObservableSubscription logsSubscription)
        {
            Client = client;
            LogsSubscription = logsSubscription;
        }

        public StreamingWebSocketClient Client { get; set; }
        public EthLogsObservableSubscription LogsSubscription { get; set; }
    }
}