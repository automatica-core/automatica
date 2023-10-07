﻿using Automatica.Core.Base.Common;
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
using Automatica.Core.Base.Tunneling;
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
    public class RemoteConnectObject
    {
        public string TunnelUrl { get; set; }
        public string SubDomain { get; set; }
    }
    public class RemoteConnectPortResponse
    {
        public int Port { get; set; }
    }
    public class CreateRemoteConnectPortObject
    {
        public Guid DriverId { get; set; }
        public string ServiceName { get; set; }
        public string TunnelingProtocol { get; set; }
    }
    public class CreateRemoteConnectObject
    {
        public string TargetSubDomain { get; set; }
    }
    public class CloudApi : ICloudApi
    {
        private readonly IConfiguration _config;
        private readonly IPluginInstaller _pluginInstaller;
        private readonly ILogger<CloudApi> _logger;
        private const string UpdateFileName = "Automatica.Core.Update.zip";

        public event EventHandler<DownloadProgressChangedEventArgs> DownloadUpdateProgressChanged;
        public event EventHandler<EventArgs> DownloadUpdateFinished;
        public event EventHandler<AsyncCompletedEventArgs> DownloadUpdateFailed;

        private const string WebApiVersion = "v2";
        private const string WebApiPrefix = "webapi";
        
        public CloudApi(IConfiguration config, IPluginInstaller pluginInstaller, ILogger<CloudApi> logger)
        {
            _config = config;
            _pluginInstaller = pluginInstaller;
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


        public async Task<RemoteConnectObject> CreateRemoteConnectUrl(string subDomain)
        {
            try
            {
                var remoteConnectObj = new CreateRemoteConnectObject
                {
                    TargetSubDomain = subDomain
                };
                return await PostRequest<RemoteConnectObject>($"/{WebApiPrefix}/{WebApiVersion}/coreServerData/createRemoteConnect", remoteConnectObj);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not say hi to cloud api");
                return null;
            }
        }

        public async Task<RemoteConnectObject> CreateRemoteConnectUrl(Guid pluginGuid, string subDomain)
        {
            try
            {
                var remoteConnectObj = new CreateRemoteConnectObject
                {
                    TargetSubDomain = subDomain
                };
                return await PostRequest<RemoteConnectObject>($"/{WebApiPrefix}/{WebApiVersion}/coreServerData/createRemoteConnect/{pluginGuid}", remoteConnectObj);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not say hi to cloud api");
                return null;
            }
        }

        public async Task<RemoteConnectPortResponse> GetRemoteConnectPort(Guid pluginGuid, string serviceName, TunnelingProtocol tunnelingProtocol)
        {
            try
            {
                var remoteConnectObj = new CreateRemoteConnectPortObject
                {
                    DriverId = pluginGuid,
                    ServiceName = serviceName,
                    TunnelingProtocol =  tunnelingProtocol.ToString().ToLowerInvariant()
                };
                return await PostRequest<RemoteConnectPortResponse>($"/{WebApiPrefix}/{WebApiVersion}/coreServerData/createRemoteConnectPort", remoteConnectObj);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not say hi to cloud api");
                return null;
            }
        }

        public async Task<bool> SendRemoteConnectUrl(string url)
        {
            try
            {
                var remoteConnectObj = new RemoteConnectObject
                {
                    TunnelUrl = url
                };
                await PostRequest<object>($"/{WebApiPrefix}/{WebApiVersion}/coreServerData/remoteConnect", remoteConnectObj);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not say hi to cloud api");
                return false;
            }
            return true;
        }

        public Task<ServerVersion> CheckForUpdates()
        {
            return GetRequest<ServerVersion>($"/{WebApiPrefix}/{WebApiVersion}/coreServerData/checkForUpdates/{ServerInfo.Rid}/{ServerInfo.GetServerVersion()}/{GetCloudEnvironmentType()}");
        }

        public Task<ServerDockerVersion> CheckForDockerUpdates()
        {
            return GetRequest<ServerDockerVersion>($"/{WebApiPrefix}/{WebApiVersion}/coreServerData/checkForDockerUpdates/{GetCloudEnvironmentType()}/{ServerInfo.GetServerVersion()}");
        }

        public Task<IList<Plugin>> GetLatestPlugins()
        {
            return GetRequest<IList<Plugin>>($"/{WebApiPrefix}/{WebApiVersion}/coreServerData/plugins/{ServerInfo.GetServerVersion()}/{GetCloudEnvironmentType()}");
        }


        public Task<string> GetLicense()
        {
            return GetRequest<string>($"/{WebApiPrefix}/{WebApiVersion}/coreServerData/license");
        }

        public async Task<bool> SayHelloToCloud(SayHelloData sayHi)
        {
            try
            {
                await PostRequest<object>($"/{WebApiPrefix}/{WebApiVersion}/coreServerData/sayHello", sayHi);
            }
            catch(Exception e)
            {
                _logger.LogError(e, "Could not say hi to cloud api");
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
                _logger.LogError(e, "Could not say hi to cloud api");
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
                _logger.LogError(e, "Could not ping cloud uri");
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

                    _logger.LogTrace($"Received {x.Result} from {apiUrl}");
                    result = JsonConvert.DeserializeObject<T>(x.Result);
                });
            }
            catch (NoApiKeyException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not execute get request to cloud");
            }

            return result;
        }

        public async Task<T> PostRequest<T>(string apiUrl, object postObject) where T : class
        {
            T result = null;
            using var client = SetupClient();
            var url = GetUrl();
            var apiKey = GetApiKey();
            var response = await client.PostAsync(new Uri(new Uri(url), apiUrl + "/" + apiKey), postObject, new JsonMediaTypeFormatter()).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            await response.Content.ReadAsStringAsync().ContinueWith(x =>
            {
                if (x.IsFaulted)
                    throw x.Exception;

                result = JsonConvert.DeserializeObject<T>(x.Result);
            });

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

        public async Task<FileInfo> DownloadUpdate(IServerVersion update)
        {
            if (update.Type != nameof(ServerVersion))
            {
                throw new ArgumentException("Invalid type...");
            }

            var updateServerVersion = (ServerVersion)update;

            var file = await DownloadFile(updateServerVersion.AzureUrl);

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
