using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Core.Plugins
{
    public static class PluginLoader
    {
        public static async Task<IList<DriverFactory>> LoadSingle(ILogger logger, Plugin plugin, IConfiguration config)
        {
            var dir = Path.Combine(ServerInfo.PluginDirectory, ServerInfo.DriversDirectory, plugin.ComponentName);

            logger.LogDebug($"Try to load drivers from directory {dir}");

            if (!Directory.Exists(dir))
            {
                logger.LogError($"Could not find driver directory: {dir}");
                return new List<DriverFactory>();
            }

            return await Loader.Load<DriverFactory>(dir, "*.dll", logger, config, false);
        }

        public static Task<IList<DriverFactory>> GetDriverFactories(ILogger logger, string path, string searchPattern, IConfiguration config, bool isInDevMode)
        {
            var fileInfo = new FileInfo(path);
            string dir = fileInfo.FullName;
            if (fileInfo.Attributes == FileAttributes.Directory)
            {
                dir = path;
            }
            var driverPath = dir;

            if (!Directory.Exists(driverPath))
            {
                Directory.CreateDirectory(driverPath);
                dir = driverPath;
            }

            return Loader.Load<DriverFactory>(dir, searchPattern, logger, config, isInDevMode);
        }
    }
}
