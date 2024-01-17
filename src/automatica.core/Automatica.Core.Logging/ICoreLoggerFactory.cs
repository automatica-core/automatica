using Microsoft.Extensions.Logging;

namespace Automatica.Core.Logging
{
    public interface ICoreLoggerFactory : ILoggerFactory
    {
        IEnumerable<ICoreLogger> GetLoggers();
        void SetLogLevel(string name, LogLevel level);
        void SetLogLevel(LogLevel level);
    }
}
