using AsyncKeyedLock;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cloud;
using Automatica.Core.Internals.Cloud.Model;
using Automatica.Core.Internals.Core;
using Automatica.Core.Internals.Plugins;
using Automatica.Core.Model.Models.User;
using Automatica.Push.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Automatica.Push.Hubs
{
    /// <summary>
    /// Server update and plugin installation, admin only.
    /// The client only says *which* update or plugin it wants: the download url, the file name and the component
    /// name are always taken from the cloud api, never from the client.
    /// </summary>
    [Authorize]
    public class UpdateHub : Hub
    {
        private readonly ICloudApi _api;
        private readonly IHubContext<UpdateHub> _updateHub;
        private readonly ICoreServer _coreServer;
        private readonly IPluginLoader _loader;
        private readonly AsyncKeyedLocker<string> _asyncKeyedLocker;
        private readonly ILogger<UpdateHub> _logger;

        public UpdateHub(ICloudApi api, IHubContext<UpdateHub> updateHub, ICoreServer coreServer, IPluginLoader loader, AsyncKeyedLocker<string> asyncKeyedLocker, ILogger<UpdateHub> logger)
        {
            _api = api;
            _updateHub = updateHub;
            _coreServer = coreServer;
            _loader = loader;
            _asyncKeyedLocker = asyncKeyedLocker;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            await HubAccess.JoinAdministratorsGroup(Context, Groups);
            await base.OnConnectedAsync();
        }

        private IClientProxy Administrators => _updateHub.Clients.Group(HubAccess.AdministratorsGroup);

        [Authorize(Policy = Role.AdminRole)]
        public Task StartUpdateDownload(ServerVersion version)
        {
            var requestedVersion = version?.ObjId ?? Guid.Empty;
            _ = Task.Run(async () =>
            {
                try
                {
                    using var releaser = await _asyncKeyedLocker.LockOrNullAsync(nameof(UpdateHub), 10).ConfigureAwait(false);
                    if (releaser == null)
                    {
                        return;
                    }

                    await DownloadUpdate(requestedVersion);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not download the update");
                    await Administrators.SendAsync("UpdateFailed", new object[] { e.Message });
                }
            });
            return Task.CompletedTask;
        }

        private async Task DownloadUpdate(Guid requestedVersion)
        {
            // never the client's version object (it carries the download url): ask the cloud for the update
            var update = await _api.CheckForUpdates();
            if (update == null || (requestedVersion != Guid.Empty && update.ObjId != requestedVersion))
            {
                _logger.LogWarning($"Update {requestedVersion} is not available");
                await Administrators.SendAsync("UpdateFailed", new object[] { "UPDATE_NOT_AVAILABLE" });
                return;
            }

            var previousState = 0;

            void OnProgressChanged(object sender, DownloadProgressChangedEventArgs e)
            {
                if (previousState != e.ProgressPercentage)
                {
                    previousState = e.ProgressPercentage;
                    _ = Administrators.SendAsync("UpdateDownloadProgressChanged",
                        new object[] { e.BytesReceived, e.TotalBytesToReceive });
                    _logger.LogInformation(
                        $"Downloading update {e.ProgressPercentage} - {e.BytesReceived}/{e.TotalBytesToReceive}");
                }
            }

            // the cloud api is a singleton: the handler is removed again, otherwise every download adds one more
            _api.DownloadUpdateProgressChanged += OnProgressChanged;
            try
            {
                // throws if the download failed
                var fileInfo = await _api.DownloadUpdate(update);

                if (!Automatica.Core.Common.Update.Update.CheckUpdateFile(_logger, fileInfo.FullName, ServerInfo.Rid))
                {
                    _logger.LogError("The downloaded update file is invalid");
                    _api.DeleteUpdate();
                    await Administrators.SendAsync("UpdateFailed", new object[] { "UPDATE_FILE_INVALID" });
                    return;
                }

                await Administrators.SendAsync("UpdateFinished");
            }
            finally
            {
                _api.DownloadUpdateProgressChanged -= OnProgressChanged;
            }
        }

        /// <returns>the requested plugins as the cloud api offers them (unknown plugins are skipped)</returns>
        private async Task<IList<Plugin>> ResolvePlugins(IEnumerable<Plugin> requested)
        {
            var cloudPlugins = await _api.GetLatestPlugins() ?? new List<Plugin>();
            var ret = new List<Plugin>();

            foreach (var plugin in requested ?? Enumerable.Empty<Plugin>())
            {
                var cloudPlugin = plugin?.PluginGuid == null
                    ? null
                    : cloudPlugins.FirstOrDefault(a => a.PluginGuid == plugin.PluginGuid);

                if (cloudPlugin == null)
                {
                    _logger.LogWarning($"Plugin {plugin?.PluginGuid} is not offered by the cloud, ignore it");
                    continue;
                }

                ret.Add(cloudPlugin);
            }

            return ret;
        }

        private Task DownloadPlugin(Plugin plugin, bool install)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var releaser = await _asyncKeyedLocker.LockOrNullAsync(nameof(UpdateHub), 10).ConfigureAwait(false);
                    if (releaser == null)
                    {
                        return;
                    }

                    var cloudPlugin = (await ResolvePlugins(new[] { plugin })).SingleOrDefault();
                    if (cloudPlugin == null)
                    {
                        return;
                    }

                    var pluginDownloader =
                        new PluginDownloader(cloudPlugin, install, _api, _updateHub, _coreServer, _loader, _logger);

                    await pluginDownloader.Download();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Could not download plugin {plugin?.PluginGuid}");
                }
            });
            return Task.CompletedTask;
        }

        [Authorize(Policy = Role.AdminRole)]
        public Task StartPluginInstall(Plugin plugin)
        {
            return DownloadPlugin(plugin, true);
        }
        [Authorize(Policy = Role.AdminRole)]
        public Task StartPluginUpdate(Plugin plugin)
        {
            return DownloadPlugin(plugin, false);
        }

        [Authorize(Policy = Role.AdminRole)]
        public Task UpdateAllPlugin(IList<Plugin> plugins)
        {
            return InstallOrUpdatePlugins(plugins);
        }

        [Authorize(Policy = Role.AdminRole)]
        public Task InstallAllPlugin(IList<Plugin> plugins)
        {
            return InstallOrUpdatePlugins(plugins);
        }
        [Authorize(Policy = Role.AdminRole)]
        public Task InstallUpdateAllPlugin(IList<Plugin> plugins)
        {
            return InstallOrUpdatePlugins(plugins);
        }

        private async Task InstallOrUpdatePlugins(IList<Plugin> plugins)
        {
            using var releaser = await _asyncKeyedLocker.LockOrNullAsync(nameof(UpdateHub), 100).ConfigureAwait(false);
            if (releaser == null)
            {
                return;
            }

            var tasks = new List<Task>();
            foreach (var plug in await ResolvePlugins(plugins))
            {
                var pluginDownloader = new PluginDownloader(plug, false, _api, _updateHub, _coreServer,
                    _loader, _logger,false);

                tasks.Add(pluginDownloader.Download());
            }

            await Task.WhenAll(tasks.ToArray());
            _coreServer.Restart();
        }
    }
}
