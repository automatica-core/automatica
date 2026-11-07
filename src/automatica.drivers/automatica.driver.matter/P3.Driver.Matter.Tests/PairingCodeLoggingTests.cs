using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using P3.Driver.Matter.Commissioning;
using P3.Driver.Matter.Storage;

namespace P3.Driver.Matter.Tests
{
    /// <summary>The setup code commissions the bridge into any fabric: it may only appear in debug logs.</summary>
    public sealed class PairingCodeLoggingTests : IAsyncLifetime
    {
        private readonly CapturingLogger _logger = new();
        private MatterBridge _bridge;

        public async Task InitializeAsync()
        {
            _bridge = new MatterBridge(new MatterNodeOptions { Port = 0, EnableMdns = false }, new InMemoryMatterStorage(), _logger);
            await _bridge.InitializeAsync();
            _bridge.Add(new BridgedOnOffDevice("light-1", "Kitchen"));
            await _bridge.StartAsync();
        }

        public async Task DisposeAsync() => await _bridge.DisposeAsync();

        private string[] Secrets()
        {
            var node = _bridge.Node;
            return new[]
            {
                node.ManualPairingCode,
                SetupPayload.FormatManualCode(node.ManualPairingCode),
                node.QrCodePayload,
                node.State.Passcode.ToString("D8"),
                node.State.Passcode.ToString()
            };
        }

        private void AssertNoSecretAtInfoOrAbove()
        {
            var secrets = Secrets();
            foreach (var entry in _logger.Entries.Where(e => e.Level >= LogLevel.Information))
            {
                foreach (var secret in secrets)
                {
                    Assert.False(entry.Message.Contains(secret, StringComparison.Ordinal),
                        $"{entry.Level} log leaks the setup code: {entry.Message}");
                }
            }
        }

        [Fact]
        public void Start_LogsListeningAtInformation_WithoutThePairingCode()
        {
            Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("listening on UDP", StringComparison.Ordinal));
            AssertNoSecretAtInfoOrAbove();
        }

        [Fact]
        public void Start_LogsThePairingCodeOnlyAtDebug()
        {
            var formatted = SetupPayload.FormatManualCode(_bridge.Node.ManualPairingCode);
            var withCode = _logger.Entries.Where(e => e.Message.Contains(formatted, StringComparison.Ordinal)).ToList();

            Assert.NotEmpty(withCode);
            Assert.All(withCode, e => Assert.True(e.Level <= LogLevel.Debug, $"{e.Level}: {e.Message}"));
        }

        [Fact]
        public async Task FailedPase_WarningDoesNotContainTheSetupCode()
        {
            using var controller = new MiniController(_bridge.Node.Port);
            var wrong = _bridge.Node.State.Passcode == 20202021 ? 11223344u : 20202021u;

            var status = await controller.FailPaseAsync(wrong);

            Assert.False(status.IsSuccess);
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (!_logger.Entries.Any(e => e.Level == LogLevel.Warning) && DateTime.UtcNow < deadline)
            {
                await Task.Delay(20);
            }

            Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("wrong setup code", StringComparison.Ordinal));
            Assert.DoesNotContain(_logger.Entries, e => e.Level >= LogLevel.Information && e.Message.Contains(wrong.ToString(), StringComparison.Ordinal));
            AssertNoSecretAtInfoOrAbove();
        }

        [Fact]
        public void OpenCommissioningWindow_DoesNotLogTheCodeAtInformation()
        {
            _bridge.Node.OpenCommissioningWindow(TimeSpan.FromMinutes(3));
            AssertNoSecretAtInfoOrAbove();
        }

        internal sealed class CapturingLogger : ILogger
        {
            public ConcurrentQueue<(LogLevel Level, string Message)> Queue { get; } = new();

            public IReadOnlyList<(LogLevel Level, string Message)> Entries => Queue.ToArray();

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                Queue.Enqueue((logLevel, formatter(state, exception) + (exception == null ? "" : " " + exception)));
            }
        }
    }
}
