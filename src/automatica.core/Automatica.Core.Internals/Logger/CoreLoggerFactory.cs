using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Automatica.Core.Base.Logger;

namespace Automatica.Core.Internals.Logger
{
    public class CoreLoggerFactory : ILoggerFactory
    {
        private static readonly object Lock = new();
        private static readonly IDictionary<string, ILogger> LoggerInstances = new ConcurrentDictionary<string, ILogger>();
        
        public IConfiguration Configuration { get; }

        public CoreLoggerFactory(IConfiguration config)
        {
            Configuration = config;
        }

        public static ILogger GetLogger(IConfiguration config, string name)
        {
            lock (Lock)
            {
                if (!LoggerInstances.ContainsKey(name))
                {
                    LoggerInstances.Add(name, new CoreLogger(config, name));
                }

                return LoggerInstances[name];

            }
        }
        public static ILogger GetLogger(IConfiguration config, string name, LogLevel? level, bool isFrameworkLog)
        {
            lock (Lock)
            {
                if (!LoggerInstances.ContainsKey(name))
                {
                    LoggerInstances.Add(name, new CoreLogger(config, name, level, isFrameworkLog));
                }

                return LoggerInstances[name];
            }
        }

        public void Dispose()
        {
            
        }

        public ILogger CreateLogger(string categoryName)
        {
            return GetLogger(Configuration, categoryName, null, categoryName.Contains("Microsoft") || categoryName.Contains("System"));
        }

        public void AddProvider(ILoggerProvider provider)
        {
            
        }
    }
}
