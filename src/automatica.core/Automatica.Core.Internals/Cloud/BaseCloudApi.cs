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

        public BaseCloudApi(IConfiguration config, ILogger logger)
        {
            _config = config;
            _logger = logger;
        }
        private string GetUrl()
        {
            using var dbContext = new AutomaticaContext(_config);
            return $"{dbContext.Settings.First(a => a.ValueKey == "cloudUrl").ValueText}";
        }

        private string GetApiKey()
        {
            using var dbContext = new AutomaticaContext(_config);
            var apiKey = dbContext.Settings.First(a => a.ValueKey == "apiKey").ValueText;

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new NoApiKeyException();
            }

            return $"{apiKey}/{ServerInfo.ServerUid}";
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

        private HttpClient SetupClient()
        {
            var httpClientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            // Three versions in one.
            HttpClient client = new HttpClient(httpClientHandler);

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }

        public async Task<T> GetRequest<T>(string apiUrl) where T : class
        {
            using var client = SetupClient();
            var response = await client.GetAsync(new Uri(new Uri(GetUrl()), apiUrl + "/" + GetApiKey()))
                .ConfigureAwait(false);

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
                _logger.LogError(e, $"Could not execute get request to cloud: {apiUrl}: {responseText}");
            }

            return default(T);
        }

        public async Task<T> PostRequest<T>(string apiUrl, object postObject) where T : class
        {
            T result = null;
            using var client = SetupClient();
            var url = GetUrl();
            var apiKey = GetApiKey();
            var response = await client.PostAsync(new Uri(new Uri(url), apiUrl + "/" + apiKey), postObject, new JsonMediaTypeFormatter()).ConfigureAwait(false);
           
            await response.Content.ReadAsStringAsync().ContinueWith(x =>
            {
                response.EnsureSuccessStatusCode();
                if (x.IsFaulted)
                    throw x.Exception;

                result = JsonConvert.DeserializeObject<T>(x.Result);
            });

            response.EnsureSuccessStatusCode();

           
            return result;
        }
    }
}
