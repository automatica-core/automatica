using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using Automatica.Core.Runtime.Core.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Core
{
    public static class RuleLoader
    {
        public static Task<IList<RuleFactory>> LoadSingle(ILogger logger, Plugin plugin, IConfiguration config)
        {
            var dir = Path.Combine(ServerInfo.GetBasePath(), ServerInfo.DriversDirectory, plugin.ComponentName);

            return Loader.Load<RuleFactory>(dir, "*.dll", logger, config, false);
        }

        public static Task<IList<RuleFactory>> GetRuleFactories(ILogger logger, string path, string searchPattern, IConfiguration config, bool isInDevMode)
        {
            var fileInfo = new FileInfo(path);
            string dir = fileInfo.DirectoryName;
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
            return Loader.Load<RuleFactory>(dir, searchPattern, logger, config, isInDevMode);
        }
    }
}
