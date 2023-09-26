using System.Diagnostics;
using Automatica.Core.Base.Common;
using Automatica.Core.Base.Logger;
using Automatica.Core.Push.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace Automatica.Core.Logging
{
    public class CoreLogger : Microsoft.Extensions.Logging.ILogger
    {
        private readonly IConfiguration _config;
        private readonly IServiceProvider? _serviceProvider;
        private readonly string _facility;
        private readonly string _logNameFacility;
        private readonly LogLevel _level;
        private readonly Serilog.ILogger _logger;

        public CoreLogger(IConfiguration config, IServiceProvider? serviceProvider) : this(config, serviceProvider, "core")
        {
        }

        public CoreLogger(IConfiguration config, IServiceProvider? serviceProvider, string facility) : this(config, serviceProvider, facility, null)
        {
            
        }

        public CoreLogger(IConfiguration config, IServiceProvider? serviceProvider, string facility, LogLevel? level, bool isFrameworkLog = false) {
            _serviceProvider = serviceProvider;
            _facility = facility;

            if (isFrameworkLog)
            {
                _level = LogLevel.Information;
            }
            else if (level.HasValue)
            {
                _level = level.Value;
            }
            else
            {
                if (config == null)
                {
                    _level = LogLevel.Information;
                }
                else
                {
                    _level = Parse(config["server:log_level"]);
                }
            }
            

            if(string.IsNullOrEmpty(facility))
            {
               facility = "dotnet"; 
            }
            facility = facility.ToLower();
            _logNameFacility = facility;

            var logBuild = new LoggerConfiguration();

            if (isFrameworkLog)
            {
                logBuild.WriteTo.RollingFile(Path.Combine(ServerInfo.GetLogDirectory(), $"framework-{facility}.log"),
                        fileSizeLimitBytes: 31457280,
                        retainedFileCountLimit: 2, restrictedToMinimumLevel: ConvertLogLevel(_level),
                        flushToDiskInterval: TimeSpan.FromSeconds(30))
                    .WriteTo.RollingFile(Path.Combine(ServerInfo.GetLogDirectory(), "all.log"),
                        fileSizeLimitBytes: 134217728, //128mb
                        retainedFileCountLimit: 10, restrictedToMinimumLevel: ConvertLogLevel(LogLevel.Warning),
                        shared: true,
                        flushToDiskInterval: TimeSpan.FromSeconds(30));
                //logBuild.WriteTo.PushSignalR(_serviceProvider, "all.log", ConvertLogLevel(LogLevel.Warning));
                //logBuild.WriteTo.PushSignalR(_serviceProvider, $"framework-{facility}.log", ConvertLogLevel(_level));
            }
            else
            {
                var fileName = Path.Combine(ServerInfo.GetLogDirectory(), $"{facility}.log");

                if (facility.Contains(LoggerConstants.FileSeparator))
                {
                    var split = facility.Split(LoggerConstants.FileSeparator).ToList();
                    split.Insert(0, ServerInfo.GetLogDirectory());
                    split[^1] += ".log";

                    fileName = Path.Combine(split.ToArray());

                    _logNameFacility = split[^1];
                }

                logBuild
                    .WriteTo.RollingFile(fileName,
                        fileSizeLimitBytes: 31457280, //~32mb
                        retainedFileCountLimit: 10, restrictedToMinimumLevel: ConvertLogLevel(_level),
                        flushToDiskInterval: TimeSpan.FromSeconds(30))
                    .WriteTo.RollingFile(Path.Combine(ServerInfo.GetLogDirectory(), "all.log"),
                        fileSizeLimitBytes: 134217728, //128mb
                        retainedFileCountLimit: 10, restrictedToMinimumLevel: ConvertLogLevel(LogLevel.Warning),
                        shared: true,
                        flushToDiskInterval: TimeSpan.FromSeconds(30));

                logBuild.WriteTo.PushSignalR(_serviceProvider, "all", ConvertLogLevel(LogLevel.Warning));
                logBuild.WriteTo.PushSignalR(_serviceProvider, facility, ConvertLogLevel(_level));
            }
            // enable log to stdout only in docker and if debugger is attached to prevent syslog from writing to much data
            if (Debugger.IsAttached || ServerInfo.InDocker)
            {
                logBuild.WriteTo.Console(theme: ConsoleTheme.None);
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
            var msg = $"{_logNameFacility.ToLower()}: {formatter.Invoke(state, exception)}";
            _logger.Write(level, exception, msg);
        }
    }
}