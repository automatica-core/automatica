using System;
using System.Diagnostics;
using System.IO;
using Automatica.Core.Base.Common;
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

        public CoreLogger() : this("core")
        {

        }

        public CoreLogger(string facility) : this(facility, null)
        {
            
        }

        public CoreLogger(string facility, LogLevel? level, bool isFrameworkLog = false) {
            _facility = facility;

            if (level.HasValue)
            {
                _level = level.Value;
            }
            else
            {
                if (CoreLoggerFactory.Configuration == null)
                {
                    _level = LogLevel.Information;
                }
                else
                {
                    _level = Parse(CoreLoggerFactory.Configuration["server:log_level"]);
                }
            }
            

            if(string.IsNullOrEmpty(facility))
            {
               facility = "dotnet"; 
            }
            facility = facility.ToLower();

            var logBuild = new LoggerConfiguration();

            if (isFrameworkLog)
            { 
                logBuild.WriteTo.RollingFile(Path.Combine(ServerInfo.GetLogDirectory(), $"framework-{facility}.log"), fileSizeLimitBytes: 31457280,
                    retainedFileCountLimit: 2, restrictedToMinimumLevel: ConvertLogLevel(_level),
                    flushToDiskInterval: TimeSpan.FromSeconds(30));
            }
            else
            {
                logBuild.WriteTo.RollingFile(Path.Combine(ServerInfo.GetLogDirectory(), $"{facility}.log"),
                    fileSizeLimitBytes: 31457280,
                    retainedFileCountLimit: 10, restrictedToMinimumLevel: ConvertLogLevel(_level),
                    flushToDiskInterval: TimeSpan.FromSeconds(30));
            }
            // enable log to stdout only in docker and if debugger is attached to prevent syslog from writing to much data
            if (Debugger.IsAttached || ServerInfo.InDocker)
            {
                logBuild.WriteTo.Console();
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

        private LogLevel Parse(string logLevel)
        {
			if(String.IsNullOrEmpty(logLevel)) {
				return LogLevel.Debug;
			}
            switch (logLevel.ToLowerInvariant())
            {
                case "trace":
                    return LogLevel.Trace;
                case "debug":
                    return LogLevel.Debug;
                case "warning":
                    return LogLevel.Warning;
                case "error":
                    return LogLevel.Error;
                case "critical":
                    return LogLevel.Critical;
                case "none":
                    return LogLevel.None;

                default:
                    return LogLevel.Error;
            }
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