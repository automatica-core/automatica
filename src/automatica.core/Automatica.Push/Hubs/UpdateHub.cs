using AsyncKeyedLock;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals;
using Automatica.Core.Internals.Cloud;
using Automatica.Core.Internals.Cloud.Model;
using Automatica.Core.Internals.Core;
using Automatica.Core.Internals.Plugins;
using Automatica.Push.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Automatica.Push.Hubs
{
    [Authorize]
    public class UpdateHub : Hub
    {
        private readonly ICloudApi _api;
        private readonly IHubContext<UpdateHub> _updateHub;
        private readonly ICoreServer _coreServer;
        private readonly IPluginLoader _loader;
        private readonly AsyncKeyedLocker<string> _asyncKeyedLocker;

        public UpdateHub(ICloudApi api, IHubContext<UpdateHub> updateHub, ICoreServer coreServer, IPluginLoader loader, AsyncKeyedLocker<string> asyncKeyedLocker)
        {
            _api = api;
            _updateHub = updateHub;
            _coreServer = coreServer;
            _loader = loader;
            _asyncKeyedLocker = asyncKeyedLocker;
        }
        public Task StartUpdateDownload(ServerVersion version)
        {
            Task.Run(async () =>
            {
                using var releaser = (AsyncKeyedLockTimeoutReleaser<string>)await _asyncKeyedLocker.LockAsync(nameof(UpdateHub), 10).ConfigureAwait(false);
                if (!releaser.EnteredSemaphore)
                {
                    return;
                }

                var previousState = 0;
                _api.DownloadUpdateProgressChanged += (sender, e) =>
                {
                    if (previousState != e.ProgressPercentage)
                    {
                        previousState = e.ProgressPercentage;
                        _updateHub.Clients.All.SendAsync("UpdateDownloadProgressChanged",
                            new object[] { e.BytesReceived, e.TotalBytesToReceive });
                        SystemLogger.Instance.LogInformation(
                            $"Downloading update {e.ProgressPercentage} - {e.BytesReceived}/{e.TotalBytesToReceive}");
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
                await _api.DownloadUpdate(version);
            });
            return Task.CompletedTask;
        }

        private Task DownloadPlugin(Plugin plugin, bool install)
        {
            Task.Run(async () =>
            {
                using var releaser = (AsyncKeyedLockTimeoutReleaser<string>)await _asyncKeyedLocker.LockAsync(nameof(UpdateHub), 10).ConfigureAwait(false);
                if (!releaser.EnteredSemaphore)
                {
                    return;
                }

                var pluginDownloader =
                    new PluginDownloader(plugin, install, _api, _updateHub, _coreServer, _loader);

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
            return InstallOrUpdatePlugins(plugins);
        }

        public Task InstallAllPlugin(IList<Plugin> plugins)
        {
            return InstallOrUpdatePlugins(plugins);
        }
        public Task InstallUpdateAllPlugin(IList<Plugin> plugins)
        {
            return InstallOrUpdatePlugins(plugins);
        }

        private async Task InstallOrUpdatePlugins(IList<Plugin> plugins)
        {
            using var releaser = (AsyncKeyedLockTimeoutReleaser<string>)await _asyncKeyedLocker.LockAsync(nameof(UpdateHub), 100).ConfigureAwait(false);
            if (!releaser.EnteredSemaphore)
            {
                return;
            }

            var tasks = new List<Task>();
            foreach (var plug in plugins)
            {
                var pluginDownloader = new PluginDownloader(plug, false, _api, _updateHub, _coreServer,
                    _loader, false);

                tasks.Add(pluginDownloader.Download());
            }

            await Task.WhenAll(tasks.ToArray());
            _coreServer.Restart();
        }
    }
}