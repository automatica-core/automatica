using System.Diagnostics;
using System.IO;
using Serilog;

namespace Automatica.Core
{
    internal static class LogConfiguration
    {
        public static LoggerConfiguration ConfigureLogger()
        {
            var logBuild = new LoggerConfiguration()
                .WriteTo.RollingFile(Path.Combine("logs", "dotnet.log"), retainedFileCountLimit: 10, fileSizeLimitBytes: 1024 * 30)
                .MinimumLevel.Verbose();

            // enable log to stdout only in docker and if debugger is attached to prevent syslog from writing to much data
            if (Debugger.IsAttached || Runtime.BoardTypes.Docker.Docker.InDocker)
            {
                logBuild.WriteTo.Console();
            }

            return logBuild;
        }
    }
}
