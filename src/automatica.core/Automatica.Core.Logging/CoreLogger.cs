using System.Diagnostics;
using Automatica.Core.Base.Common;
using Automatica.Core.Base.Logger;
using Automatica.Core.Push.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace Automatica.Core.Logging
{
    public class CoreLogger : ICoreLogger
    {
        private readonly string _logNameFacility;
        private readonly Serilog.ILogger _logger;
        private readonly LoggingLevelSwitch _levelSwitch;
        private LogLevel _logLevel;


        public LogLevel LogLevel
        {
            get => _logLevel;
            set
            {
                _logLevel = value;
                _levelSwitch.MinimumLevel = ConvertLogLevel(value);
            }
        }

        public string Facility { get; }

        public CoreLogger(IConfiguration? config, IServiceProvider? serviceProvider) : this(config, serviceProvider, "core")
        {
        }

        public CoreLogger(IConfiguration? config, IServiceProvider? serviceProvider, string facility) : this(config, serviceProvider, facility, null)
        {
            
        }

        public CoreLogger(IConfiguration? config, IServiceProvider? serviceProvider, string facility, LogLevel? level, bool isFrameworkLog = false) {
            
            Facility = facility; 
            _levelSwitch = new LoggingLevelSwitch();


            if (isFrameworkLog)
            {
                LogLevel = LogLevel.Warning;
            }
            else if (level.HasValue)
            {
                LogLevel = level.Value;
            }
            else
            {
                if (config == null)
                {
                    LogLevel = LogLevel.Information;
                }
                else
                {
                    LogLevel = Parse(config["server:log_level"]!);
                }
            }
            

            if(string.IsNullOrEmpty(facility))
            {
               facility = "dotnet"; 
            }
            facility = facility.ToLower();
            _logNameFacility = facility;

            var logBuild = new LoggerConfiguration();
            
            _levelSwitch.MinimumLevel = ConvertLogLevel(LogLevel);

            if (isFrameworkLog)
            {
                logBuild.WriteTo.File(Path.Combine(ServerInfo.GetLogDirectory(), $"framework-{facility}.log"),
                        fileSizeLimitBytes: 31457280,
                        rollingInterval: RollingInterval.Day,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: 2,
                        flushToDiskInterval: TimeSpan.FromSeconds(30))
                    .WriteTo.File(Path.Combine(ServerInfo.GetLogDirectory(), "all.log"),
                        fileSizeLimitBytes: 134217728, //128mb
                        rollingInterval: RollingInterval.Day,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: 10,
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
                    .WriteTo.File(fileName,
                        fileSizeLimitBytes: 31457280, //~32mb
                        rollingInterval: RollingInterval.Day,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: 20,
                        flushToDiskInterval: TimeSpan.FromSeconds(30))
                    .WriteTo.File(Path.Combine(ServerInfo.GetLogDirectory(), "all.log"),
                        fileSizeLimitBytes: 134217728 * 2, //256mb
                        rollingInterval: RollingInterval.Day,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: 10,
                        shared: true,
                        flushToDiskInterval: TimeSpan.FromSeconds(30));

                logBuild.WriteTo.PushSignalR(serviceProvider, "all");
                logBuild.WriteTo.PushSignalR(serviceProvider, facility);
            }
            // enable log to stdout only in docker and if debugger is attached to prevent syslog from writing to much data
            if (Debugger.IsAttached || ServerInfo.InDocker)
            {
                logBuild.WriteTo.Console(theme: ConsoleTheme.None);
            }
            logBuild.MinimumLevel.ControlledBy(_levelSwitch);

            _logger = logBuild.CreateLogger();
        }

        internal static LogLevel Parse(string logLevel)
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
            var s = state as IDisposable;
            return s!;
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
                    level = LogEventLevel.Fatal;
                    break;
            }
            return level;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception, string> formatter)
        {
            var level = ConvertLogLevel(logLevel);

            if (!_logger.IsEnabled(level))
            {
                return;
            }

            var msg = $"{_logNameFacility.ToLower()}: {formatter.Invoke(state, exception!)}";
            _logger.Write(level, exception, msg);
        }

    }
}