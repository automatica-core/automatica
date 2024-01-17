using Microsoft.Extensions.Logging;

namespace Automatica.Core.Logging
{
    public interface ICoreLoggerSettings
    {
        LogLevel GetLogLevel(string facility);
        void Save(IDictionary<string, ICoreLogger> logLevels);
    }
}
