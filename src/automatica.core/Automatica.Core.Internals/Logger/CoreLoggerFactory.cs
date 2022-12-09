using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Automatica.Core.Base.Logger;

namespace Automatica.Core.Internals.Logger
{
    public class CoreLoggerFactory : ILoggerFactory
    {
        private static readonly object _lock = new object();
        private static readonly IDictionary<string, ILogger> _loggerInstances = new ConcurrentDictionary<string, ILogger>();
        
        public static IConfiguration Configuration { get; set; }

        public static ILogger GetLogger(string name)
        {
            lock (_lock)
            {
                if (!_loggerInstances.ContainsKey(name))
                {
                    _loggerInstances.Add(name, new CoreLogger(name));
                }

                return _loggerInstances[name];

            }
        }
        public static ILogger GetLogger(string name, LogLevel level, bool isFrameworkLog)
        {
            lock (_lock)
            {
                if (!_loggerInstances.ContainsKey(name))
                {
                    _loggerInstances.Add(name, new CoreLogger(name, level, isFrameworkLog));
                }

                return _loggerInstances[name];
            }
        }

        public void Dispose()
        {
            
        }

        public ILogger CreateLogger(string categoryName)
        {
            return GetLogger(categoryName, LogLevel.Error, !categoryName.Contains(LoggerConstants.FileSeparator));
        }

        public void AddProvider(ILoggerProvider provider)
        {
            
        }
    }
}
