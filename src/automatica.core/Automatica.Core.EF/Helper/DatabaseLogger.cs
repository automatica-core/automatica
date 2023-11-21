using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Automatica.Core.EF.Helper
{
    class DatabaseLogger : Microsoft.Extensions.Logging.ILogger
    {
        private readonly string _facility;
        private readonly Serilog.ILogger _logger;

        public DatabaseLogger() 
        {
            _facility = "database";

            var logBuild = new LoggerConfiguration()
            .WriteTo.File(Path.Combine("logs", $"{_facility}.log"), fileSizeLimitBytes: 31457280, retainedFileCountLimit: 10, restrictedToMinimumLevel: LogEventLevel.Verbose, flushToDiskInterval: TimeSpan.FromSeconds(30))
            .WriteTo.Console();

            if(!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("DATABASE_DEBUG_LOG")))
            {
                logBuild.MinimumLevel.Debug();
            }
            else
            {
                logBuild.MinimumLevel.Warning();
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
