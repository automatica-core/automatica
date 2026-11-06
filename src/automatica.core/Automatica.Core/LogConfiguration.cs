using System.Diagnostics;
using System.IO;
using Automatica.Core.Base.Common;
using Serilog;
using Serilog.Events;

namespace Automatica.Core
{
    internal static class LogConfiguration
    {
        public static LoggerConfiguration ConfigureLogger()
        {
            var logBuild = new LoggerConfiguration()
                // rolls over at the size limit (without rollOnFileSizeLimit the sink stops writing for good once the file is full)
                .WriteTo.File(Path.Combine(ServerInfo.GetLogDirectory(), "dotnet.log"), retainedFileCountLimit: 10,
                    fileSizeLimitBytes: 5 * 1024 * 1024, rollOnFileSizeLimit: true)
                .MinimumLevel.Verbose()
                // request logs contain urls (SignalR passes the access token in the query string)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.With(new AccessTokenRedactionEnricher());

            // enable log to stdout only in docker and if debugger is attached to prevent syslog from writing to much data
            if (Debugger.IsAttached || Runtime.BoardTypes.Docker.Docker.InDocker)
            {
                logBuild.WriteTo.Console();
            }

            return logBuild;
        }
    }
}
