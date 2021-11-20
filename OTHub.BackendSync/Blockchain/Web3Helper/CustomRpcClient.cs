using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using MySqlConnector;
using Nethereum.JsonRpc.Client;
using Nethereum.JsonRpc.Client.RpcMessages;
using Newtonsoft.Json;
using OTHub.BackendSync.Database.Models;
using OTHub.Settings;

namespace OTHub.BackendSync.Blockchain.Web3Helper
{
    public class CustomRpcClient : ClientBase
    {

        private readonly ILog _log;
        private readonly EndpointDelegate _getEndpointDelegate;
        private readonly EndpointToTryOnFailureDelegate _getEndpointToTryOnFailureDelegate;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly Dictionary<Web3RpcEndpoint, RpcHttpClientManager> _httpManagerDictionary = new Dictionary<Web3RpcEndpoint, RpcHttpClientManager>();

        public delegate Web3RpcEndpoint EndpointDelegate();
        public delegate Web3RpcEndpoint EndpointToTryOnFailureDelegate(List<Web3RpcEndpoint> triedEndpoints);

        public CustomRpcClient(Web3RpcEndpoint[] endpoints, EndpointDelegate getEndpointDelegate, EndpointToTryOnFailureDelegate endpointToTryOnFailureDelegate, JsonSerializerSettings jsonSerializerSettings = null,
            HttpClientHandler httpClientHandler = null,
            ILog log = null)
        {

            if (jsonSerializerSettings == null)
                jsonSerializerSettings = DefaultJsonSerializerSettingsFactory.BuildDefaultJsonSerializerSettings();
            _getEndpointDelegate = getEndpointDelegate;
            _getEndpointToTryOnFailureDelegate = endpointToTryOnFailureDelegate;
            this._jsonSerializerSettings = jsonSerializerSettings;
            this._log = log;

            foreach (var web3RpcEndpoint in endpoints)
            {
                _httpManagerDictionary[web3RpcEndpoint] = new RpcHttpClientManager(httpClientHandler, null, web3RpcEndpoint.Url);
            }
        }



        protected override async Task<RpcResponseMessage> SendAsync(
          RpcRequestMessage request,
          string route = null)
        {
            RpcLogger logger = new RpcLogger(this._log);
            RpcResponseMessage rpcResponseMessage;

            List<Web3RpcEndpoint> endpointsTried = new List<Web3RpcEndpoint>();

            Web3RpcEndpoint endpoint = _getEndpointDelegate();
            endpointsTried.Add(endpoint);

            int? previousRPCID = null;

            startOfHttpCall:

            Rpcshistory history = new Rpcshistory
            {
                Timestamp = DateTime.Now,
                RPCID = endpoint.ID,
                Method = request.Method,
                RedirectedRPCID = previousRPCID
            };

            try
            {
                RpcHttpClientManager httpClientManager = _httpManagerDictionary[endpoint];
                HttpClient httpClient = httpClientManager.GetOrCreateHttpClient();
                string str = JsonConvert.SerializeObject((object) request, this._jsonSerializerSettings);
                StringContent stringContent1 = new StringContent(str, Encoding.UTF8, "application/json");
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(ClientBase.ConnectionTimeout);
                logger.LogRequest(str);
                string requestUri = route;
                StringContent stringContent2 = stringContent1;
                CancellationToken token = cancellationTokenSource.Token;

                HttpResponseMessage httpResponseMessage = await httpClient
                    .PostAsync(requestUri, (HttpContent) stringContent2, token).ConfigureAwait(false);

#if DEBUG
                Console.WriteLine(httpResponseMessage.RequestMessage.RequestUri);
#endif

                httpResponseMessage.EnsureSuccessStatusCode();
                using (StreamReader streamReader =
                    new StreamReader(await httpResponseMessage.Content.ReadAsStreamAsync(token)))
                {
                    using (JsonTextReader jsonTextReader = new JsonTextReader((TextReader) streamReader))
                    {
                        RpcResponseMessage responseMessage = JsonSerializer.Create(this._jsonSerializerSettings)
                            .Deserialize<RpcResponseMessage>((JsonReader) jsonTextReader);
                        logger.LogResponse(responseMessage);
                        rpcResponseMessage = responseMessage;
                    }
                }

                history.Success = true;
                history.Duration = (int)(DateTime.Now - history.Timestamp).TotalMilliseconds;
            }
            catch (TaskCanceledException ex)
            {
                history.Success = false;
                history.Duration = (int)(DateTime.Now - history.Timestamp).TotalMilliseconds;
                await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                {
                    await history.Insert(connection);
                }

                RpcClientTimeoutException timeoutException = new RpcClientTimeoutException(
                    $"Rpc timeout after {(object) ClientBase.ConnectionTimeout.TotalMilliseconds} milliseconds",
                    (Exception) ex);
                logger.LogException((Exception) timeoutException);
                throw timeoutException;
            }
            catch (Exception ex)
            {
                history.Success = false;
                history.Duration = (int)(DateTime.Now - history.Timestamp).TotalMilliseconds;
                await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                {
                    await history.Insert(connection);
                }

                RpcClientUnknownException unknownException =
                    new RpcClientUnknownException("Error occurred when trying to send rpc requests(s)", ex);
                logger.LogException((Exception) unknownException);



                Web3RpcEndpoint endpointToRetryOn = _getEndpointToTryOnFailureDelegate(endpointsTried);
                if (endpointToRetryOn != null)
                {
                    previousRPCID = endpoint.ID;
                    endpoint = endpointToRetryOn;
                    endpointsTried.Add(endpoint);
                    goto startOfHttpCall;
                }

                throw unknownException;
            }

            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                await history.Insert(connection);
            }

            logger = (RpcLogger)null;
            return rpcResponseMessage;
        }
    }
}