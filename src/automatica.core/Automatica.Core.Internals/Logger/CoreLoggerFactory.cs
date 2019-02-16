using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;

namespace Automatica.Core.Internals.Logger
{
    public class CoreLoggerFactory : ILoggerFactory
    {
        private static readonly Dictionary<string, ILogger> _loggerInstances = new Dictionary<string, ILogger>();

        public static ILogger GetLogger(string name)
        {
            if (!_loggerInstances.ContainsKey(name))
            {
                _loggerInstances.Add(name, new CoreLogger(name));
            }
            return _loggerInstances[name];
        }
        public static ILogger GetLogger(string name, LogLevel level, bool isFrameworkLog)
        {
            if (!_loggerInstances.ContainsKey(name))
            {
                _loggerInstances.Add(name, new CoreLogger(name, level, isFrameworkLog));
            }
            return _loggerInstances[name];
        }

        public void Dispose()
        {
            
        }

        public ILogger CreateLogger(string categoryName)
        {
            return GetLogger(categoryName, LogLevel.Error, true);
        }

        public void AddProvider(ILoggerProvider provider)
        {
            
        }
    }
}
