using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Automatica.Core.Base.Common;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Core.Plugins
{
    public static class PluginLoader
    {
        public static IList<DriverFactory> LoadSingle(ILogger logger, Plugin plugin, AutomaticaContext database)
        {
            var dir = Path.Combine(ServerInfo.GetBasePath(), ServerInfo.DriversDirectory, plugin.ComponentName);

            return Loader.Load<DriverFactory>(dir, "*.dll", logger, database, false);
        }

        public static IList<DriverFactory> GetDriverFactories(ILogger logger, string path, string searchPattern, AutomaticaContext database, bool isInDevMode)
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

            return Loader.Load<DriverFactory>(dir, searchPattern, logger, database, isInDevMode);
        }
    }
}
