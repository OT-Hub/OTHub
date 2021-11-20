using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Nethereum.JsonRpc.Client;

namespace OTHub.BackendSync.Blockchain.Web3Helper
{
    public class RpcHttpClientManager
    {
        private HttpClient _httpClient;
        private HttpClient _httpClient2;
        private const int NUMBER_OF_SECONDS_TO_RECREATE_HTTP_CLIENT = 500;
        private readonly AuthenticationHeaderValue _authHeaderValue;
        private readonly Uri _baseUrl;
        private readonly HttpClientHandler _httpClientHandler;
        private readonly bool _rotateHttpClients = true;
        private DateTime _httpClientLastCreatedAt;
        private volatile bool _firstHttpClient;
        private readonly object _lockObject = new object();

        public RpcHttpClientManager(HttpClientHandler httpClientHandler,
            AuthenticationHeaderValue authHeaderValue, Uri baseUrl)
        {
            this._rotateHttpClients = false;
            this._httpClientHandler = httpClientHandler;
            this._authHeaderValue = authHeaderValue;
            this._baseUrl = baseUrl;
            this._httpClient = this.CreateNewHttpClient();
        }

        private HttpClient GetClient()
        {
            if (!this._rotateHttpClients)
                return this._httpClient;
            lock (this._lockObject)
                return this._firstHttpClient ? this._httpClient : this._httpClient2;
        }

        private void CreateNewRotatedHttpClient()
        {
            HttpClient newHttpClient = this.CreateNewHttpClient();
            this._httpClientLastCreatedAt = DateTime.UtcNow;
            if (this._firstHttpClient)
            {
                lock (this._lockObject)
                {
                    this._firstHttpClient = false;
                    this._httpClient2 = newHttpClient;
                }
            }
            else
            {
                lock (this._lockObject)
                {
                    this._firstHttpClient = true;
                    this._httpClient = newHttpClient;
                }
            }
        }

        private HttpClient CreateNewHttpClient()
        {
            HttpClient httpClient = new HttpClient();
            if (this._httpClientHandler != null)
            {
                httpClient = new HttpClient((HttpMessageHandler)this._httpClientHandler);
            }
            else
            {
                HttpMessageHandler defaultHandler = GetDefaultHandler();
                if (defaultHandler != null)
                    httpClient = new HttpClient(defaultHandler);
            }
            this.InitialiseHttpClient(httpClient);
            return httpClient;
        }

        private void InitialiseHttpClient(HttpClient httpClient)
        {
            httpClient.DefaultRequestHeaders.Authorization = this._authHeaderValue;
            httpClient.BaseAddress = this._baseUrl;
        }

        public HttpClient GetOrCreateHttpClient()
        {
            if (!this._rotateHttpClients)
                return this.GetClient();
            lock (this._lockObject)
            {
                if ((DateTime.UtcNow - this._httpClientLastCreatedAt).TotalSeconds > NUMBER_OF_SECONDS_TO_RECREATE_HTTP_CLIENT)
                    this.CreateNewRotatedHttpClient();
                return this.GetClient();
            }
        }

        private static HttpMessageHandler GetDefaultHandler()
        {
            try
            {
                return (HttpMessageHandler)new SocketsHttpHandler()
                {
                    PooledConnectionLifetime = new TimeSpan(0, 60, 0),
                    PooledConnectionIdleTimeout = new TimeSpan(0, 60, 0),
                    MaxConnectionsPerServer = RpcClient.MaximumConnectionsPerServer
                };
            }
            catch
            {
                return (HttpMessageHandler)null;
            }
        }
    }
}