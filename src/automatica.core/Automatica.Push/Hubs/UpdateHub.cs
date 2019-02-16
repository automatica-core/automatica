using Automatica.Core.Base.Common;

using Automatica.Core.EF.Models;
using Automatica.Core.Internals;
using Automatica.Core.Internals.Cloud;
using Automatica.Core.Internals.Cloud.Model;
using Automatica.Core.Internals.Core;
using Automatica.Push.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Automatica.Push.Hubs
{
    [Authorize]
    public class UpdateHub : Hub 
    {
        private readonly ICloudApi _api;
        private readonly IHubContext<UpdateHub> _updateHub;
        private readonly ICoreServer _coreServer;

        public UpdateHub(ICloudApi api, IHubContext<UpdateHub> updateHub, ICoreServer coreServer)
        {
            _api = api;
            _updateHub = updateHub;
            _coreServer = coreServer;
        }
        public Task StartUpdateDownload(ServerVersion version)
        {
            Task.Run(() =>
            {
                var previousState = 0;
                _api.DownloadUpdateProgressChanged += (sender, e) =>
                {
                    if (previousState != e.ProgressPercentage)
                    {
                        previousState = e.ProgressPercentage;
                        _updateHub.Clients.All.SendAsync("UpdateDownloadProgressChanged", new object[] { e.BytesReceived, e.TotalBytesToReceive });
                        SystemLogger.Instance.LogInformation($"Downloading update {e.ProgressPercentage} - {e.BytesReceived}/{e.TotalBytesToReceive}");
                    }
                    
                };
                _api.DownloadUpdateFinished += (sender, e) =>
                {
                    _updateHub.Clients.All.SendAsync("UpdateFinished");
                };
                _api.DownloadUpdateFailed += (sender, e) =>
                {
                    _updateHub.Clients.All.SendAsync("UpdateFailed", new object[] { e.Error });
                };
                _api.DownloadUpdate(version);
            });
            return Task.CompletedTask;
        }

        private Task DownloadPlugin(Plugin plugin, bool install)
        {
            Task.Run(async () =>
            {
            var pluginDownloader = new PluginDownloader(plugin, install, _api, _updateHub, _coreServer);

                await pluginDownloader.Download();
            });
            return Task.CompletedTask;
        }

        public Task StartPluginInstall(Plugin plugin)
        {
            return DownloadPlugin(plugin, true);
        }
        public Task StartPluginUpdate(Plugin plugin)
        {
            return DownloadPlugin(plugin, false);
        }

        public Task UpdateAllPlugin(IList<Plugin> plugins)
        {
            foreach (var plug in plugins) {
                Task.Run(async () =>
                {
                    var pluginDownloader = new PluginDownloader(plug, false, _api, _updateHub, _coreServer, false);

                    await pluginDownloader.Download();
                });
            }

            return Task.CompletedTask;
        }

        public void Restart()
        {
            _coreServer.Restart();
        }
    }
}
