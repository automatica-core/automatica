using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Automatica.Driver.Shelly.Gen1.Clients;
using Automatica.Driver.Shelly.Gen2;
using Automatica.Driver.Shelly.Unit.Tests.Fakes;
using Automatica.Driver.ShellyFactory;
using Microsoft.Extensions.Logging;
using Moq;
using ShellyGeneration = Automatica.Driver.ShellyFactory.Discovery.ShellyGeneration;
using Xunit;

namespace Automatica.Driver.Shelly.Unit.Tests
{
    /// <summary>
    /// <see cref="ShellyDriverDevice"/> with its real relay node tree, talking to a fake device on 127.0.0.1.
    /// </summary>
    public class ShellyDriverDeviceTests : DriverFactoryTestBase<ShellyFactory.ShellyFactory>, IAsyncLifetime
    {
        private const string Mac = "DC4F22766C9C";
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        // Shelly 1PM (Gen1) relay templates
        private static readonly Guid Gen1RelaysInterface = new("f66e7c26-1341-4016-8d64-4c1b7db28357");
        private static readonly Guid Gen1RelayBase = new("e2684369-6b1a-450b-b337-ff5d0bcb1888");

        // Shelly Plus 1PM (Gen2) relay templates
        private static readonly Guid Gen2RelaysInterface = new("1483043e-b0de-4598-8af4-1cf218f95978");
        private static readonly Guid Gen2RelayBase = new("2984e4bb-62ff-499e-b4ba-5dbc3d09d210");

        private FakeShellyServer _server;
        private readonly CapturingLogger _logger = new();
        private readonly ConcurrentQueue<object> _dispatched = new();
        private readonly Mock<IDispatcher> _dispatcher = new();

        public ShellyDriverDeviceTests()
        {
            _dispatcher.Setup(a => a.DispatchValue(It.IsAny<IDispatchable>(), It.IsAny<object>(), It.IsAny<DispatchValueSource>()))
                .Callback<IDispatchable, object, DispatchValueSource>((_, value, _) => _dispatched.Enqueue(value))
                .Returns(Task.CompletedTask);
        }

        public async Task InitializeAsync()
        {
            _server = await FakeShellyServer.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await _server.DisposeAsync();
        }

        private static Guid Offset(Guid guid, int c)
        {
            var bytes = guid.ToByteArray();
            bytes[^1] = (byte)(bytes[^1] + c);
            return new Guid(bytes);
        }

        private NodeInstance Node(Guid template)
        {
            var node = CreateNodeInstance(template);
            node.ObjId = Guid.NewGuid();
            return node;
        }

        private ShellyDriverDevice CreateDevice(Guid deviceTemplate, Guid relaysInterface, Guid relayBase, string shellyId = Mac)
        {
            var device = Node(deviceTemplate);
            device.SetProperty("shelly-use-ip", true);
            device.SetProperty("shelly-ip", _server.Address);
            device.SetProperty(ShellyFactory.ShellyFactory.DeviceIdPropertyKey, shellyId);
            device.SetProperty("polling-interval", 3_600_000); // no timer poll during a test

            var relays = Node(relaysInterface);
            var relay = Node(Offset(relayBase, 2));
            var relayState = Node(Offset(relayBase, 4));
            relay.InverseThis2ParentNodeInstanceNavigation.Add(relayState);
            relays.InverseThis2ParentNodeInstanceNavigation.Add(relay);
            device.InverseThis2ParentNodeInstanceNavigation.Add(relays);

            var ctx = new TestDriverContext(device, DriverFactory, Factory, _dispatcher.Object, _logger);
            return new ShellyDriverDevice(ctx, new EmptyTelegramMonitorInstance());
        }

        private async Task<ShellyDriverDevice> CreateGen1Device(string shellyId = Mac)
        {
            var device = CreateDevice(ShellyFactory.ShellyFactory.Shelly1PmDevice, Gen1RelaysInterface, Gen1RelayBase, shellyId);
            Assert.True(await device.Configure());
            return device;
        }

        private void ServeGen1(string mac = Mac, int statusCode = 200)
        {
            var status = File.ReadAllText("./status_response_1PM.json");
            _server.Handler = r => r.Path switch
            {
                "/shelly" => FakeResponse.Json($"{{\"type\":\"SHSW-PM\",\"mac\":\"{mac}\"}}"),
                "/status" => statusCode == 200 ? FakeResponse.Json(status) : new FakeResponse(statusCode, ""),
                _ => new FakeResponse(404, "")
            };
        }

        [Fact]
        public async Task Configure_CreatesTheGen1ClientTypeFromTheTemplate()
        {
            var device = await CreateGen1Device();

            Assert.Equal(ShellyGeneration.Gen1, device.Generation);
            Assert.Equal(Mac, device.ShellyId);
            Assert.Equal(3_600_000, device.PollingInterval);
        }

