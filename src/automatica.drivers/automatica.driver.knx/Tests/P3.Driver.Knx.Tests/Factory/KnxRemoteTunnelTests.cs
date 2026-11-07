using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.License;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Retry;
using Automatica.Core.Base.Templates;
using Automatica.Core.Base.Tunneling;
using Automatica.Core.Control;
using Automatica.Core.Driver;
using Automatica.Core.Driver.Discovery;
using Automatica.Core.Driver.LeanMode;
using Automatica.Core.Driver.Monitor;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using Xunit;

namespace P3.Driver.Knx.Tests.Factory
{
    /// <summary>
    /// The remote connection tunnel of the KNX gateway has to forward to the KNX interface the driver connects to
    /// (regression: it was created with an empty address and port 0).
    /// </summary>
    public class KnxRemoteTunnelTests : DriverFactoryTestBase<KnxIpDriverFactory>
    {
        private const string RemoteFeature = "knx-interface-remote-connection";

        internal sealed class RecordingTunnelingProvider : ITunnelingProvider
        {
            public bool Available { get; set; } = true;
            public Exception Throw { get; set; }
            public List<(TunnelingProtocol Protocol, string Name, string Address, int Port)> Tunnels { get; } = new();

            public Task<bool> IsAvailableAsync(CancellationToken token) => Task.FromResult(Available);

            public Task<string> CreateTunnelAsync(TunnelingProtocol protocol, string name, string address, int targetPort, CancellationToken token)
            {
                if (Throw != null)
                {
                    throw Throw;
                }
                Tunnels.Add((protocol, name, address, targetPort));
                return Task.FromResult($"tunnel-{Tunnels.Count}");
            }

            public Task<string> CreateWebTunnelAsync(TunnelingProtocol protocol, string name, string subDomain, string localIp, int localPort, string basicUser,
                string basicPassword, CancellationToken token) => throw new NotSupportedException();
        }

        internal sealed class LicenseContract(params string[] features) : ILicenseContract
        {
            public bool IsFeatureLicensed(string featureName) => Array.IndexOf(features, featureName) >= 0;
            public bool DriverLicenseCountExceeded() => false;
            public void IncrementDriverCount() { }
        }

        /// <summary>
        /// <see cref="DriverContextMock"/> with a recording tunneling provider and a configurable license.
        /// </summary>
        internal sealed class TunnelDriverContext(IDriverContext inner, ITunnelingProvider tunnelingProvider, ILicenseContract licenseContract) : IDriverContext
        {
            public NodeInstance NodeInstance => inner.NodeInstance;
            public IDriverFactory Factory => inner.Factory;
            public IDispatcher Dispatcher => inner.Dispatcher;
            public INodeTemplateFactory NodeTemplateFactory => inner.NodeTemplateFactory;
            public ITelegramMonitor TelegramMonitor => inner.TelegramMonitor;
            public ILicenseState LicenseState => inner.LicenseState;
            public bool IsTest => true;
            public ILogger Logger => inner.Logger;
            public ILearnMode LearnMode => inner.LearnMode;
            public IServerCloudApi CloudApi => inner.CloudApi;
            public ILicenseContract LicenseContract => licenseContract;
            public ILoggerFactory LoggerFactory => inner.LoggerFactory;
            public ITunnelingProvider TunnelingProvider => tunnelingProvider;
            public IZeroconfDiscovery ZeroconfDiscovery => inner.ZeroconfDiscovery;
            public IControlContext ControlContext => inner.ControlContext;
            public TimeProvider TimeProvider => inner.TimeProvider;
            public ILocalizationProvider LocalizationProvider => inner.LocalizationProvider;
            public IRetryContext RetryContext => inner.RetryContext;

            public Task CreateNotification(string subject, string body, NotificationSeverity severity, CancellationToken token = default) => Task.CompletedTask;

            public IDriverContext Copy(NodeInstance node, ILogger logger) => new TunnelDriverContext(inner.Copy(node, logger), tunnelingProvider, licenseContract);
        }

        private readonly RecordingTunnelingProvider _tunnels = new();

