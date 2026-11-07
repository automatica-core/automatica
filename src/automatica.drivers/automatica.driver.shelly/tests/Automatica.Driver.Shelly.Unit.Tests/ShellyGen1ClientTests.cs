using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Driver.Shelly.Gen1.Clients;
using Automatica.Driver.Shelly.Gen1.Options;
using Automatica.Driver.Shelly.Unit.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Automatica.Driver.Shelly.Unit.Tests
{
    /// <summary>
    /// Gen1 REST client against a fake device on 127.0.0.1.
    /// </summary>
    public class ShellyGen1ClientTests : IAsyncLifetime
    {
        private FakeShellyServer _server;

        public async Task InitializeAsync()
        {
            _server = await FakeShellyServer.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await _server.DisposeAsync();
        }

        private static string StatusJson(string file = "./status_response_1PM.json") => File.ReadAllText(file);

        private ShellyGen1Client CreateClient(string ip = null, TimeSpan? timeout = null, string user = "admin", string password = "secret")
        {
            return new ShellyGen1Client(new EmptyTelegramMonitorInstance(), new ShellyOptions
            {
                IpAddress = ip ?? _server.Address,
                UserName = user,
                Password = password,
                DefaultTimeout = timeout ?? TimeSpan.FromSeconds(5)
            }, NullLogger.Instance);
        }

        private void ServeStatus(string json)
        {
            _server.Handler = r => r.Path == "/status" ? FakeResponse.Json(json) : new FakeResponse(404, "");
        }

        [Fact]
        public async Task GetStatus_Ok_ParsesRelaysAndMeters()
        {
            ServeStatus(StatusJson());
            using var client = CreateClient();

            var result = await client.GetStatus(CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.False(result.IsTransient);
            Assert.Equal(2, result.Value.Relays.Count);
            Assert.True(result.Value.Relays[0].IsOn);
            Assert.Single(result.Value.Meters);
            Assert.Equal(95.35, result.Value.Meters[0].Power);
            Assert.Equal("DC4F22766C9C", result.Value.MacAddress);
        }

        [Fact]
        public async Task GetStatus_SendsBasicAuthOfConfiguredCredentials()
        {
            ServeStatus(StatusJson());
            using var client = CreateClient(user: "admin", password: "made-up-pw");

            await client.GetStatus(CancellationToken.None);

            var request = Assert.Single(_server.Requests);
            Assert.Equal("GET", request.Method);
            Assert.Equal("/status", request.Path);
            Assert.Equal("Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes("admin:made-up-pw")), request.Authorization);
        }

        [Fact]
        public async Task GetStatus_ServiceUnavailable_IsTransientFailure()
        {
            _server.Handler = _ => new FakeResponse(503, "");
            using var client = CreateClient();

            var result = await client.GetStatus(CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.True(result.IsTransient);
            Assert.Contains("ServiceUnavailable", result.Message);
        }

        [Fact]
        public async Task GetStatus_NotFound_IsSuccessWithoutValue()
        {
            _server.Handler = _ => new FakeResponse(404, "");
            using var client = CreateClient();

            var result = await client.GetStatus(CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Null(result.Value);
            Assert.Contains("NotFound", result.Message);
        }

        [Fact]
        public async Task GetStatus_ServerError_IsPermanentFailure()
        {
            _server.Handler = _ => new FakeResponse(500, "boom");
            using var client = CreateClient();

            var result = await client.GetStatus(CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.False(result.IsTransient);
            Assert.Contains("InternalServerError", result.Message);
        }

        [Fact]
        public async Task GetStatus_DeviceTooSlow_FailsAfterTheTimeout()
        {
            _server.Handler = _ => new FakeResponse(200, StatusJson(), TimeSpan.FromSeconds(10));
            using var client = CreateClient(timeout: TimeSpan.FromMilliseconds(150));

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = await client.GetStatus(CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task GetStatus_DeviceOffline_IsFailureNotException()
        {
            using var offline = FakeShellyServer.StartOfflineDevice();
            using var client = CreateClient(ip: offline.Address);

            var result = await client.GetStatus(CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.False(string.IsNullOrEmpty(result.Message));
        }

        [Fact]
        public async Task GetStatus_InvalidJson_IsFailureNotException()
        {
            ServeStatus("{ not json");
            using var client = CreateClient();

            var result = await client.GetStatus(CancellationToken.None);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task GetRelayState_ReadsTheRequestedChannel()
        {
            ServeStatus(StatusJson());
            using var client = CreateClient();

            Assert.True(await client.GetRelayState(0, CancellationToken.None));
            Assert.False(await client.GetRelayState(1, CancellationToken.None));
        }

        [Fact]
        public async Task GetRelayState_StatusFailure_Throws()
        {
            _server.Handler = _ => new FakeResponse(500, "");
            using var client = CreateClient();

            await Assert.ThrowsAnyAsync<Exception>(() => client.GetRelayState(0, CancellationToken.None));
        }

        [Fact]
        public async Task GetRelayEnergy_ConvertsGen1WattMinutesToWh()
        {
            // meters[0].total = 8849733 Wmin
            ServeStatus(StatusJson());
            using var client = CreateClient();

            var energy = await client.GetRelayEnergy(0);

            Assert.Equal(8849733 / 60d, energy, 6);
        }

        [Fact]
        public async Task GetRelayPowerCurrentVoltageAndTimestamp_ComeFromTheMeter()
        {
            ServeStatus(StatusJson());
            using var client = CreateClient();

            Assert.Equal(95.35, await client.GetRelayPower(0));
            Assert.Equal(95.35 / 230, await client.GetRelayCurrent(0), 6);
            Assert.Equal(230d, await client.GetRelayVoltage(0));
            Assert.Equal(1588532068, await client.GetRelayEnergyTimestamp(0));
        }

        [Fact]
        public async Task GetHasUpdate_ReflectsTheStatusFlag()
        {
            ServeStatus(StatusJson());
            using var client = CreateClient();

            Assert.True(await client.GetHasUpdate(CancellationToken.None));
        }

        [Fact]
        public async Task GetHasUpdate_StatusFailure_IsFalse()
        {
            _server.Handler = _ => new FakeResponse(500, "");
            using var client = CreateClient();

            Assert.False(await client.GetHasUpdate(CancellationToken.None));
        }

        [Theory]
        [InlineData(true, 0, "?turn=on")]
        [InlineData(false, 0, "?turn=off")]
        [InlineData(true, 30, "?turn=on&timer=30")]
        [InlineData(false, 5, "?turn=off&timer=5")]
        public async Task SetRelayState_SendsTurnAndOptionalTimer(bool on, int timer, string expectedQuery)
        {
            _server.Handler = r => r.Path == "/relay/1" ? FakeResponse.Json($"{{\"ison\":{(on ? "true" : "false")},\"has_timer\":false}}") : new FakeResponse(404, "");
            using var client = CreateClient();

            var state = await client.SetRelayState(1, on, timer, CancellationToken.None);

            Assert.Equal(on, state);
            var request = Assert.Single(_server.Requests);
            Assert.Equal("/relay/1", request.Path);
            Assert.Equal(expectedQuery, request.Query);
            Assert.StartsWith("Basic ", request.Authorization);
        }

        [Fact]
        public async Task GetInfo_ReturnsTheMac()
        {
            _server.Handler = r => r.Path == "/shelly" ? FakeResponse.Json("{\"type\":\"SHSW-PM\",\"mac\":\"A4CF12F3D2B1\"}") : new FakeResponse(404, "");
            using var client = CreateClient();

            var info = await client.GetInfo();

            Assert.Equal("A4CF12F3D2B1", info.Mac);
        }

        [Fact]
        public async Task GetInfo_DeviceOffline_ReturnsNull()
        {
            using var offline = FakeShellyServer.StartOfflineDevice();
            using var client = CreateClient(ip: offline.Address);

            Assert.Null(await client.GetInfo());
        }

        [Fact]
        public async Task Dispose_ReleasesTheHttpClient()
        {
            ServeStatus(StatusJson());
            var client = CreateClient();
            client.Dispose();

            var result = await client.GetStatus(CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Empty(_server.Requests);
        }

        [Fact]
        public async Task Connect_And_Disconnect_AreNoOpsForGen1()
        {
            using var client = CreateClient();

            Assert.True(await client.Connect());
            Assert.True(await client.Disconnect());
            Assert.Empty(_server.Requests);
        }

        [Fact]
        public void Constructor_WithoutOptions_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ShellyGen1Client(new EmptyTelegramMonitorInstance(), null, NullLogger.Instance));
        }
    }
}
