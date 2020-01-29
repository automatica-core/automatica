using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Plugins;
using Automatica.Core.Runtime.Abstraction.Plugins;
using Automatica.Core.Runtime.Abstraction.Plugins.Driver;
using Automatica.Core.Runtime.Abstraction.Plugins.Logic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Core.Plugins
{
    internal class PluginHandler : IPluginHandler
    {
        private readonly ILogger<PluginHandler> _logger;
        private readonly AutomaticaContext _dbContext;
        private readonly IDriverLoader _driverLoader;
        private readonly ILogicLoader _logicLoader;
        private readonly INodeTemplateCache _nodeTemplateCache;
        private readonly IConfiguration _config;
        private readonly IPluginInstaller _pluginInstaller;
        private readonly object _lock = new object();

        public PluginHandler(ILogger<PluginHandler> logger, AutomaticaContext dbContext, IDriverLoader driverLoader, ILogicLoader logicLoader, INodeTemplateCache nodeTemplateCache, IConfiguration config, IPluginInstaller pluginInstaller)
        {
            _logger = logger;
            _dbContext = dbContext;
            _driverLoader = driverLoader;
            _logicLoader = logicLoader;
            _nodeTemplateCache = nodeTemplateCache;
            _config = config;
            _pluginInstaller = pluginInstaller;
        }

        public async Task CheckAndInstallPluginUpdates()
        {
            var updateDirectory = Path.Combine(ServerInfo.GetTempPath(), ServerInfo.PluginUpdateDirectoryName);
            if (!Directory.Exists(updateDirectory))
            {
                Directory.CreateDirectory(updateDirectory);
                return;
            }

            var files = Directory.GetFiles(updateDirectory);

            foreach (var f in files)
            {
                var tmp = Path.Combine(ServerInfo.GetTempPath(), Guid.NewGuid().ToString().Replace("-", ""));
                try
                {
                    var manifest = Common.Update.Plugin.GetPluginManifest(_logger, f, tmp);
                    await _pluginInstaller.InstallPlugin(manifest, f);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not install plugin");
                }
                finally
                {
                    Directory.Delete(tmp, true);
                }
                File.Delete(f);
            }
        }

        public async Task LoadPlugin(Plugin plugin)
        {
            if (plugin.PluginType == PluginType.Driver)
            {
                var factories = PluginLoader.LoadSingle(_logger, plugin, _config);

                foreach (var factory in await factories)
                {
                    await _driverLoader.Load(factory, ServerInfo.BoardType);
                }
            }
            else if (plugin.PluginType == PluginType.Logic)
            {
                var factories = RuleLoader.LoadSingle(_logger, plugin, _config);

                foreach (var factory in await factories)
                {
                    await _logicLoader.Load(factory, ServerInfo.BoardType);
                }
            }

            _nodeTemplateCache.Clear();

        }

        private string GetEntryAssemblyPath()
        {
            return Assembly.GetEntryAssembly()?.Location;
        }
    }
}
