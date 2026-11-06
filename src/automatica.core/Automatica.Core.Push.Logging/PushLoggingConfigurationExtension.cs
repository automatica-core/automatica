using Automatica.Core.Logging.SignalR;
using Automatica.Core.Logging.SignalR.Interfaces;
using Automatica.Push.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Configuration;
using Serilog.Events;

namespace Automatica.Core.Push.Logging
{
    public static class PushLoggingConfigurationExtension
    {

        public static LoggerSinkConfiguration PushSignalR(this LoggerSinkConfiguration config, IServiceProvider? serviceProvider, string facility, LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose)
        {
            if (serviceProvider != null)
            {
                var loggingHub = serviceProvider.GetRequiredService<IHubContext<LoggingHub, ISerilogHub>>();
                // logs are admin-only, only the administrator connections are in this group
                config.SignalR(loggingHub, facility, restrictedToMinimumLevel: restrictedToMinimumLevel, groups: new[] { HubAccess.AdministratorsGroup });

            }

            return config;
        }
    }
}