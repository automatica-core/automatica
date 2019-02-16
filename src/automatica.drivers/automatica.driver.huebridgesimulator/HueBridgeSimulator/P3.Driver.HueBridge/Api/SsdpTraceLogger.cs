using Microsoft.Extensions.Logging;
using Rssdp;

namespace P3.Driver.HueBridge.Api
{
    internal class SsdpTraceLogger : ISsdpLogger
    {
        public SsdpTraceLogger(ILogger logger)
        {
            Logger = logger;
        }

        public ILogger Logger { get; }

        public void LogError(string message)
        {
            Logger.LogError(message);
        }
    
        public void LogInfo(string message)
        {
            Logger.LogInformation(message);
        }

        public void LogVerbose(string message)
        {
            Logger.LogDebug(message);
        }

        public void LogWarning(string message)
        {
            Logger.LogWarning(message);
        }
    }
}