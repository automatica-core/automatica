using System.ComponentModel;
using Automatica.Core.Base.Common;
using Automatica.Core.Logging;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Automatica.Core.Mcp.Tools
{
    /// <summary>
    /// The server log files and log levels. Logs can contain addresses, device credentials and tokens, so the API key
    /// needs the setting <c>mcpAllowLogs</c> and JWT callers the admin role (like <c>webapi/logging</c>). Changing a
    /// log level needs write access on top.
    /// </summary>
    [McpServerToolType]
    public class LogTools
    {
        private readonly McpAccess _access;
        private readonly ILoggerFactory _loggerFactory;

        public LogTools(McpAccess access, ILoggerFactory loggerFactory)
        {
            _access = access;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// The directory the log files are read from, the server's log directory unless a test sets another one.
        /// </summary>
        public string LogDirectory { get; set; } = ServerInfo.GetLogDirectory();

        private void EnsureCanRead()
        {
            if (_access.CanReadLogs)
            {
                return;
            }

            throw new McpException(_access.IsApiKeyCaller
                ? "Log access is disabled for the MCP API key. Enable the setting 'mcpAllowLogs' or log in as an administrator."
                : "Logs are available to administrators only");
        }

        [McpServerTool(Name = "list_log_files", Title = "List log files", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Lists the newest server log files with size and last change, newest first, plus how many files there are in total. 'all*.log' holds the log of every facility, the other files one driver or part of the server each; older days carry the date in the name. Servers keep thousands of old files: narrow down with 'query' instead of raising the limit.")]
        public LogFileList ListLogFiles(
            [Description("Only files whose path contains this text (case insensitive), e.g. a driver name or a date like 20261108.")] string query = null,
            [Description("How many of the newest files to return, 1 to 500.")] int limit = DefaultFileLimit)
        {
            EnsureCanRead();
            var files = LogFileReader.List(LogDirectory, query);
            var returned = files.Take(Math.Clamp(limit, 1, MaxFileLimit)).ToList();
            return new LogFileList(files.Count, files.Sum(a => a.SizeBytes), returned.Count, returned);
        }

        public const int DefaultFileLimit = 50;
        public const int MaxFileLimit = 500;

        [McpServerTool(Name = "read_log", Title = "Read a log file", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Returns the last lines of a log file (oldest first). With 'contains' only the matching lines are returned, searched backwards over the last 64 MB of the file.")]
        public LogTail ReadLog(
            [Description("The file path as list_log_files returns it.")] string file,
            [Description("How many lines to return, 1 to 2000.")] int lines = LogFileReader.DefaultLines,
            [Description("Only lines containing this text (case insensitive), e.g. 'ERR', an exception name or a device name.")] string contains = null)
        {
            EnsureCanRead();
            try
            {
                return LogFileReader.Tail(LogDirectory, file, lines, contains);
            }
            catch (FileNotFoundException e)
            {
                throw new McpException(e.Message);
            }
        }

        [McpServerTool(Name = "get_log_levels", Title = "Get log levels", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Lists the logging facilities (drivers and parts of the server) with their current log level.")]
        public IList<LogLevelInfo> GetLogLevels()
        {
            EnsureCanRead();
            return CoreLoggers()
                .Select(a => new LogLevelInfo(a.Facility, a.LogLevel.ToString()))
                .OrderBy(a => a.Facility, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        [McpServerTool(Name = "set_log_level", Title = "Change a log level", Destructive = false, Idempotent = true, OpenWorld = false)]
        [Description("Changes the log level of one facility, or of all facilities when none is given. Levels: Trace, Debug, Information, Warning, Error, Critical, None. Debug and Trace log a lot; set the level back when done.")]
        public IList<LogLevelInfo> SetLogLevel(
            [Description("The new level: Trace, Debug, Information, Warning, Error, Critical or None.")] string level,
            [Description("The facility as get_log_levels returns it; empty for all facilities.")] string facility = null)
        {
            EnsureCanRead();
            if (!_access.CanWrite)
            {
                throw new McpException("Writing is disabled for the MCP API key. Enable the setting 'mcpAllowWrite' or log in with a user token.");
            }

            if (!Enum.TryParse<LogLevel>(level, true, out var logLevel) || !Enum.IsDefined(logLevel))
            {
                throw new McpException($"'{level}' is not a log level. Use Trace, Debug, Information, Warning, Error, Critical or None.");
            }

            if (_loggerFactory is not ICoreLoggerFactory factory)
            {
                throw new McpException("This server does not support changing log levels");
            }

            if (string.IsNullOrWhiteSpace(facility))
            {
                factory.SetLogLevel(logLevel);
            }
            else
            {
                var logger = factory.GetLoggers().FirstOrDefault(a => string.Equals(a.Facility, facility, StringComparison.OrdinalIgnoreCase))
                             ?? throw new McpException($"Unknown facility '{facility}'. Use get_log_levels for the available facilities.");
                factory.SetLogLevel(logger.Facility, logLevel);
            }

            return GetLogLevels();
        }

        private IEnumerable<ICoreLogger> CoreLoggers()
        {
            return _loggerFactory is ICoreLoggerFactory factory ? factory.GetLoggers() : Enumerable.Empty<ICoreLogger>();
        }
    }

    public record LogLevelInfo(string Facility, string Level);

    /// <param name="TotalCount">All matching files, also the ones not returned.</param>
    /// <param name="TotalSizeBytes">Size of all matching files.</param>
    /// <param name="Returned">How many files are in <paramref name="Files"/>.</param>
    public record LogFileList(int TotalCount, long TotalSizeBytes, int Returned, IList<LogFileInfo> Files);
}
