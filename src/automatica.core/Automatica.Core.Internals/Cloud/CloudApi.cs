using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cloud.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Automatica.Core.Internals.Cloud.Exceptions;
using Automatica.Core.Internals.Plugins;

namespace Automatica.Core.Internals.Cloud
{
    public class SayHelloData
    {
        public string Rid { get; set; }
        public string Version { get; set; }
        public Guid ServerGuid { get; set; }
    }

    public class CloudApi : ICloudApi
    {
        private readonly IConfiguration _config;
        private readonly IPluginInstaller _pluginInstaller;
        private const string UpdateFileName = "Automatica.Core.Update.zip";

        public event EventHandler<DownloadProgressChangedEventArgs> DownloadUpdateProgressChanged;
        public event EventHandler<EventArgs> DownloadUpdateFinished;
        public event EventHandler<AsyncCompletedEventArgs> DownloadUpdateFailed;

        private const string WebApiVersion = "v2";
        private const string WebApiPrefix = "webapi";
        
        public CloudApi(IConfiguration config, IPluginInstaller pluginInstaller)
        {
            _config = config;
            _pluginInstaller = pluginInstaller;
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

        private string GetCloudEnvironmentType()
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


        public Task<ServerVersion> CheckForUpdates()
        {
            return GetRequest<ServerVersion>($"/{WebApiPrefix}/{WebApiVersion}/coreServerData/checkForUpdates/{ServerInfo.Rid}/{ServerInfo.GetServerVersion()}/{GetCloudEnvironmentType()}");
        }

        public Task<IList<Plugin>> GetLatestPlugins()
        {
            return GetRequest<IList<Plugin>>($"/{WebApiPrefix}/{WebApiVersion}/coreServerData/plugins/{ServerInfo.GetServerVersion()}/{GetCloudEnvironmentType()}");
        }

        public async Task<bool> SayHelloToCloud(SayHelloData sayHi)
        {
            try
            {
                await PostRequest<object>($"/{WebApiPrefix}/{WebApiVersion}/coreServerData/sayHello", sayHi);
            }
            catch(Exception e)
            {
                SystemLogger.Instance.LogError(e, "Could not say hi to cloud api");
                return false;
            }
            return true;
        }

        public async Task<bool> SendEmail(IList<string> to, string subject, string message)
        {
            try
            {
                dynamic dyn = new ExpandoObject();
                dyn.To = to;
                dyn.Subject = subject;
                dyn.Body = message;

                await PostRequest<object>($"/{WebApiPrefix}/{WebApiVersion}/coreServerData/sendMail", dyn);
            }
            catch (Exception e)
            {
                SystemLogger.Instance.LogError(e, "Could not say hi to cloud api");
                return false;
            }
            return true;
        }

        public async Task<bool> Ping()
        {
            try
            {
                await GetRequest<object>($"/{WebApiPrefix}/{WebApiVersion}/coreServerData/ping");
            }
            catch(Exception e)
            {
                SystemLogger.Instance.LogError(e, "Could not ping cloud uri");
                return false;
            }
            return true;
        }

        public async Task<T> GetRequest<T>(string apiUrl) where T : class
        {
            T result = null;
            try
            {
                using var client = SetupClient();
                var response = await client.GetAsync(new Uri(new Uri(GetUrl()), apiUrl + "/" + GetApiKey()))
                    .ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new NoApiKeyException();
                }
                response.EnsureSuccessStatusCode();

                await response.Content.ReadAsStringAsync().ContinueWith(x =>
                {
                    if (x.IsFaulted)
                        throw x.Exception;

                    SystemLogger.Instance.LogTrace($"Received {x.Result} from {apiUrl}");
                    result = JsonConvert.DeserializeObject<T>(x.Result);
                });
            }
            catch (NoApiKeyException)
            {
                throw;
            }
            catch (Exception e)
            {
                SystemLogger.Instance.LogError(e, "Could not execute get request to cloud");
            }

            return result;
        }

        public async Task<T> PostRequest<T>(string apiUrl, object postObject) where T : class
        {
            T result = null;
            using (var client = SetupClient())
            {
                var response = await client.PostAsync(new Uri(new Uri(GetUrl()), apiUrl + "/" + GetApiKey()), postObject, new JsonMediaTypeFormatter()).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                await response.Content.ReadAsStringAsync().ContinueWith(x =>
                {
                    if (x.IsFaulted)
                        throw x.Exception;

                    result = JsonConvert.DeserializeObject<T>(x.Result);
                });
            }

            return result;
        }

        public Task<bool> UpdateAlreadyDownloaded()
        {
            var fileExists = File.Exists(Path.Combine(ServerInfo.GetTempPath(), UpdateFileName));
            return Task.FromResult(fileExists);
        }

        public void DeleteUpdate()
        {
            var updateFile = Path.Combine(ServerInfo.GetTempPath(), UpdateFileName);

            if(File.Exists(updateFile))
            {
                File.Delete(updateFile);
            }
        }

        public async Task<FileInfo> DownloadUpdate(ServerVersion update)
        {
            var file = await DownloadFile(update.AzureUrl);

            var tmpFile = Path.Combine(ServerInfo.GetTempPath(), UpdateFileName);
            await using(var stream = new FileStream(tmpFile, FileMode.OpenOrCreate))
            {
                stream.Write(file);
            }

            return new FileInfo(tmpFile);
        }

        public async Task<byte[]> DownloadFile(string url)
        {
            using var webClient = new WebClient();
            webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;

            try
            {
                var download = await webClient.DownloadDataTaskAsync(url);
                webClient.DownloadProgressChanged -= WebClient_DownloadProgressChanged;
                DownloadUpdateFinished?.Invoke(this, EventArgs.Empty);
                return download;
            }
            catch (Exception e)
            {
                DownloadUpdateFailed?.Invoke(this, new AsyncCompletedEventArgs(e, false, null));
                return Array.Empty<byte>();
            }
        }

        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DownloadUpdateProgressChanged?.Invoke(this, e);
        }


        public Task<bool> InstallPlugin(Plugin plugin, string fileName)
        {
            return _pluginInstaller.InstallPlugin(plugin, fileName);
        }


    }
}
