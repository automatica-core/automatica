using System;
using System.IO;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.Common.Update;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Plugin = Automatica.Core.EF.Models.Plugin;

namespace Automatica.Core.Runtime.Core.Plugins
{
    internal class PluginInstaller : IPluginInstaller
    {
        private readonly ILogger _logger;

        public PluginInstaller(IConfiguration config, ILogger<PluginInstaller> logger)
        {
            _logger = logger;
        }
        public Task<bool> InstallPlugin(Plugin plugin, string acPkgFilePath)
        {
            return InstallPlugin(plugin.PluginType, plugin.ComponentName, acPkgFilePath);
        }

        public Task<bool> InstallPlugin(PluginManifest plugin, string acPkgFilePath)
        {
            PluginType pluginType;

            switch (plugin.Automatica.Type)
            {
                case "driver":
                    pluginType = PluginType.Driver;
                    break;
                case "rule":
                    pluginType = PluginType.Logic;
                    break;
                default:
                    throw new NotImplementedException($"PluginType {plugin.Automatica.Type} is not supported");
            }

            return InstallPlugin(pluginType, plugin.Automatica.ComponentName, acPkgFilePath);
        }

        private Task<bool> InstallPlugin(PluginType pluginType, string componentName, string acPkgFilePath)
        {

            _logger.LogDebug($"Start install plugin {componentName} from file {acPkgFilePath}");

            var tmpDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tmpDirectory);

                var installDir = Path.Combine(ServerInfo.PluginDirectory,
                    pluginType == PluginType.Driver
                        ? ServerInfo.DriversDirectory
                        : ServerInfo.LogicsDirectory, componentName);

                if (Directory.Exists(installDir))
                {
                    Directory.Delete(installDir, true);
                }

                _logger.LogDebug($"Extract plugin to {tmpDirectory}");
                Common.Update.Plugin.InstallPlugin(acPkgFilePath, tmpDirectory);

                if (!Directory.Exists(installDir))
                {
                    _logger.LogDebug($"Create target directory {installDir}");
                    Directory.CreateDirectory(installDir);
                }

                foreach (var file in Directory.GetFiles(Path.Combine(tmpDirectory, componentName)))
                {
                    var fileInfo = new FileInfo(file);

                    var destination = Path.Combine(installDir, fileInfo.Name);

                    _logger.LogDebug($"Copy file from {file} to {destination}");
                    File.Copy(file, destination, true);
                }
                _logger.LogDebug($"Done install plugin...");
                return Task.FromResult(true);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not install plugin...");
            }
            finally
            {
                Directory.Delete(tmpDirectory, true);
            }

            return Task.FromResult(false);
        }
    }
}
