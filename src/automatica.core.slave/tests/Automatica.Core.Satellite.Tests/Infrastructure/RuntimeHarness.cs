using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Satellite.Runtime;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Satellite.Tests.Infrastructure
{
    /// <summary>
    /// A SatelliteRuntime with a fake docker client, connected to a real in-process broker
    /// </summary>
    internal sealed class RuntimeHarness : IAsyncDisposable
    {
        public const string SatelliteId = "sat-test-1";

        public TestBroker Broker { get; private set; }
        public FakeDocker Docker { get; } = new();
        public CapturingLoggerProvider Logs { get; } = new();
        public IConfigurationRoot Config { get; private set; }
        public SatelliteRuntime Runtime { get; private set; }

        public static async Task<RuntimeHarness> StartAsync(IDictionary<string, string> extraConfig = null, bool connect = true,
            TestBroker broker = null, Action<FakeDocker> configureDocker = null)
        {
            var harness = new RuntimeHarness();
            harness.Broker = broker ?? await TestBroker.StartAsync();
            configureDocker?.Invoke(harness.Docker);

            var config = new Dictionary<string, string>
            {
                { "server:master", "127.0.0.1" },
                { "server:masterPort", harness.Broker.Port.ToString() },
                { "server:clientId", SatelliteId },
                { "server:clientKey", TestKeys.SatelliteKey },
                { "server:dockerTag", "" },
                { "Logging:LogLevel:Default", "Warning" },
                // no host devices in the tests (on Linux "auto" would pass the runner's real /dev entries)
                { "docker:devices", "none" },
                { "docker:allowedDevices", "/dev/ttyUSB*" }
            };
            foreach (var kv in extraConfig ?? new Dictionary<string, string>())
            {
                config[kv.Key] = kv.Value;
            }

            harness.Config = new ConfigurationBuilder().AddInMemoryCollection(config).Build();
            harness.Runtime = new SatelliteRuntime(harness.Config, harness.Logs.CreateLogger<SatelliteRuntime>(), harness.Docker.Client.Object);

            if (connect)
            {
                await harness.Runtime.StartAsync(CancellationToken.None);
            }
            return harness;
        }

        public async ValueTask DisposeAsync()
        {
            if (Runtime != null)
            {
                await Runtime.StopAsync(CancellationToken.None);
            }
            if (Broker != null)
            {
                await Broker.DisposeAsync();
            }
        }
    }
}
