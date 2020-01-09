using Automatica.Core.Base.Common;

using Automatica.Core.EF.Models;
using Automatica.Core.Internals;
using Automatica.Core.Internals.Cloud;
using Automatica.Core.Internals.Core;
using Automatica.Push.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Automatica.Core.Internals.Plugins;

namespace Automatica.Push.Helper
{
    public class PluginDownloader
    {
        private readonly Plugin _plugin;
        private readonly bool _install;
        private readonly ICloudApi _api;
        private readonly IHubContext<UpdateHub> _updateHub;
        private readonly ICoreServer _coreServer;
        private readonly IPluginLoader _pluginLoader;
        private readonly bool _restartOnUpdate;
        private int _previousState;

        public PluginDownloader(Plugin plugin, bool install, ICloudApi cloudApi, IHubContext<UpdateHub> updateHub, ICoreServer coreServer, IPluginLoader pluginLoader, bool restartOnUpdate = true)
        {
            _plugin = plugin;
            _install = install;
            _api = cloudApi;
            _updateHub = updateHub;
            _coreServer = coreServer;
            _pluginLoader = pluginLoader;
            _restartOnUpdate = restartOnUpdate;
        }

        public async Task Download()
        {
            using (var webClient = new WebClient())
            {
                var automaticaPluginUpdateFile = Path.Combine(ServerInfo.GetTempPath(), ServerInfo.PluginUpdateDirectoryName, _plugin.AzureFileName);

                try
                {
                    webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                    webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
                    await webClient.DownloadFileTaskAsync(_plugin.AzureUrl, automaticaPluginUpdateFile);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Could not download file {e}");
                    File.Delete(automaticaPluginUpdateFile);
                }
                finally
                {
                    webClient.DownloadProgressChanged -= WebClient_DownloadProgressChanged;
                    webClient.DownloadFileCompleted -= WebClient_DownloadFileCompleted;
                }
            }   
        }

        private async void WebClient_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            var automaticaPluginUpdateFile = Path.Combine(ServerInfo.GetTempPath(), ServerInfo.PluginUpdateDirectoryName, _plugin.AzureFileName);
            try
            {
                if (e.Error == null && !e.Cancelled)
                {
                    await _updateHub.Clients.All.SendAsync("PluginFinished", new object[] {_plugin.PluginGuid});
                    if (_install)
                    {
                        if(await _api.InstallPlugin(_plugin, automaticaPluginUpdateFile))
                        {
                            await _pluginLoader.LoadPlugin(_plugin);
                        }
                    }
                    else
                    {
                        if (_restartOnUpdate)
                        {
                            _coreServer.Restart();
                        }
                    }
                }
            }
            finally
            {
                File.Delete(automaticaPluginUpdateFile);
            }
        }

        private async void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (_previousState != e.ProgressPercentage)
            {
                _previousState = e.ProgressPercentage;
                await _updateHub.Clients.All.SendAsync("PluginDownloadProgressChanged", new object[] { _plugin.PluginGuid, e.BytesReceived, e.TotalBytesToReceive });
                SystemLogger.Instance.LogInformation($"Downloading plugin {e.ProgressPercentage} - {e.BytesReceived}/{e.TotalBytesToReceive}");
            }
        }
    }
}
