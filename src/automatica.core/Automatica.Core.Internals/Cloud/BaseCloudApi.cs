using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cloud.Exceptions;
using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http.Formatting;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Internals.Cloud
{
    public  class BaseCloudApi
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        protected const string WebApiVersion = "v2";
        protected const string WebApiPrefix = "webapi";

        /// <summary>
        /// The api key and server guid are also sent as headers. The cloud still reads them from the url path, once every cloud
        /// accepts the headers the path segments can be dropped.
        /// </summary>
        public const string ApiKeyHeader = "X-Automatica-ApiKey";
        public const string ServerGuidHeader = "X-Automatica-ServerGuid";

        /// <summary>
        /// One shared client: certificate validation is always on (no custom validation callback)
        /// </summary>
        private static readonly HttpClient Client = CreateClient();

        public BaseCloudApi(IConfiguration config, ILogger logger)
        {
            _config = config;
            _logger = logger;
        }

        protected string GetUrl()
        {
            using var dbContext = new AutomaticaContext(_config);
            return $"{dbContext.Settings.First(a => a.ValueKey == "cloudUrl").ValueText}";
        }

        private string GetApiKeyValue()
        {
            using var dbContext = new AutomaticaContext(_config);
            var apiKey = dbContext.Settings.First(a => a.ValueKey == "apiKey").ValueText;

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new NoApiKeyException();
            }

            return apiKey;
        }

        protected string GetCloudEnvironmentType()
        {
            using var dbContext = new AutomaticaContext(_config);
            var cloudEnv = dbContext.Settings.FirstOrDefault(a => a.ValueKey == "cloudEnvironment");

            if (cloudEnv == null)
            {
                return "develop";
            }
            return $"{cloudEnv.ValueText}";
        }

        private static HttpClient CreateClient()
        {
            var client = new HttpClient(new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                AllowAutoRedirect = false
            })
            {
                Timeout = TimeSpan.FromMinutes(2)
            };
            return client;
        }

        /// <summary>
        /// The api key must never be sent in clear text: https only, plain http only for a local development cloud
        /// </summary>
        internal static Uri BuildRequestUri(string cloudUrl, string apiUrl, string apiKey, Guid serverUid)
        {
            var baseUri = new Uri(cloudUrl);
            if (baseUri.Scheme != Uri.UriSchemeHttps && !baseUri.IsLoopback)
            {
                throw new InvalidOperationException($"The cloud url must use https ({baseUri.Scheme} is not allowed)");
            }

            return new Uri(baseUri, $"{apiUrl}/{Uri.EscapeDataString(apiKey)}/{serverUid}");
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string apiUrl)
        {
            var apiKey = GetApiKeyValue();
            var request = new HttpRequestMessage(method, BuildRequestUri(GetUrl(), apiUrl, apiKey, ServerInfo.ServerUid));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.TryAddWithoutValidation(ApiKeyHeader, apiKey);
            request.Headers.TryAddWithoutValidation(ServerGuidHeader, ServerInfo.ServerUid.ToString());
            return request;
        }

        public async Task<T> GetRequest<T>(string apiUrl) where T : class
        {
            using var request = CreateRequest(HttpMethod.Get, apiUrl);
            using var response = await Client.SendAsync(request).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new NoApiKeyException();
            }

            var responseText = await response.Content.ReadAsStringAsync();
            try
            {
                response.EnsureSuccessStatusCode();

                _logger.LogTrace($"Received {responseText} from {apiUrl}");
                return JsonConvert.DeserializeObject<T>(responseText);

            }
            catch (NoApiKeyException)
            {
                throw;
            }
            catch (Exception e)
            {
                // never log the request url, it contains the api key
                _logger.LogError(e, $"Could not execute get request to cloud: {apiUrl}: {responseText}");
            }

            return default(T);
        }

        public async Task<T> PostRequest<T>(string apiUrl, object postObject) where T : class
        {
            using var request = CreateRequest(HttpMethod.Post, apiUrl);
            request.Content = new ObjectContent(postObject?.GetType() ?? typeof(object), postObject, new JsonMediaTypeFormatter());

            using var response = await Client.SendAsync(request).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            return JsonConvert.DeserializeObject<T>(responseText);
        }
    }
}