        private KnxDriver CreateDriver(string ip, int? port, bool useTunnel, ILicenseContract license = null)
        {
            var gateway = CreateNodeInstance(KnxIpDriverFactory.KnxGateway);
            gateway.SetProperty("knx-ip", ip);
            if (port.HasValue)
            {
                gateway.SetProperty("knx-port", port.Value);
            }
            gateway.SetProperty("knx-use-tunnel", useTunnel);

            var inner = new DriverContextMock(gateway, DriverFactory, Factory, Dispatcher, NullLoggerFactory.Instance, TimeProvider.System);
            var context = new TunnelDriverContext(inner, _tunnels, license ?? new LicenseContract(RemoteFeature));
            return new KnxDriver(context, false);
        }

        [Fact]
        public async Task Init_RemoteTunnelEnabled_ForwardsToTheGatewayAddressAndPort()
        {
            var driver = CreateDriver("192.0.2.10", 3700, true);

            Assert.True(await driver.Init());

            var tunnel = Assert.Single(_tunnels.Tunnels);
            Assert.Equal(TunnelingProtocol.Udp, tunnel.Protocol);
            Assert.Equal("knx", tunnel.Name);
            Assert.Equal("192.0.2.10", tunnel.Address);
            Assert.Equal(3700, tunnel.Port);
        }

        [Fact]
        public async Task Init_RemoteTunnelWithTheDefaultPort_Uses3671()
        {
            var driver = CreateDriver("192.0.2.11", null, true);

            Assert.True(await driver.Init());

            var tunnel = Assert.Single(_tunnels.Tunnels);
            Assert.Equal("192.0.2.11", tunnel.Address);
            Assert.Equal(3671, tunnel.Port);
        }

        [Fact]
        public async Task Init_RemoteTunnel_NeverUsesAnEmptyAddressOrPortZero()
        {
            var driver = CreateDriver("10.0.0.5", 3671, true);

            Assert.True(await driver.Init());

            var tunnel = Assert.Single(_tunnels.Tunnels);
            Assert.False(string.IsNullOrWhiteSpace(tunnel.Address));
            Assert.NotEqual("0.0.0.0", tunnel.Address);
            Assert.NotEqual(0, tunnel.Port);
        }

        [Fact]
        public async Task Init_TunnelDisabled_CreatesNoTunnel()
        {
            var driver = CreateDriver("192.0.2.10", 3671, false);

            Assert.True(await driver.Init());

            Assert.Empty(_tunnels.Tunnels);
        }

        [Fact]
        public async Task Init_RemoteFeatureNotLicensed_CreatesNoTunnel()
        {
            var driver = CreateDriver("192.0.2.10", 3671, true, new LicenseContract());

            Assert.True(await driver.Init());

            Assert.Empty(_tunnels.Tunnels);
        }

        [Fact]
        public async Task Init_TunnelingProviderUnavailable_CreatesNoTunnel()
        {
            _tunnels.Available = false;
            var driver = CreateDriver("192.0.2.10", 3671, true);

            Assert.True(await driver.Init());

            Assert.Empty(_tunnels.Tunnels);
        }

        [Fact]
        public async Task Init_TunnelCreationFails_InitStillSucceeds()
        {
            _tunnels.Throw = new InvalidOperationException("cloud down");
            var driver = CreateDriver("192.0.2.10", 3671, true);

            Assert.True(await driver.Init());
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task Init_EmptyIp_FailsWithoutTunnel(string ip)
        {
            var driver = CreateDriver(ip, 3671, true);

            Assert.False(await driver.Init());
            Assert.Empty(_tunnels.Tunnels);
        }

        [Theory]
        [InlineData("not-an-ip")]
        [InlineData("300.1.1.1")]
        public async Task Init_InvalidIp_FailsWithoutTunnel(string ip)
        {
            var driver = CreateDriver(ip, 3671, true);

            Assert.False(await driver.Init());
            Assert.Empty(_tunnels.Tunnels);
        }

        [Fact]
        public async Task Init_OnlyUseTunnel_StartDoesNotConnect()
        {
            var driver = CreateDriver("192.0.2.10", 3671, true);
            driver.DriverContext.NodeInstance.SetProperty("knx-only-use-tunnel", true);

            Assert.True(await driver.Init());
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            Assert.True(await driver.Start(cts.Token)); // returns at once, nothing connects to 192.0.2.10

            Assert.Single(_tunnels.Tunnels);
        }
    }
}
