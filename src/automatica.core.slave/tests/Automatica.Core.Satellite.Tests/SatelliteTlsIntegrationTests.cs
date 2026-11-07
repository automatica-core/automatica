using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Automatica.Core.Satellite.Abstraction;
using Automatica.Core.Satellite.Tests.Infrastructure;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Automatica.Core.Satellite.Tests
{
    /// <summary>
    /// The satellite connects to the core's TLS broker listener with a pinned certificate fingerprint
    /// </summary>
    public sealed class SatelliteTlsIntegrationTests : IAsyncLifetime
    {
        private X509Certificate2 _cert;
        private TestBroker _tlsBroker;
        private string _fingerprint;

        public async Task InitializeAsync()
        {
            _cert = TestCertificates.SharedServerCertificate();
            _fingerprint = TestCertificates.Fingerprint(_cert);
            _tlsBroker = await TestBroker.StartAsync(_cert);
        }

        public async Task DisposeAsync()
        {
            await _tlsBroker.DisposeAsync();
            _cert.Dispose();
        }

        private Task<RuntimeHarness> StartTls(string fingerprint) =>
            RuntimeHarness.StartAsync(new Dictionary<string, string>
            {
                { "server:useTls", "true" },
                { "server:tlsPort", _tlsBroker.Port.ToString() },
                { "server:tlsFingerprint", fingerprint }
            }, broker: _tlsBroker);

        private static ActionRequest Start(int tlsPort = 0, string fingerprint = null) => new()
        {
            Action = SlaveAction.Start,
            ImageName = "automaticacore/automatica-plugin-knx",
            Tag = "1.0",
            Id = Guid.NewGuid(),
            ClientKey = TestKeys.PluginKey,
            BrokerTlsPort = tlsPort,
            BrokerFingerprint = fingerprint
        };

        [Fact]
        public async Task PinnedFingerprint_Matches_Connects()
        {
            var h = await StartTls(_fingerprint);
            try
            {
                Assert.True(h.Runtime.Connected);
                var connect = Assert.Single(_tlsBroker.Connects);
                Assert.Equal(RuntimeHarness.SatelliteId, connect.UserName);
                Assert.Equal(TestKeys.SatelliteKey, connect.Password);
            }
            finally
            {
                await h.Runtime.StopAsync(default);
            }
        }

        [Fact]
        public async Task PinnedFingerprint_InColonFormat_Connects()
        {
            var colon = String.Join(":", Enumerable.Range(0, 32).Select(i => _fingerprint.Substring(i * 2, 2).ToUpperInvariant()));
            var h = await StartTls(colon);
            try
            {
                Assert.True(h.Runtime.Connected);
            }
            finally
            {
                await h.Runtime.StopAsync(default);
            }
        }

        [Fact]
        public async Task PinnedFingerprint_Mismatch_DoesNotConnectOrSendTheKey()
        {
            var h = await StartTls(new string('0', 64));
            try
            {
                Assert.False(h.Runtime.Connected);
                // the handshake fails before CONNECT, so the broker never sees the key
                Assert.Empty(_tlsBroker.Connects);
                Assert.True(h.Logs.Contains(LogLevel.Error, "does not match the pinned fingerprint"));
            }
            finally
            {
                await h.Runtime.StopAsync(default);
            }
        }

        [Fact]
        public async Task NoFingerprint_PinsTheFirstCertificate()
        {
            var h = await StartTls(null);
            try
            {
                Assert.True(h.Runtime.Connected);
                Assert.Equal(_fingerprint, h.Config["server:tlsFingerprint"]);
                Assert.True(h.Logs.Contains(LogLevel.Warning, "No broker certificate fingerprint configured"));
            }
            finally
            {
                await h.Runtime.StopAsync(default);
            }
        }

        [Fact]
        public async Task Tls_PluginContainerGetsTheTlsPortAndFingerprint()
        {
            var h = await StartTls(_fingerprint);
            try
            {
                await h.Runtime.ExecuteAction(Start());

                var env = Assert.Single(h.Docker.Created).Env;
                Assert.Contains($"AUTOMATICA_SLAVE_TLS_PORT={_tlsBroker.Port}", env);
                Assert.Contains($"AUTOMATICA_SLAVE_TLS_FINGERPRINT={_fingerprint}", env);
            }
            finally
            {
                await h.Runtime.StopAsync(default);
            }
        }

        [Fact]
        public async Task CoreOffersTls_PlainSatelliteSwitchesToThePinnedTlsListener()
        {
            await using var h = await RuntimeHarness.StartAsync();
            Assert.True(h.Runtime.Connected);

            await h.Runtime.ExecuteAction(Start(_tlsBroker.Port, _fingerprint));

            Assert.Empty(h.Docker.Created);
            Assert.Equal("true", h.Config["server:useTls"]);
            Assert.Equal(_tlsBroker.Port.ToString(), h.Config["server:tlsPort"]);
            Assert.Equal(_fingerprint, h.Config["server:tlsFingerprint"]);
            await Eventually.Assert(() => _tlsBroker.Connects.Any(c => c.ClientId == RuntimeHarness.SatelliteId), "satellite did not connect to the TLS listener");
            await Eventually.Assert(() => h.Runtime.Connected, "satellite is not connected after the switch");
        }

        [Fact]
        public async Task CoreOffersTls_ExplicitlyDisabled_StaysPlainAndStartsTheContainer()
        {
            await using var h = await RuntimeHarness.StartAsync(new Dictionary<string, string> { { "server:useTls", "false" } });

            await h.Runtime.ExecuteAction(Start(_tlsBroker.Port, _fingerprint));

            Assert.Single(h.Docker.Created);
            Assert.Equal("false", h.Config["server:useTls"]);
            Assert.Null(h.Config["server:tlsFingerprint"]);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("not-a-fingerprint")]
        public async Task CoreOffersTls_WithoutValidFingerprint_IsNotUpgraded(string fingerprint)
        {
            await using var h = await RuntimeHarness.StartAsync();

            await h.Runtime.ExecuteAction(Start(_tlsBroker.Port, fingerprint));

            Assert.Single(h.Docker.Created);
            Assert.Null(h.Config["server:useTls"]);
            Assert.Empty(_tlsBroker.Connects);
        }
    }
}
