using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Driver.Shelly.Gen1.Options;
using Automatica.Driver.Shelly.Gen2;
using Automatica.Driver.Shelly.Gen2.Models;
using Automatica.Driver.Shelly.Unit.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Automatica.Driver.Shelly.Unit.Tests
{
    /// <summary>
    /// Gen2 client: JSON-RPC over HTTP POST /rpc and status notifications over the ws://{ip}/rpc websocket,
    /// both served by a fake device on 127.0.0.1.
    /// </summary>
    public class ShellyGen2ClientTests : IAsyncLifetime
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        private const string SwitchStatusResult =
            "{\"id\":\"1\",\"src\":\"shellyplus1pm-a8032ab12345\",\"result\":{\"id\":0,\"source\":\"init\",\"output\":true," +
            "\"apower\":12.5,\"voltage\":231.2,\"freq\":50.0,\"current\":0.054,\"pf\":0.9," +
            "\"aenergy\":{\"total\":1234.5,\"by_minute\":[1.0,2.0,3.0],\"minute_ts\":1700000000},\"temperature\":{\"tC\":41.2,\"tF\":106.2}}}";

        private FakeShellyServer _server;
        private readonly ShellyOptions _options = new() { Password = "made-up-pw", SourceId = Guid.Parse("8c1f5f0a-1d1e-4e55-9b49-2f6f1c3c0a11") };

        public async Task InitializeAsync()
        {
            _server = await FakeShellyServer.StartAsync();
            _options.IpAddress = _server.Address;
            _options.DefaultTimeout = TimeSpan.FromSeconds(5);
        }

        public async Task DisposeAsync()
        {
            await _server.DisposeAsync();
        }

        private ShellyGen2Client CreateClient() => new(new EmptyTelegramMonitorInstance(), _options, NullLogger.Instance);

        private void ServeRpc(string json)
        {
            _server.Handler = r => r.Method == "POST" && r.Path == "/rpc" ? FakeResponse.Json(json) : new FakeResponse(404, "");
        }

        private JObject LastRpc() => JObject.Parse(_server.Requests.Last(a => a.Path == "/rpc").Body);

        [Fact]
        public async Task GetRelayState_PostsSwitchGetStatusRpc()
        {
            ServeRpc(SwitchStatusResult);
            using var client = CreateClient();

            var state = await client.GetRelayState(1);

            Assert.True(state);
            var rpc = LastRpc();
            Assert.Equal("Switch.GetStatus", (string)rpc["method"]);
            Assert.Equal(1, (int)rpc["params"]!["id"]);
            Assert.Equal(_options.SourceId.ToString(), (string)rpc["src"]);
            Assert.False(string.IsNullOrEmpty((string)rpc["id"]));
            Assert.Null(rpc["auth"]); // no digest before the device asked for it
        }

        [Fact]
        public async Task Rpc_UsesANewIdPerRequest()
        {
            ServeRpc(SwitchStatusResult);
            using var client = CreateClient();

            await client.GetRelayState(0);
            await client.GetRelayState(0);

            var ids = _server.Requests.Select(a => (string)JObject.Parse(a.Body)["id"]).ToList();
            Assert.Equal(2, ids.Distinct().Count());
        }

        [Fact]
        public async Task SwitchValues_AreReadFromTheResult()
        {
            ServeRpc(SwitchStatusResult);
            using var client = CreateClient();

            Assert.Equal(12.5, await client.GetRelayPower(0));
            Assert.Equal(231.2, await client.GetRelayVoltage(0));
            Assert.Equal(0.054, await client.GetRelayCurrent(0));
            Assert.Equal(1700000000, await client.GetRelayEnergyTimestamp(0));
        }

        [Fact]
        public async Task GetRelayEnergy_IsAenergyTotalInWh_NotDividedLikeGen1()
        {
            ServeRpc(SwitchStatusResult);
            using var client = CreateClient();

            Assert.Equal(1234.5, await client.GetRelayEnergy(0));
        }

        [Fact]
        public async Task GetRelayEnergy_WithoutAenergy_IsZero()
        {
            ServeRpc("{\"id\":\"1\",\"result\":{\"id\":0,\"output\":false,\"apower\":0}}");
            using var client = CreateClient();

            Assert.Equal(0d, await client.GetRelayEnergy(0));
            Assert.Equal(0L, await client.GetRelayEnergyTimestamp(0));
        }

        [Fact]
        public async Task GetRelayState_RpcError_ThrowsWithTheDeviceMessage()
        {
            ServeRpc("{\"id\":\"1\",\"error\":{\"code\":-105,\"message\":\"Argument 'id', value 7 not found!\"}}");
            using var client = CreateClient();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetRelayState(7));

            Assert.Contains("value 7 not found", ex.Message);
            Assert.Contains("switch 7", ex.Message);
        }

        [Fact]
        public async Task GetRelayState_HttpFailure_ThrowsInsteadOfNullReference()
        {
            _server.Handler = _ => new FakeResponse(500, "");
            using var client = CreateClient();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetRelayPower(0));
            Assert.Contains("InternalServerError", ex.Message);
        }

        [Fact]
        public async Task GetRelayState_EmptyResult_Throws()
        {
            ServeRpc("{\"id\":\"1\"}");
            using var client = CreateClient();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetRelayState(0));
            Assert.Contains("empty response", ex.Message);
        }

        [Fact]
        public async Task GetRelayState_DeviceOffline_Throws()
        {
            using var offline = FakeShellyServer.StartOfflineDevice();
            _options.IpAddress = offline.Address;
            using var client = CreateClient();

            await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetRelayState(0));
        }

        [Fact]
        public async Task SetRelayState_WithResetTimer_SendsToggleAfter()
        {
            ServeRpc("{\"id\":\"1\",\"result\":{\"was_on\":false}}");
            using var client = CreateClient();

            Assert.True(await client.SetRelayState(0, true, 15));

            var rpc = LastRpc();
            Assert.Equal("Switch.Set", (string)rpc["method"]);
            Assert.Equal(0, (int)rpc["params"]!["id"]);
            Assert.True((bool)rpc["params"]!["on"]);
            Assert.Equal(15, (int)rpc["params"]!["toggle_after"]);
        }

        [Fact]
        public async Task SetRelayState_WithoutResetTimer_SendsNoToggleAfter()
        {
            ServeRpc("{\"id\":\"1\",\"result\":{\"was_on\":true}}");
            using var client = CreateClient();

            await client.SetRelayState(1, false, 0);

            var rpc = LastRpc();
            Assert.False((bool)rpc["params"]!["on"]);
            Assert.Null(rpc["params"]!["toggle_after"]);
        }

        [Fact]
        public async Task GetHasUpdate_IsAlwaysFalse_WithoutAskingTheDevice()
        {
            using var client = CreateClient();

            Assert.False(await client.GetHasUpdate());
            Assert.Empty(_server.Requests);
        }

        [Fact]
        public async Task GetInfo_ReadsTheMacFromShellyEndpoint()
        {
            _server.Handler = r => r.Path == "/shelly" ? FakeResponse.Json("{\"id\":\"shellyplus1pm-a8032ab12345\",\"mac\":\"A8032AB12345\",\"gen\":2}") : new FakeResponse(404, "");
            using var client = CreateClient();

            Assert.Equal("A8032AB12345", (await client.GetInfo()).Mac);
        }

        [Fact]
        public async Task Connect_OpensTheWebsocket_AndRequestsTheStatus()
        {
            using var client = CreateClient();
            var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            client.OnOpened += _ => opened.TrySetResult();

            Assert.True(await client.Connect());

            var message = JObject.Parse(await _server.ReceiveWebSocketMessageAsync(Timeout));
            Assert.Equal("Shelly.GetStatus", (string)message["method"]);
            Assert.Equal("1", (string)message["id"]);
            Assert.Equal(_options.SourceId.ToString(), (string)message["src"]);
            Assert.Null(message["auth"]);

            await opened.Task.WaitAsync(Timeout);
            await client.Disconnect();
        }

        [Fact]
        public async Task Connect_DeviceOffline_ReturnsFalse()
        {
            using var offline = FakeShellyServer.StartOfflineDevice();
            _options.IpAddress = offline.Address;
            using var client = CreateClient();

            Assert.False(await client.Connect());
        }

        [Fact]
        public async Task WebSocketNotifyStatus_RaisesTheNotifyEventWithTheSwitchValues()
        {
            using var client = CreateClient();
            var events = new BlockingCollection<NotifyStatusEvent>();
            client.OnNotifyEvent += (_, e) => events.Add(e);

            await client.Connect();
            await _server.ReceiveWebSocketMessageAsync(Timeout);

            await _server.SendWebSocketMessageAsync(
                "{\"src\":\"shellyplus2pm-a8032ab12345\",\"dst\":\"x\",\"method\":\"NotifyStatus\",\"params\":{\"ts\":1700000001.25," +
                "\"switch:1\":{\"id\":1,\"apower\":42.5,\"current\":0.19,\"output\":true,\"source\":\"WS_in\"}}}", Timeout);

            Assert.True(events.TryTake(out var e, Timeout));
            Assert.Equal(1700000001.25, e.Ts);
            Assert.Null(e.Switch0);
            Assert.NotNull(e.Switch1);
            Assert.True(e.Switch1.Output);
            Assert.Equal(42.5, e.Switch1.Power);
            Assert.Equal(0.19, e.Switch1.Current);
            Assert.Equal("WS_in", e.Switch1.Source);

            await client.Disconnect();
        }

        [Fact]
        public async Task WebSocketResponseWithoutNotifyMethod_RaisesNoEvent()
        {
            using var client = CreateClient();
            var events = new BlockingCollection<NotifyStatusEvent>();
            client.OnNotifyEvent += (_, e) => events.Add(e);

            await client.Connect();
            await _server.ReceiveWebSocketMessageAsync(Timeout);

            // the answer to Shelly.GetStatus, then a notification as a marker that the first one was processed
            await _server.SendWebSocketMessageAsync("{\"id\":\"1\",\"src\":\"dev\",\"result\":{\"switch:0\":{\"output\":true}}}", Timeout);
            await _server.SendWebSocketMessageAsync("{\"src\":\"dev\",\"method\":\"NotifyStatus\",\"params\":{\"ts\":2.0}}", Timeout);

            Assert.True(events.TryTake(out var e, Timeout));
            Assert.Equal(2.0, e.Ts);
            Assert.False(events.TryTake(out _, TimeSpan.FromMilliseconds(100)));

            await client.Disconnect();
        }

        [Fact]
        public async Task WebSocket401_MakesFollowingRpcsCarryASha256Digest()
        {
            ServeRpc(SwitchStatusResult);
            using var client = CreateClient();
            await client.Connect();
            await _server.ReceiveWebSocketMessageAsync(Timeout);

            await _server.SendWebSocketMessageAsync(Auth401("shellyplus1pm-a8032ab12345", 1625038762), Timeout);

            var auth = await WaitForAuthAsync(client);

            Assert.Equal("shellyplus1pm-a8032ab12345", (string)auth["realm"]);
            Assert.Equal("admin", (string)auth["username"]);
            Assert.Equal(1625038762L, (long)auth["nonce"]);
            Assert.Equal("SHA-256", (string)auth["algorithm"]);

            var cnonce = (long)auth["cnonce"];
            Assert.True(cnonce >= 0);
            Assert.Equal(ExpectedDigest("admin", "shellyplus1pm-a8032ab12345", "made-up-pw", 1625038762, cnonce), (string)auth["response"]);

            await client.Disconnect();
        }

        [Fact]
        public async Task WebSocket401_UsesAFreshRandomCnonceEachTime()
        {
            ServeRpc(SwitchStatusResult);
            using var client = CreateClient();
            await client.Connect();
            await _server.ReceiveWebSocketMessageAsync(Timeout);

            await _server.SendWebSocketMessageAsync(Auth401("realm-a", 1), Timeout);
            var first = await WaitForAuthAsync(client, a => (long)a["nonce"] == 1);

            await _server.SendWebSocketMessageAsync(Auth401("realm-a", 2), Timeout);
            var second = await WaitForAuthAsync(client, a => (long)a["nonce"] == 2);

            Assert.NotEqual((long)first["cnonce"], (long)second["cnonce"]);
            Assert.NotEqual((string)first["response"], (string)second["response"]);

            await client.Disconnect();
        }

        private static string Auth401(string realm, long nonce)
        {
            var inner = $"{{\"auth_type\":\"digest\",\"nonce\":{nonce},\"nc\":1,\"realm\":\"{realm}\",\"algorithm\":\"SHA-256\"}}";
            return new JObject
            {
                ["id"] = "1",
                ["src"] = "dev",
                ["error"] = new JObject { ["code"] = 401, ["message"] = inner }
            }.ToString();
        }

        private async Task<JObject> WaitForAuthAsync(ShellyGen2Client client, Func<JObject, bool> predicate = null)
        {
            var deadline = DateTime.UtcNow + Timeout;
            while (DateTime.UtcNow < deadline)
            {
                await client.GetRelayState(0);
                var auth = LastRpc()["auth"] as JObject;
                if (auth != null && (predicate == null || predicate(auth)))
                {
                    return auth;
                }
                await Task.Delay(20);
            }

            throw new TimeoutException("the client never sent an auth object");
        }

        private static string ExpectedDigest(string user, string realm, string password, long nonce, long cnonce)
        {
            static string Sha(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s))).ToLowerInvariant();
            return Sha($"{Sha($"{user}:{realm}:{password}")}:{nonce}:1:{cnonce}:auth:{Sha("dummy_method:dummy_uri")}");
        }
    }
}
