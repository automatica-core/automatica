using Microsoft.AspNetCore.SignalR;
using Serilog.Configuration;
using Serilog;
using Serilog.Events;

namespace Automatica.Core.Logging.SignalR
{
    public static class LoggingExtensions
    {        public static LoggerConfiguration SignalR<THub, T>(
            this LoggerSinkConfiguration loggerConfiguration,
            IHubContext<THub, T> hub,
            string facility,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose,
            IFormatProvider? formatProvider = null,
            string[]? groups = null,
            string[]? userIds = null,
            string[]? excludedConnectionIds = null
        ) where THub : Hub<T> where T : class
        {
            if (loggerConfiguration == null) { throw new ArgumentNullException(nameof(loggerConfiguration)); }
            if (hub == null) { throw new ArgumentNullException(nameof(hub)); }

            return loggerConfiguration.Sink(new SignalRSink<THub, T>(
                hub,
                facility,
                formatProvider,
                groups,
                userIds,
                excludedConnectionIds
            ), restrictedToMinimumLevel);
        }
    }
}
