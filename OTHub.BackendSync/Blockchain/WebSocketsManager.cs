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

namespace OTHub.BackendSync.Blockchain
{
    public static class WebSocketsManager
    {
        private static readonly ConcurrentDictionary<int, Subscription> _dictionary = new ConcurrentDictionary<int, Subscription>();
        public static async Task Start(int blockchainID, string webSocketsUrl, string rpcUrl,
            BlockchainType blockchainType, BlockchainNetwork blockchainNetwork)
        {
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
                        await ProcessSmartContractEvent(blockchainID, blockchainType, blockchainNetwork, type, eth, filterLog, transaction, cl);
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


                int errorCounter = 0;

                while (true)
                {
                    try
                    {
                        var handler = new EthBlockNumberObservableHandler(client);
                        handler.GetResponseAsObservable().Subscribe(integer => { });
                        await handler.SendRequestAsync();
                        SystemStatus status = new SystemStatus("WebSockets", blockchainID);
                        using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                        {
                            status.InsertOrUpdate(connection, true, null, false, "Blockchain Sync");
                        }
                    }
                    catch (Exception ex) when (errorCounter <= 100)
                    {
                        Logger.WriteLine(Source.BlockchainSync, ex.ToString());
                        errorCounter++;
                    }
                    catch (Exception ex) when (errorCounter > 100)
                    {
                        errorCounter = 0;
                        try
                        {
                            await client.StopAsync();
                            await client.StartAsync();
                        }
                        catch (Exception)
                        {
                            Logger.WriteLine(Source.BlockchainSync, ex.ToString());
                        }
                    }

                    await Task.Delay(30000);
                }
            }
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
        }

        private static async Task ProcessProfileSmartContractEvent(int blockchainID, BlockchainType blockchainType,
            BlockchainNetwork blockchainNetwork, EthApiService eth, FilterLog filterLog, FilterLog transaction, Web3 cl)
        {
            var contract = new Contract(eth,
                AbiHelper.GetContractAbi(ContractTypeEnum.Profile, blockchainType, blockchainNetwork), filterLog.Address);


        }

        private static async Task ProcessHoldingSmartContractEvent(int blockchainID, BlockchainType blockchainType,
            BlockchainNetwork blockchainNetwork, EthApiService eth, FilterLog filterLog, FilterLog transaction, Web3 cl)
        {
            var contract = new Contract(eth,
                AbiHelper.GetContractAbi(ContractTypeEnum.Holding, blockchainType, blockchainNetwork), filterLog.Address);

            var offerCreatedEvent = contract.GetEvent("OfferCreated");
            var offerFinalizedEvent = contract.GetEvent("OfferFinalized");
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
                    else if (offerFinalizedEvent.IsLogForEvent(transaction))
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
                    else if (paidOutEvent.IsLogForEvent(transaction))
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
                SystemStatus status = new SystemStatus("WebSockets", found.Key);
                using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                {
                    status.InsertOrUpdate(connection, false, null, false, "Blockchain Sync");
                }

                await Task.Delay(30000);

                try
                {
                    await found.Value.Client.StartAsync();
                }
                catch (Exception x)
                {
                    Logger.WriteLine(Source.BlockchainSync, x.ToString());
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