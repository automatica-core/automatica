using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Logging;
using Automatica.Core.Mcp;
using Automatica.Core.Mcp.Authentication;
using Automatica.Core.Mcp.Tools;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Xunit;

namespace Automatica.Core.Tests.Mcp
{
    public sealed class LogToolsTests : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "automatica-mcp-logs-" + Guid.NewGuid().ToString("N"));
        private readonly FakeSettingsCache _settings = new();
        private readonly FakeLoggerFactory _loggers = new("knx", "Sonos", "framework-core");

        public LogToolsTests()
        {
            Directory.CreateDirectory(_directory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_directory, true);
            }
            catch (IOException)
            {
                // a file still open by a failed test, the temp folder is cleaned up eventually
            }
        }

        private string WriteLog(string name, string content)
        {
            var path = Path.Combine(_directory, name);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content, new UTF8Encoding(false));
            return path;
        }

        private static string Lines(int count, string prefix = "line", string newline = "\n")
        {
            return string.Concat(Enumerable.Range(1, count).Select(i => $"{prefix} {i}{newline}"));
        }

        private LogTools Tools(ClaimsPrincipal user)
        {
            var access = new McpAccess(new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = user } }, _settings);
            return new LogTools(access, _loggers) { LogDirectory = _directory };
        }

        private static ClaimsPrincipal ApiKey()
        {
            return new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, McpApiKeyAuthenticationHandler.ApiKeyUserName),
                new Claim(ClaimTypes.Role, Role.ViewerRole)
            }, McpApiKeyAuthenticationHandler.SchemeName));
        }

        private static ClaimsPrincipal User(string role)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "patrik"), new Claim(ClaimTypes.Role, role) }, "Bearer"));
        }

        private void AllowLogs(bool logs, bool write = false)
        {
            _settings.Set(McpSettings.AllowLogsKey, logs);
            _settings.Set(McpSettings.AllowWriteKey, write);
        }

        // ---------- reader ----------

        [Fact]
        public void Tail_ReturnsTheLastLinesOldestFirst()
        {
            WriteLog("all20261109.log", Lines(10));

            var tail = LogFileReader.Tail(_directory, "all20261109.log", 3);

            Assert.Equal(new[] { "line 8", "line 9", "line 10" }, tail.Lines);
            Assert.Equal(3, tail.LineCount);
            Assert.False(tail.ReachedStartOfFile);
        }

        [Fact]
        public void Tail_FewerLinesThanAsked_ReturnsTheWholeFileIncludingTheFirstLine()
        {
            WriteLog("knx.log", "first\nsecond\nlast without newline");

            var tail = LogFileReader.Tail(_directory, "knx.log", 50);

            Assert.Equal(new[] { "first", "second", "last without newline" }, tail.Lines);
            Assert.True(tail.ReachedStartOfFile);
        }

        [Fact]
        public void Tail_LinesAcrossChunkBoundaries_StayWhole()
        {
            // ~490 KB, so the backwards reader needs several 64 KB chunks and lines get split between them
            WriteLog("all.log", Lines(20000, "2026-11-09 10:00:00.000 [INF] knx: telegram received from 1.1.1"));

            var tail = LogFileReader.Tail(_directory, "all.log", 2000);

            Assert.Equal(2000, tail.Lines.Count);
            Assert.Equal("2026-11-09 10:00:00.000 [INF] knx: telegram received from 1.1.1 18001", tail.Lines[0]);
            Assert.Equal("2026-11-09 10:00:00.000 [INF] knx: telegram received from 1.1.1 20000", tail.Lines[^1]);
            Assert.All(tail.Lines, a => Assert.StartsWith("2026-11-09 10:00:00.000 [INF] knx: telegram received from 1.1.1 ", a));
        }

        [Fact]
        public void Tail_Contains_ReturnsOnlyMatchingLinesCaseInsensitive()
        {
            WriteLog("all.log", "a [INF] ok\nb [ERR] knx failed\nc [INF] ok\nd [err] sonos failed\ne [INF] ok\n");

            var tail = LogFileReader.Tail(_directory, "all.log", 10, "ERR");

            Assert.Equal(new[] { "b [ERR] knx failed", "d [err] sonos failed" }, tail.Lines);
        }

        [Fact]
        public void Tail_WindowsLineEndingsAndUmlauts_AreReadCleanly()
        {
            WriteLog("all.log", Lines(3, "Küche Temperatur", "\r\n"));

            var tail = LogFileReader.Tail(_directory, "all.log", 2);

            Assert.Equal(new[] { "Küche Temperatur 2", "Küche Temperatur 3" }, tail.Lines);
        }

        [Fact]
        public void Tail_LineCountIsClamped()
        {
            WriteLog("all.log", Lines(3000));

            Assert.Single(LogFileReader.Tail(_directory, "all.log", 0).Lines);
            Assert.Equal(LogFileReader.MaxLines, LogFileReader.Tail(_directory, "all.log", 100000).Lines.Count);
        }

        [Fact]
        public void Tail_VeryLongLine_IsCut()
        {
            WriteLog("all.log", new string('x', LogFileReader.MaxLineLength + 500) + "\n");

            var line = Assert.Single(LogFileReader.Tail(_directory, "all.log", 5).Lines);

            Assert.Equal(LogFileReader.MaxLineLength + 2, line.Length);
        }

        [Fact]
        public void Tail_FileStillOpenForWriting_CanBeRead()
        {
            var path = WriteLog("all.log", "");
            using var writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete));
            writer.Write(Lines(5));
            writer.Flush();

            Assert.Equal("line 5", LogFileReader.Tail(_directory, "all.log", 1).Lines.Single());
        }

        [Theory]
        [InlineData("../secret.txt")]
        [InlineData("..\\secret.txt")]
        [InlineData("sub/../../secret.txt")]
        [InlineData("missing.log")]
        [InlineData("")]
        public void Tail_OutsideTheLogDirectoryOrMissing_IsRefused(string file)
        {
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(_directory)!, "secret.txt"), "password=1234");

            Assert.Throws<FileNotFoundException>(() => LogFileReader.Tail(_directory, file));
        }

        [Fact]
        public void Tail_AbsolutePathOutsideTheLogDirectory_IsRefused()
        {
            var outside = Path.Combine(Path.GetTempPath(), "automatica-outside-" + Guid.NewGuid().ToString("N") + ".log");
            File.WriteAllText(outside, "secret");
            try
            {
                Assert.Throws<FileNotFoundException>(() => LogFileReader.Tail(_directory, outside));
            }
            finally
            {
                File.Delete(outside);
            }
        }

        [Fact]
        public void List_ReturnsRelativePathsWithSubdirectoriesNewestFirst()
        {
            File.SetLastWriteTime(WriteLog("all20261108.log", "old\n"), new DateTime(2026, 11, 8, 23, 0, 0));
            File.SetLastWriteTime(WriteLog("all20261109.log", "new\n"), new DateTime(2026, 11, 9, 10, 0, 0));
            File.SetLastWriteTime(WriteLog("drivers/knx.log", "knx\n"), new DateTime(2026, 11, 9, 9, 0, 0));

            var files = LogFileReader.List(_directory);

            Assert.Equal(new[] { "all20261109.log", "drivers/knx.log", "all20261108.log" }, files.Select(a => a.File));
            Assert.Equal(4, files[0].SizeBytes);
            Assert.Equal(new[] { "drivers/knx.log" }, LogFileReader.List(_directory, "KNX").Select(a => a.File));
            Assert.Equal("knx", LogFileReader.Tail(_directory, "drivers/knx.log").Lines.Single());
        }

        [Fact]
        public void List_MissingDirectory_IsEmpty()
        {
            Assert.Empty(LogFileReader.List(Path.Combine(_directory, "nope")));
        }

        // ---------- access ----------

        [Fact]
        public void ApiKey_WithoutMcpAllowLogs_IsRefused()
        {
            WriteLog("all.log", "x\n");
            AllowLogs(false, write: true);
            var tools = Tools(ApiKey());

            var error = Assert.Throws<McpException>(() => tools.ListLogFiles());
            Assert.Contains("mcpAllowLogs", error.Message);
            Assert.Throws<McpException>(() => tools.ReadLog("all.log"));
            Assert.Throws<McpException>(() => tools.GetLogLevels());
            Assert.Throws<McpException>(() => tools.SetLogLevel("Debug"));
        }

        [Fact]
        public void ApiKey_WithMcpAllowLogs_ReadsLogs()
        {
            WriteLog("all.log", "hello\n");
            AllowLogs(true);
            var tools = Tools(ApiKey());

            Assert.Equal("all.log", tools.ListLogFiles().Single().File);
            Assert.Equal("hello", tools.ReadLog("all.log").Lines.Single());
            Assert.Equal(3, tools.GetLogLevels().Count);
        }

        [Fact]
        public void ApiKey_WithLogsButWithoutWrite_CannotChangeLogLevels()
        {
            AllowLogs(true, write: false);

            var error = Assert.Throws<McpException>(() => Tools(ApiKey()).SetLogLevel("Debug", "knx"));

            Assert.Contains("mcpAllowWrite", error.Message);
            Assert.All(_loggers.Loggers, a => Assert.Equal(LogLevel.Information, a.LogLevel));
        }

        [Fact]
        public void Admin_ReadsLogsWithoutTheApiKeySetting()
        {
            WriteLog("all.log", "hello\n");
            AllowLogs(false);

            Assert.Equal("hello", Tools(User(Role.AdminRole)).ReadLog("all.log").Lines.Single());
        }

        [Theory]
        [InlineData(Role.ViewerRole)]
        [InlineData(Role.VisuRole)]
        public void NonAdminUser_IsRefusedEvenWhenTheApiKeyMayReadLogs(string role)
        {
            AllowLogs(true, write: true);

            var error = Assert.Throws<McpException>(() => Tools(User(role)).ListLogFiles());

            Assert.Contains("administrators", error.Message);
        }

        [Fact]
        public void ReadLog_UnknownFile_IsAnMcpError()
        {
            AllowLogs(true);

            var error = Assert.Throws<McpException>(() => Tools(ApiKey()).ReadLog("../../etc/passwd"));

            Assert.Contains("list_log_files", error.Message);
        }

        // ---------- log levels ----------

        [Fact]
        public void SetLogLevel_OneFacility_MatchesCaseInsensitive()
        {
            AllowLogs(true, write: true);

            var levels = Tools(ApiKey()).SetLogLevel("debug", "SONOS");

            Assert.Equal(LogLevel.Debug, _loggers.Get("Sonos").LogLevel);
            Assert.Equal(LogLevel.Information, _loggers.Get("knx").LogLevel);
            Assert.Equal("Debug", levels.Single(a => a.Facility == "Sonos").Level);
        }

        [Fact]
        public void SetLogLevel_WithoutFacility_ChangesAll()
        {
            Tools(User(Role.AdminRole)).SetLogLevel("Warning");

            Assert.All(_loggers.Loggers, a => Assert.Equal(LogLevel.Warning, a.LogLevel));
        }

        [Theory]
        [InlineData("Verbose")]
        [InlineData("42")]
        [InlineData("")]
        public void SetLogLevel_UnknownLevel_IsRefused(string level)
        {
            var error = Assert.Throws<McpException>(() => Tools(User(Role.AdminRole)).SetLogLevel(level));

            Assert.Contains("not a log level", error.Message);
        }

        [Fact]
        public void SetLogLevel_UnknownFacility_IsRefused()
        {
            var error = Assert.Throws<McpException>(() => Tools(User(Role.AdminRole)).SetLogLevel("Debug", "nope"));

            Assert.Contains("get_log_levels", error.Message);
        }

        [Fact]
        public void GetLogLevels_AreSortedByFacility()
        {
            var levels = Tools(User(Role.AdminRole)).GetLogLevels();

            Assert.Equal(new[] { "framework-core", "knx", "Sonos" }, levels.Select(a => a.Facility));
            Assert.All(levels, a => Assert.Equal("Information", a.Level));
        }

        [Fact]
        public void Tools_RegisterWithTheMcpServerLikeOnTheHost()
        {
            var services = new ServiceCollection();
            services.AddMcpServer().WithTools<LogTools>();
            using var provider = services.BuildServiceProvider();

            var tools = provider.GetServices<McpServerTool>().ToDictionary(a => a.ProtocolTool.Name);

            Assert.Equal(new[] { "get_log_levels", "list_log_files", "read_log", "set_log_level" }, tools.Keys.OrderBy(a => a));
            Assert.True(tools["read_log"].ProtocolTool.Annotations?.ReadOnlyHint);
            var schema = tools["read_log"].ProtocolTool.InputSchema.ToString();
            Assert.Contains("\"file\"", schema);
            Assert.Contains("\"contains\"", schema);
        }

        // ---------- fakes ----------

        private sealed class FakeSettingsCache : ISettingsCache
        {
            private readonly Dictionary<string, Setting> _settings = new();

            public void Set(string key, bool value)
            {
                _settings[key] = new Setting { ValueKey = key, Type = (long)PropertyTemplateType.Bool, Value = value };
            }

            public Setting GetByKey(string key) => _settings.GetValueOrDefault(key);
            public void UpdateByKey(string key, Setting value) => _settings[key] = value;
            public void Add(long key, Setting value) => throw new NotSupportedException();
            public void Update(long key, Setting value) => throw new NotSupportedException();
            public void Remove(long key) => throw new NotSupportedException();
            public bool Contains(long key) => false;
            public Setting Get(long key) => null;
            public ICollection<Setting> Get(params long[] keys) => new List<Setting>();
            public ICollection<Setting> All() => _settings.Values.ToList();
            public void Clear() { }
            public void ClearAndLoad() { }
            public IDictionary<long, Setting> Dictionary() => new Dictionary<long, Setting>();
        }

        private sealed class FakeLogger : ICoreLogger
        {
            public FakeLogger(string facility) => Facility = facility;

            public LogLevel LogLevel { get; set; } = LogLevel.Information;
            public string Facility { get; }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel;
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
        }

        private sealed class FakeLoggerFactory : ICoreLoggerFactory
        {
            private readonly Dictionary<string, FakeLogger> _loggers;

            public FakeLoggerFactory(params string[] facilities)
            {
                _loggers = facilities.ToDictionary(a => a, a => new FakeLogger(a));
            }

            public IEnumerable<FakeLogger> Loggers => _loggers.Values;
            public FakeLogger Get(string facility) => _loggers[facility];

            public IEnumerable<ICoreLogger> GetLoggers() => _loggers.Values;

            public void SetLogLevel(string name, LogLevel level)
            {
                // like CoreLoggerFactory: the exact facility name
                if (_loggers.TryGetValue(name, out var logger))
                {
                    logger.LogLevel = level;
                }
            }

            public void SetLogLevel(LogLevel level)
            {
                foreach (var logger in _loggers.Values)
                {
                    logger.LogLevel = level;
                }
            }

            public ILogger CreateLogger(string categoryName) => _loggers.GetValueOrDefault(categoryName) ?? new FakeLogger(categoryName);
            public void AddProvider(ILoggerProvider provider) { }
            public void Dispose() { }
        }
    }
}
