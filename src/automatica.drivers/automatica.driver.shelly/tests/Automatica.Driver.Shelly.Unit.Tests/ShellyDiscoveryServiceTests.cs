using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Driver.Discovery;
using Automatica.Driver.ShellyFactory.Discovery;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Automatica.Driver.Shelly.Unit.Tests
{
    public class ShellyDiscoveryServiceTests
    {
        private sealed class FakeZeroconf : IZeroconfDiscovery
        {
            private readonly Dictionary<string, List<ZeroconfHost>> _hosts = new();
            public List<string> Queried { get; } = new();

            public FakeZeroconf Add(string service, string displayName, string ip, string gen = null)
            {
                var props = new List<IReadOnlyDictionary<string, string>>
                {
                    gen == null ? new Dictionary<string, string> { ["app"] = "x" } : new Dictionary<string, string> { ["gen"] = gen }
                };
                var services = new Dictionary<string, ZeroconfService>
                {
                    [$"{displayName}.{service}"] = new ZeroconfService(displayName, service, 80, 120, props)
                };

                if (!_hosts.TryGetValue(service, out var list))
                {
                    _hosts[service] = list = new List<ZeroconfHost>();
                }
                list.Add(new ZeroconfHost(displayName, displayName, ip, new[] { ip }, services));
                return this;
            }

            public Task<IEnumerable<ZeroconfHost>> DiscoverAsync(string serviceType, TimeSpan timeout)
            {
                Queried.Add(serviceType);
                return Task.FromResult<IEnumerable<ZeroconfHost>>(_hosts.TryGetValue(serviceType, out var l) ? l : new List<ZeroconfHost>());
            }
        }

        private const string Http = "_http._tcp.local.";
        private const string ShellyTcp = "_shelly._tcp.local.";

        private static Task<List<ShellyDevice>> Search(FakeZeroconf zc) => new ShellyDiscoveryService(zc, NullLogger.Instance).SearchShellys();

        [Theory]
        [InlineData("shelly1-A4CF12F3D2B1", ShellyDeviceType.Shelly1)]
        [InlineData("shelly1pm-A4CF12F3D2B1", ShellyDeviceType.Shelly1Pm)]
        [InlineData("shellyswitch25-A4CF12F3D2B1", ShellyDeviceType.Shelly25)]
        [InlineData("shellyplus1pm-a8032ab12345", ShellyDeviceType.ShellyPlus1Pm)]
        [InlineData("shellyplus2pm-a8032ab12345", ShellyDeviceType.ShellyPlus2Pm)]
        [InlineData("shellyplusplugs-a8032ab12345", ShellyDeviceType.ShellyPlugS)]
        public async Task SearchShellys_MapsTheHostNameToTheDeviceType(string name, ShellyDeviceType expected)
        {
            var devices = await Search(new FakeZeroconf().Add(Http, name, "192.168.1.20"));

            var device = Assert.Single(devices);
            Assert.Equal(expected, device.Type);
            Assert.Equal(name, device.Id);
            Assert.Equal(name.Split('-')[1], device.DeviceId);
            Assert.Equal("192.168.1.20", device.IpAddress);
        }

        [Fact]
        public async Task SearchShellys_1And1PmAreDifferentTypes()
        {
            // regression: "1" used to map to the 1PM template, and 1PM was not recognized at all
            var devices = await Search(new FakeZeroconf()
                .Add(Http, "shelly1-AAAAAAAAAAAA", "10.0.0.1")
                .Add(Http, "shelly1pm-BBBBBBBBBBBB", "10.0.0.2"));

            Assert.Equal(ShellyDeviceType.Shelly1, devices.Single(a => a.IpAddress == "10.0.0.1").Type);
            Assert.Equal(ShellyDeviceType.Shelly1Pm, devices.Single(a => a.IpAddress == "10.0.0.2").Type);
        }

        [Theory]
        [InlineData("1", ShellyGeneration.Gen1)]
        [InlineData("2", ShellyGeneration.Gen2)]
        [InlineData("3", ShellyGeneration.Gen1)]
        [InlineData(null, ShellyGeneration.Gen1)]
        public async Task SearchShellys_ReadsTheGenerationFromTheTxtRecord(string gen, ShellyGeneration expected)
        {
            var devices = await Search(new FakeZeroconf().Add(ShellyTcp, "shellyplus1pm-a8032ab12345", "10.0.0.3", gen));

            Assert.Equal(expected, Assert.Single(devices).Generation);
        }

        [Fact]
        public async Task SearchShellys_UnknownTypesAndOtherHostsAreSkipped()
        {
            var devices = await Search(new FakeZeroconf()
                .Add(Http, "shellyht-AAAAAAAAAAAA", "10.0.0.4")
                .Add(Http, "printer-1234", "10.0.0.5"));

            Assert.Empty(devices);
        }

        [Fact]
        public async Task SearchShellys_QueriesBothServices_AndDeduplicatesById()
        {
            var zc = new FakeZeroconf()
                .Add(Http, "shellyplus1pm-a8032ab12345", "10.0.0.6", "2")
                .Add(ShellyTcp, "shellyplus1pm-a8032ab12345", "10.0.0.6", "2")
                .Add(ShellyTcp, "shellyplus2pm-b8032ab12345", "10.0.0.7", "2");

            var devices = await Search(zc);

            Assert.Equal(new[] { Http, ShellyTcp }, zc.Queried);
            Assert.Equal(2, devices.Count);
            Assert.Single(devices, a => a.Id == "shellyplus1pm-a8032ab12345");
        }
    }
}
