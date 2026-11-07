using Automatica.Core.Base.Common;

using Automatica.Core.EF.Models;
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
    /// <summary>
    /// Downloads (and installs) a plugin. The <see cref="Plugin"/> must come from the cloud api, never from a client.
    /// </summary>
    public class PluginDownloader
    {
        private readonly Plugin _plugin;
        private readonly bool _install;
        private readonly ICloudApi _api;
        private readonly IHubContext<UpdateHub> _updateHub;
        private readonly ICoreServer _coreServer;
        private readonly ILogger _logger;
        private readonly bool _restartOnUpdate;
        private int _previousState;

        /// <param name="install">install (or update) right away</param>
        /// <param name="restartOnUpdate">update right away, false: only download, the caller installs it (<see cref="StagedFile"/>)</param>
        public PluginDownloader(Plugin plugin, bool install, ICloudApi cloudApi, IHubContext<UpdateHub> updateHub, ICoreServer coreServer, ILogger logger, bool restartOnUpdate = true)
        {
            _plugin = plugin;
            _install = install;
            _api = cloudApi;
            _updateHub = updateHub;
            _coreServer = coreServer;
            _logger = logger;
            _restartOnUpdate = restartOnUpdate;
        }

        public const string InstallFailedMessage = "The plugin could not be installed, see the server log";

        public Plugin Plugin => _plugin;

        /// <summary>
        /// The downloaded and verified package in the plugin update directory, null if the download failed
        /// </summary>
        public string StagedFile { get; private set; }

        private IClientProxy Administrators => _updateHub.Clients.Group(HubAccess.AdministratorsGroup);

        /// <returns>the download target in the plugin update directory, null if the file name is not a plain file name</returns>
        private string GetUpdateFilePath()
        {
            var fileName = _plugin.AzureFileName;
            if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName ||
                fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || fileName == "." || fileName == "..")
            {
                return null;
            }

            // private staging directory (0700), never the world writable temp directory
            return Path.Combine(ServerInfo.GetPluginUpdateDirectory(), fileName);
        }

        /// <summary>
        /// Tells the administrators that the plugin was not installed (the plugin page shows the message), the error is logged by the caller
        /// </summary>
        internal static async Task SendFailed(IHubContext<UpdateHub> updateHub, ILogger logger, Guid? pluginGuid, string message)
        {
            try
            {
                await updateHub.Clients.Group(HubAccess.AdministratorsGroup).SendAsync("PluginFailed", new object[] { pluginGuid, message });
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Could not send the plugin failure");
            }
        }

        private Task SendFailed(string message) => SendFailed(_updateHub, _logger, _plugin.PluginGuid, message);

        public async Task Download()
        {
            var automaticaPluginUpdateFile = GetUpdateFilePath();
            if (automaticaPluginUpdateFile == null)
            {
                _logger.LogError($"Invalid plugin file name for {_plugin.ComponentName}, plugin is not downloaded");
                await SendFailed($"Invalid plugin file name {_plugin.AzureFileName}");
                return;
            }

            if (!_api.IsTrustedDownloadUrl(_plugin.AzureUrl, out var reason))
            {
                _logger.LogError($"Plugin {_plugin.ComponentName} is not downloaded: {reason}");
                await SendFailed($"The plugin is not downloaded: {reason}");
                return;
            }

#pragma warning disable SYSLIB0014 //ignore for now, need to have a download progress
            using var webClient = new WebClient();
#pragma warning restore SYSLIB0014
            var downloaded = false;

            try
            {
                webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                await webClient.DownloadFileTaskAsync(_plugin.AzureUrl, automaticaPluginUpdateFile);
                downloaded = true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not download file for {_plugin.ComponentName}");
                DeleteFile(automaticaPluginUpdateFile);
                await SendFailed($"Could not download the plugin: {e.Message}");
            }
            finally
            {
                webClient.DownloadProgressChanged -= WebClient_DownloadProgressChanged;
            }

            if (downloaded)
            {
                try
                {
                    // fail early: a staged plugin is verified again before it is installed
                    Automatica.Core.Common.Update.Plugin.VerifyPluginPackage(_logger, automaticaPluginUpdateFile, _plugin.ComponentName);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Plugin package for {_plugin.ComponentName} is not trusted, it is not installed");
                    DeleteFile(automaticaPluginUpdateFile);
                    // e.g. "Package ... is not signed (set AUTOMATICA_ALLOW_UNSIGNED_PACKAGES=true to accept unsigned packages)"
                    await SendFailed(e.Message);
                    return;
                }

                await DownloadCompleted(automaticaPluginUpdateFile);
            }
        }

        private async Task DownloadCompleted(string automaticaPluginUpdateFile)
        {
            // awaited by Download (no async void event handler), an install error must not crash the process
            try
            {
                StagedFile = automaticaPluginUpdateFile;
                await Administrators.SendAsync("PluginFinished", new object[] {_plugin.PluginGuid});
                if (!_install && !_restartOnUpdate)
                {
                    // the caller installs all downloaded plugins together
                    return;
                }

                // without a restart where possible: only the plugin's drivers and logics are restarted
                var summary = await _coreServer.UpdatePlugins(new[] { new PluginUpdate(_plugin, automaticaPluginUpdateFile) });
                if (summary.RestartRequired)
                {
                    _coreServer.Restart();
                    return;
                }

                if (summary.Failed.Count > 0)
                {
                    await SendFailed(InstallFailedMessage);
                    return;
                }

                await Administrators.SendAsync("PluginLoaded", new object[] { _plugin.PluginGuid });
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not install plugin {_plugin.ComponentName}");
                await SendFailed($"Could not install the plugin: {e.Message}");
            }
        }

        private void DeleteFile(string file)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Could not delete {file}");
            }
        }

        private async void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            // async void event handler: never let an exception escape
            try
            {
                if (_previousState != e.ProgressPercentage)
                {
                    _previousState = e.ProgressPercentage;
                    await Administrators.SendAsync("PluginDownloadProgressChanged", new object[] { _plugin.PluginGuid, e.BytesReceived, e.TotalBytesToReceive });
                    _logger.LogInformation($"Downloading plugin {_plugin.ComponentName} {e.ProgressPercentage} - {e.BytesReceived}/{e.TotalBytesToReceive}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not send the plugin download progress");
            }
        }
    }
}
