using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.Runtime.Core.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Core
{
    public static class RuleLoader
    {
        public static async Task<IList<LogicFactory>> LoadSingle(ILogger logger, Plugin plugin, IConfiguration config)
        {
            var dir = Path.Combine(ServerInfo.PluginDirectory, ServerInfo.LogicsDirectory, plugin.ComponentName);

            logger.LogDebug($"Try to load logics from directory {dir}");

            if (!Directory.Exists(dir))
            {
                logger.LogError($"Could not find logic directory: {dir}");
                return new List<LogicFactory>();
            }

            return await Loader.Load<LogicFactory>(dir, "*.dll", logger, config, false);
        }

        public static Task<IList<LogicFactory>> GetRuleFactories(ILogger logger, string path, string searchPattern, IConfiguration config, bool isInDevMode)
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
            return Loader.Load<LogicFactory>(dir, searchPattern, logger, config, isInDevMode);
        }
    }
}
