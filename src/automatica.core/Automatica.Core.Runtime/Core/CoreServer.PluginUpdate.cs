using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Core;
using Automatica.Core.Internals.Plugins;
using Automatica.Core.Runtime.Core.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Core
{
    /// <summary>
    /// Plugin updates without a process restart, see <see cref="PluginUpdater"/>
    /// </summary>
    public partial class CoreServer : IPluginUpdateTarget
    {
        private readonly SemaphoreSlim _pluginUpdateLock = new(1, 1);

        public async Task<PluginUpdateSummary> UpdatePlugins(IReadOnlyCollection<PluginUpdate> updates)
        {
            if (updates.Count == 0)
            {
                return PluginUpdateSummary.None;
            }

            await _pluginUpdateLock.WaitAsync();
            try
            {
                return await new PluginUpdater(this, _logger).UpdateAll(updates);
            }
            finally
            {
                _pluginUpdateLock.Release();
            }
        }

        bool IPluginUpdateTarget.IsStarted => RunState == RunState.Started;

        void IPluginUpdateTarget.VerifyPackage(Plugin plugin, string packageFile)
        {
            Common.Update.Plugin.VerifyPluginPackage(_logger, packageFile, plugin.ComponentName);
        }

        async Task<PluginInstances> IPluginUpdateTarget.StopAndRelease(PluginLoadContext context, Plugin plugin)
        {
            // drivers: the instance or its factory is a type of the plugin
            var drivers = _driverStore.All().Where(a => context.Owns(a) || context.Owns(a.DriverContext?.Factory)).ToList();
            var driverNodeIds = drivers.Select(a => a.DriverContext.NodeInstance.ObjId).ToList();

            foreach (var driver in drivers)
            {
                await StopDriver(driver);
            }

            foreach (var driver in drivers)
            {
                try
                {
                    await driver.Stopped();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error in Stopped of driver {driver.Name}");
                }
            }
            drivers.Clear();

            // logics: the links stay, they look up the logic instance when a value is dispatched
            var logics = _logicInstanceStore.Dictionary().Where(a => context.Owns(a.Value)).ToList();
            var ruleInstanceIds = logics.Select(a => a.Key.ObjId).ToList();
            foreach (var logic in logics)
            {
                await StopLogic(logic.Value, logic.Key, false);
                _logicInstanceStore.Remove(logic.Key);
                _logicStore.Remove(logic.Key);
            }
            logics.Clear();

            // factories and manifest: a new version is only added if the key is free
            foreach (var factory in _driverFactoryStore.All().Where(context.Owns).ToList())
            {
                _driverFactoryStore.RemoveDriver(factory.DriverGuid);
            }

            foreach (var factory in _logicFactoryStore.All().Where(context.Owns).ToList())
            {
                _logicFactoryStore.Remove(factory.LogicGuid);
            }

            if (plugin.PluginGuid is { } pluginGuid)
            {
                _store.Remove(pluginGuid);
            }

            _logger.LogInformation($"Plugin {plugin.ComponentName}: stopped {driverNodeIds.Count} drivers and {ruleInstanceIds.Count} logics for the update");
            return new PluginInstances(driverNodeIds, ruleInstanceIds);
        }

        Task<bool> IPluginUpdateTarget.Install(Plugin plugin, string packageFile)
        {
            return _serviceProvider.GetRequiredService<IPluginInstaller>().InstallPlugin(plugin, packageFile);
        }

        Task IPluginUpdateTarget.Load(Plugin plugin)
        {
            return _pluginHandler.LoadPlugin(plugin);
        }

        async Task<bool> IPluginUpdateTarget.Start(PluginInstances instances)
        {
            var success = true;
            foreach (var nodeInstanceId in instances.DriverNodeInstanceIds)
            {
                var nodeInstance = _nodeInstanceCache.Get(nodeInstanceId);
                var template = nodeInstance?.This2NodeTemplate is { } templateId ? _nodeTemplateCache.Get(templateId) : null;
                if (nodeInstance == null || template == null || !_driverFactoryStore.Contains(template.ObjId))
                {
                    _logger.LogError($"Could not start driver {nodeInstanceId} after the plugin update, the plugin does not provide it");
                    success = false;
                    continue;
                }

                await InitializeAndStartDriver(nodeInstance, template);
            }

            foreach (var ruleInstanceId in instances.RuleInstanceIds)
            {
                try
                {
                    var logic = InitLogicInstance(ruleInstanceId);
                    await StartLogic(logic.Value, logic.Key);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Could not start logic {ruleInstanceId} after the plugin update");
                    success = false;
                }
            }

            return success;
        }
    }
}