        [Fact]
        public async Task Start_InitialPollFails_LogsTheErrorAndReturnsFalse_InsteadOfThrowing()
        {
            ServeGen1(statusCode: 500);
            var device = await CreateGen1Device();

            var started = await device.Start();

            Assert.False(started);
            Assert.True(_logger.Contains(LogLevel.Error, "Could not read initial values"));
            Assert.Empty(_dispatched);
            await device.Stop();
        }

        [Fact]
        public async Task Start_DeviceOffline_ReturnsFalseWithAnOfflineError()
        {
            _server.Handler = _ => new FakeResponse(503, "");
            var device = await CreateGen1Device();

            Assert.False(await device.Start());
            Assert.True(_logger.Contains(LogLevel.Error, "offline or not reachable"));
            await device.Stop();
        }

        [Fact]
        public async Task Start_OtherDeviceAtTheAddress_ReturnsFalse()
        {
            ServeGen1(mac: "AABBCCDDEEFF");
            var device = await CreateGen1Device();

            Assert.False(await device.Start());
            Assert.True(_logger.Contains(LogLevel.Error, "Invalid shelly id"));
            Assert.DoesNotContain(_server.Requests, a => a.Path == "/status");
            await device.Stop();
        }

        [Fact]
        public async Task Start_MacMatchesIgnoringCaseAndWhitespace_StartsAndDispatchesTheInitialRelayState()
        {
            ServeGen1(mac: " dc4f22766c9c ");
            var device = await CreateGen1Device(shellyId: "DC4F22766C9C ");

            Assert.True(await device.Start());

            Assert.Contains(true, _dispatched.OfType<bool>());
            Assert.IsType<ShellyGen1Client>(device.Client);
            await device.Stop();
        }

        [Fact]
        public async Task Start_Retried_DisposesThePreviousClient()
        {
            ServeGen1(statusCode: 500);
            var device = await CreateGen1Device();

            Assert.False(await device.Start());
            var first = device.Client;

            ServeGen1();
            Assert.True(await device.Start());

            Assert.NotSame(first, device.Client);
            // the released client's HttpClient is disposed, so it cannot reach the device any more
            Assert.Null(await first.GetInfo());
            await device.Stop();
        }

        [Fact]
        public async Task Stop_ReleasesTheClient()
        {
            ServeGen1();
            var device = await CreateGen1Device();
            Assert.True(await device.Start());

            await device.Stop();

            Assert.Null(device.Client);
        }

        [Fact]
        public async Task Gen2Start_ConnectsTheWebsocket_ReadsViaRpc_AndAppliesNotifications()
        {
            var output = true;
            _server.Handler = r => r.Path switch
            {
                "/shelly" => FakeResponse.Json($"{{\"id\":\"shellyplus1pm-{Mac.ToLowerInvariant()}\",\"mac\":\"{Mac}\",\"gen\":2}}"),
                "/rpc" when r.Method == "POST" => FakeResponse.Json($"{{\"id\":\"1\",\"result\":{{\"id\":0,\"output\":{(output ? "true" : "false")},\"apower\":1.5}}}}"),
                _ => new FakeResponse(404, "")
            };

            var device = CreateDevice(ShellyFactory.ShellyFactory.ShellyPlus1PmDevice, Gen2RelaysInterface, Gen2RelayBase);
            Assert.True(await device.Configure());
            Assert.Equal(ShellyGeneration.Gen2, device.Generation);

            Assert.True(await device.Start());
            Assert.IsType<ShellyGen2Client>(device.Client);
            Assert.Contains(_server.Requests, a => a.Path == "/rpc" && a.Body.Contains("Switch.GetStatus"));
            Assert.Contains(true, _dispatched.OfType<bool>());

            var hello = await _server.ReceiveWebSocketMessageAsync(Timeout);
            Assert.Contains("Shelly.GetStatus", hello);

            // the relay was switched off at the device: the notification makes the node re-read and dispatch the new state
            output = false;
            await _server.SendWebSocketMessageAsync("{\"src\":\"dev\",\"method\":\"NotifyStatus\",\"params\":{\"ts\":1,\"switch:0\":{\"id\":0,\"output\":false}}}", Timeout);

            var deadline = DateTime.UtcNow + Timeout;
            while (!_dispatched.OfType<bool>().Contains(false) && DateTime.UtcNow < deadline)
            {
                await Task.Delay(20);
            }

            Assert.Contains(false, _dispatched.OfType<bool>());
            await device.Stop();
            Assert.Null(device.Client);
        }
    }
}
