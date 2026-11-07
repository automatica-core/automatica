using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Core;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Core.Plugins
{
    internal enum PluginUpdateResult
    {
        /// <summary>installed and running, no restart needed</summary>
        Updated,
        /// <summary>not installed (e.g. invalid package), a restart would not change that</summary>
        Failed,
        /// <summary>could not be done in place, the package stays staged and is installed on the next start</summary>
        RestartRequired
    }

    /// <summary>
    /// The drivers and logics that ran on a plugin before it was unloaded
    /// </summary>
    internal record PluginInstances(IReadOnlyList<Guid> DriverNodeInstanceIds, IReadOnlyList<Guid> RuleInstanceIds)
    {
        public int Count => DriverNodeInstanceIds.Count + RuleInstanceIds.Count;
    }

    /// <summary>
    /// What the in-place update needs from the core server
    /// </summary>
    internal interface IPluginUpdateTarget
    {
        bool IsStarted { get; }

        /// <summary>
        /// Throws if the package is not trusted
        /// </summary>
        void VerifyPackage(Plugin plugin, string packageFile);

        /// <summary>
        /// Stops the drivers and logics of the plugin and removes every reference to its types the core keeps (factories, instances)
        /// </summary>
        Task<PluginInstances> StopAndRelease(PluginLoadContext context, Plugin plugin);

        Task<bool> Install(Plugin plugin, string packageFile);

        Task Load(Plugin plugin);

        /// <returns>false if a driver or logic could not be created with the loaded plugin</returns>
        Task<bool> Start(PluginInstances instances);
    }

    /// <summary>
    /// Updates plugins without a process restart: stop the plugin's drivers and logics, unload its load context,
    /// install the package, load it into a new context and start the drivers and logics again.
    /// Everything that can not be done in place falls back to the install on restart.
    /// </summary>
    internal class PluginUpdater
    {
        internal static TimeSpan UnloadCheckDelay = TimeSpan.FromSeconds(10);

        private readonly IPluginUpdateTarget _target;
        private readonly ILogger _logger;

        public PluginUpdater(IPluginUpdateTarget target, ILogger logger)
        {
            _target = target;
            _logger = logger;
        }

        public static string GetPluginFolder(Plugin plugin)
        {
            return Path.Combine(ServerInfo.PluginDirectory,
                plugin.PluginType == PluginType.Driver ? ServerInfo.DriversDirectory : ServerInfo.LogicsDirectory,
                plugin.ComponentName);
        }

        /// <returns>true if a restart is still required</returns>
        public async Task<bool> UpdateAll(IReadOnlyCollection<PluginUpdate> updates, Func<Plugin, string>? pluginFolder = null)
        {
            pluginFolder ??= GetPluginFolder;
            var restart = false;
            foreach (var update in updates)
            {
                PluginUpdateResult result;
                try
                {
                    result = await Update(update.Plugin, update.PackageFile, pluginFolder(update.Plugin));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Could not update plugin {update.Plugin.ComponentName} without a restart");
                    result = PluginUpdateResult.RestartRequired;
                }

                if (result == PluginUpdateResult.RestartRequired)
                {
                    // installed by CheckAndInstallPluginUpdates on the next start
                    restart = true;
                }
                else
                {
                    DeleteFile(update.PackageFile);
                }
            }

            return restart;
        }

        internal async Task<PluginUpdateResult> Update(Plugin plugin, string packageFile, string folder)
        {
            var name = plugin.ComponentName;
            if (!_target.IsStarted)
            {
                _logger.LogInformation($"Plugin {name}: the server is not started, the update is installed with a restart");
                return PluginUpdateResult.RestartRequired;
            }

            try
            {
                // before anything is stopped, the installer verifies it again
                _target.VerifyPackage(plugin, packageFile);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Plugin package for {name} is not trusted, it is not installed");
                return PluginUpdateResult.Failed;
            }

            var state = GetState(folder);
            if (state == LoadState.NotLoaded)
            {
                // not loaded (new plugin): nothing to stop
                if (!await _target.Install(plugin, packageFile))
                {
                    return PluginUpdateResult.Failed;
                }
                await _target.Load(plugin);
                _logger.LogInformation($"Plugin {name} installed and loaded");
                return PluginUpdateResult.Updated;
            }

            if (state == LoadState.NativeLibrariesLoaded)
            {
                _logger.LogInformation($"Plugin {name} uses native libraries, they can not be unloaded: the update is installed with a restart");
                return PluginUpdateResult.RestartRequired;
            }

            var timer = new StartupTimer();
            var (instances, unloaded) = await timer.Measure("stop", () => StopAndUnload(folder, plugin));
            if (OperatingSystem.IsWindows())
            {
                // the old files are locked until the context is collected
                WaitForUnload(unloaded, 5);
            }

            bool installed;
            try
            {
                installed = await timer.Measure("install", () => _target.Install(plugin, packageFile));
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not install plugin {name}");
                installed = false;
            }

            // a failed install mostly leaves the old version in place (it is removed only after the package is verified and extracted): load what is there
            await timer.Measure("load", () => _target.Load(plugin));
            var started = await timer.Measure($"start {instances.Count} instances", () => _target.Start(instances));

            timer.Log(_logger, $"Plugin update {name}");
            _ = CheckUnloaded(unloaded, name);

            if (!started)
            {
                _logger.LogError($"Plugin {name}: not every driver or logic could be started after the update, restart");
                return PluginUpdateResult.RestartRequired;
            }

            return installed ? PluginUpdateResult.Updated : PluginUpdateResult.Failed;
        }

        private enum LoadState
        {
            NotLoaded,
            NativeLibrariesLoaded,
            Loaded
        }

        // the context is only touched in these methods: a local of the long running async method would keep it alive (debug builds keep all locals)
        private static LoadState GetState(string folder)
        {
            if (!PluginLoadContexts.TryGet(folder, out var context))
            {
                return LoadState.NotLoaded;
            }

            return context.NativeLibrariesLoaded ? LoadState.NativeLibrariesLoaded : LoadState.Loaded;
        }

        private async Task<(PluginInstances Instances, WeakReference? Unloaded)> StopAndUnload(string folder, Plugin plugin)
        {
            PluginInstances instances;
            if (PluginLoadContexts.TryGet(folder, out var context))
            {
                instances = await _target.StopAndRelease(context, plugin);
            }
            else
            {
                instances = new PluginInstances(Array.Empty<Guid>(), Array.Empty<Guid>());
            }

            return (instances, PluginLoadContexts.Unload(folder));
        }

        internal static bool WaitForUnload(WeakReference? unloaded, int attempts)
        {
            for (var i = 0; unloaded is { IsAlive: true } && i < attempts; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            return unloaded is not { IsAlive: true };
        }

        private async Task CheckUnloaded(WeakReference? unloaded, string name)
        {
            try
            {
                await Task.Delay(UnloadCheckDelay);
                if (!WaitForUnload(unloaded, 10))
                {
                    // something still references the old types (a static event, a thread that did not stop,...), harmless until the next restart
                    _logger.LogWarning($"Plugin {name}: the old version could not be unloaded, it stays in memory until the next restart");
                }
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, "Could not check the plugin unload");
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
    }
}
