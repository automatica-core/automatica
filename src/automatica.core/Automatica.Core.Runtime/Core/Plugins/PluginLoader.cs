using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
        public static Task<IList<DriverFactory>> LoadSingle(ILogger logger, Plugin plugin, IConfiguration config)
        {
            var dir = Path.Combine(ServerInfo.GetBasePath(), ServerInfo.DriversDirectory, plugin.ComponentName);

            return Loader.Load<DriverFactory>(dir, "*.dll", logger, config, false);
        }

        public static Task<IList<DriverFactory>> GetDriverFactories(ILogger logger, string path, string searchPattern, IConfiguration config, bool isInDevMode)
        {
            var fileInfo = new FileInfo(path);
            string dir = fileInfo.DirectoryName;
            if (fileInfo.Attributes == FileAttributes.Directory)
            {
                dir = path;
            }
            var driverPath = Path.Combine(dir, ServerInfo.DriversDirectory);

            if (!Directory.Exists(driverPath))
            {
                Directory.CreateDirectory(driverPath);
                dir = driverPath;
            }

            return Loader.Load<DriverFactory>(dir, searchPattern, logger, config, isInDevMode);
        }
    }
}
