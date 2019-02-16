using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Automatica.Core.Internals.Logger
{
    public class CoreLogger : Microsoft.Extensions.Logging.ILogger
    {
        private readonly string _facility;
        private readonly LogLevel _level;
        private readonly Serilog.ILogger _logger;

        public CoreLogger() : this(null)
        {

        }

        public CoreLogger(string facility) : this(facility, LogLevel.Debug)
        {
            
        }

        public CoreLogger(string facility, LogLevel level, bool isFrameworkLog = false) {
            _facility = facility;
            _level = level;

            if(string.IsNullOrEmpty(facility))
            {
               facility = "dotnet"; 
            }
            facility = facility.ToLower();

            var logBuild = new LoggerConfiguration();

            if (!isFrameworkLog)
            {
                logBuild.WriteTo.RollingFile(Path.Combine("logs", $"{facility}.log"), fileSizeLimitBytes: 31457280,
                        retainedFileCountLimit: 10, restrictedToMinimumLevel: ConvertLogLevel(level),
                        flushToDiskInterval: TimeSpan.FromSeconds(30))
                    .WriteTo.Console();
            }
            else
            {
                logBuild.WriteTo.RollingFile(Path.Combine("framework", "logs", $"{facility}.log"), fileSizeLimitBytes: 31457280,
                    retainedFileCountLimit: 2, restrictedToMinimumLevel: ConvertLogLevel(level),
                    flushToDiskInterval: TimeSpan.FromSeconds(30));
            }

            switch (_level)
            {
                case LogLevel.Trace:
                    logBuild.MinimumLevel.Verbose();
                    break;
                case LogLevel.Debug:
                    logBuild.MinimumLevel.Debug();
                    break;
                case LogLevel.Information:
                    logBuild.MinimumLevel.Information();
                    break;
                case LogLevel.Warning:
                    logBuild.MinimumLevel.Warning();
                    break;
                case LogLevel.Error:
                    logBuild.MinimumLevel.Error();
                    break;
                case LogLevel.Critical:
                    logBuild.MinimumLevel.Fatal();
                    break;
                case LogLevel.None:
                    break;
                default:
                    logBuild.MinimumLevel.Error();
                    break;
            }

            _logger = logBuild.CreateLogger();
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            var level = ConvertLogLevel(logLevel);
            return _logger.IsEnabled(level);
        }

        private LogEventLevel ConvertLogLevel(LogLevel logLevel)
        {
            LogEventLevel level = LogEventLevel.Verbose;
            switch (logLevel)
            {
                case LogLevel.Trace:
                    level = LogEventLevel.Verbose;
                    break;
                case LogLevel.Debug:
                    level = LogEventLevel.Debug;
                    break;
                case LogLevel.Information:
                    level = LogEventLevel.Information;
                    break;
                case LogLevel.Warning:
                    level = LogEventLevel.Warning;
                    break;
                case LogLevel.Error:
                    level = LogEventLevel.Error;
                    break;
                case LogLevel.Critical:
                    level = LogEventLevel.Fatal;
                    break;
                case LogLevel.None:
                    level = LogEventLevel.Verbose;
                    break;
            }
            return level;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            var level = ConvertLogLevel(logLevel);
            var msg = $"{_facility.ToLower()}: {formatter.Invoke(state, exception)}";
            _logger.Write(level, exception, msg);
        }
    }
}