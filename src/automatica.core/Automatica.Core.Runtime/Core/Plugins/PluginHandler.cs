using System;
using System.IO;
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
    internal class PluginHandler(ILogger<PluginHandler> logger, IDriverLoader driverLoader, ILogicLoader logicLoader,
            INodeTemplateCache nodeTemplateCache, IConfiguration config, IPluginInstaller pluginInstaller)
        : IPluginHandler
    {
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
                    var manifest = Common.Update.Plugin.GetPluginManifest(logger, f, tmp);
                    await pluginInstaller.InstallPlugin(manifest, f);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Could not install plugin");
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
                var factories = PluginLoader.LoadSingle(logger, plugin, config);

                foreach (var factory in await factories)
                {
                    await driverLoader.Load(factory, ServerInfo.BoardType);
                }
            }
            else if (plugin.PluginType == PluginType.Logic)
            {
                var factories = RuleLoader.LoadSingle(logger, plugin, config);

                foreach (var factory in await factories)
                {
                    await logicLoader.Load(factory, ServerInfo.BoardType);
                }
            }

            nodeTemplateCache.Clear();

        }
    }
}
