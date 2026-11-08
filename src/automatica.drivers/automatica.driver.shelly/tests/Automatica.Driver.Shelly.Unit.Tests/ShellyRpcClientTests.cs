using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Driver.Shelly.Rpc;
using Automatica.Driver.Shelly.Unit.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Automatica.Driver.Shelly.Unit.Tests
{
    /// <summary>
    /// The Gen2+ JSON-RPC client (POST /rpc with SHA-256 digest, websocket with notifications) and the outbound websocket server.
    /// </summary>
    public class ShellyRpcClientTests : IAsyncLifetime
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
        private FakeShellyServer _server;

        public async Task InitializeAsync() => _server = await FakeShellyServer.StartAsync();

        public async Task DisposeAsync() => await _server.DisposeAsync();

        [Fact]
        public void Digest_ParsesTheChallengeAndBuildsTheDeviceResponse()
        {
            var challenge = ShellyDigestAuth.ParseChallenge("Digest qop=\"auth\", realm=\"shellyplus1pm-a8032ab12345\", nonce=\"60dc59c6\", algorithm=SHA-256");

            Assert.Equal("shellyplus1pm-a8032ab12345", challenge.Realm);
            Assert.Equal("60dc59c6", challenge.Nonce);
            Assert.Equal("auth", challenge.Qop);
            Assert.Null(ShellyDigestAuth.ParseChallenge("Basic realm=\"x\""));
            Assert.Null(ShellyDigestAuth.ParseChallenge(null));

            var header = ShellyDigestAuth.CreateAuthorizationHeader(challenge, "pw", "POST", "/rpc", 1, "abcd");
            var ha1 = ShellyDigestAuth.Sha256Hex("admin:shellyplus1pm-a8032ab12345:pw");
            var ha2 = ShellyDigestAuth.Sha256Hex("POST:/rpc");
            var expected = ShellyDigestAuth.Sha256Hex($"{ha1}:60dc59c6:00000001:abcd:auth:{ha2}");

            Assert.StartsWith("Digest ", header);
            Assert.Contains("username=\"admin\"", header);
            Assert.Contains("nc=00000001", header);
            Assert.Contains($"response=\"{expected}\"", header);
            Assert.Contains("algorithm=SHA-256", header);
        }

        [Fact]
        public void Digest_RpcAuthObjectOfTheWebSocket()
        {
            var auth = ShellyDigestAuth.CreateRpcAuth("shellypro4pm-f008d1d8b8b8", 1625038762, "pw", 313273, 1);

            var ha1 = ShellyDigestAuth.Sha256Hex("admin:shellypro4pm-f008d1d8b8b8:pw");
            var ha2 = ShellyDigestAuth.Sha256Hex("dummy_method:dummy_uri");
            Assert.Equal("admin", auth.Value<string>("username"));
            Assert.Equal("shellypro4pm-f008d1d8b8b8", auth.Value<string>("realm"));
            Assert.Equal(1625038762, auth.Value<long>("nonce"));
            Assert.Equal(313273, auth.Value<long>("cnonce"));
            Assert.Equal("SHA-256", auth.Value<string>("algorithm"));
            Assert.Equal(ShellyDigestAuth.Sha256Hex($"{ha1}:1625038762:1:313273:auth:{ha2}"), auth.Value<string>("response"));
        }

        [Fact]
        public async Task Call_WithoutAuth_ReturnsTheResult()
        {
            var fake = new FakeShellyDevice(_server, "gen2_plus1pm");
            using var client = new ShellyRpcClient(_server.Address, null, NullLogger.Instance, source: "test");

            var info = await client.CallAsync("Shelly.GetDeviceInfo");

            Assert.Equal("A8032AB12345", info!.Value<string>("mac"));
            var request = _server.Requests.Single();
            Assert.Equal("POST", request.Method);
            Assert.Equal("/rpc", request.Path);
            var frame = JObject.Parse(request.Body);
            Assert.Equal("test", frame.Value<string>("src"));
            Assert.Equal("Shelly.GetDeviceInfo", frame.Value<string>("method"));
            Assert.Single(fake.Calls);
        }

        [Fact]
        public async Task Call_WithParams_SendsThem()
        {
            var fake = new FakeShellyDevice(_server, "gen2_plus1pm");
            using var client = new ShellyRpcClient(_server.Address, null, NullLogger.Instance);

            await client.CallAsync("Switch.Set", new JObject { ["id"] = 0, ["on"] = true });

            var call = fake.Calls.Single();
            Assert.Equal("Switch.Set", call.Method);
            Assert.True(call.Params.Value<bool>("on"));
        }

        [Fact]
        public async Task Call_DigestProtected_RetriesWithTheChallengeAndReusesIt()
        {
            var fake = new FakeShellyDevice(_server, "gen2_plus1pm") { Password = "secret" };
            using var client = new ShellyRpcClient(_server.Address, "secret", NullLogger.Instance);

            var status = await client.CallAsync("Shelly.GetStatus");
            await client.CallAsync("Shelly.GetConfig");

            Assert.NotNull(status!["switch:0"]);
            // 401 + retry for the first call, the second call authenticates right away (nc 2)
            Assert.Equal(3, _server.Requests.Count);
            Assert.Contains("nc=00000002", _server.Requests.Last().Authorization);
            Assert.Equal(2, fake.Calls.Count);
        }

        [Fact]
        public async Task Call_WrongOrMissingPassword_Throws401()
        {
            _ = new FakeShellyDevice(_server, "gen2_plus1pm") { Password = "secret" };

            using var wrong = new ShellyRpcClient(_server.Address, "wrong", NullLogger.Instance);
            var e1 = await Assert.ThrowsAsync<ShellyRpcException>(() => wrong.CallAsync("Shelly.GetStatus"));
            Assert.Equal(401, e1.Code);
            Assert.Contains("rejected", e1.Message);

            using var none = new ShellyRpcClient(_server.Address, null, NullLogger.Instance);
            var e2 = await Assert.ThrowsAsync<ShellyRpcException>(() => none.CallAsync("Shelly.GetStatus"));
            Assert.Equal(401, e2.Code);
            Assert.Contains("password", e2.Message);
        }

        [Fact]
        public async Task Call_ErrorFrame_ThrowsWithTheRpcCode()
        {
            _server.Handler = r => FakeResponse.Json($"{{\"id\":{JObject.Parse(r.Body)["id"]},\"src\":\"x\",\"error\":{{\"code\":-103,\"message\":\"Invalid argument 'id'\"}}}}");
            using var client = new ShellyRpcClient(_server.Address, null, NullLogger.Instance);

            var e = await Assert.ThrowsAsync<ShellyRpcException>(() => client.CallAsync("Switch.Set", new JObject { ["id"] = 5 }));

            Assert.Equal(-103, e.Code);
            Assert.Contains("Invalid argument", e.Message);
        }

        [Fact]
        public async Task Call_OfflineOrSlow_ThrowsUnreachableOrTimeout()
        {
            using var offline = FakeShellyServer.StartOfflineDevice();
            using var offlineClient = new ShellyRpcClient(offline.Address, null, NullLogger.Instance);
            Assert.Equal(503, (await Assert.ThrowsAsync<ShellyRpcException>(() => offlineClient.CallAsync("Shelly.GetStatus"))).Code);

            _server.Handler = _ => new FakeResponse(200, "{}", TimeSpan.FromSeconds(5));
            using var slow = new ShellyRpcClient(_server.Address, null, NullLogger.Instance, timeout: TimeSpan.FromMilliseconds(300));
            Assert.Equal(408, (await Assert.ThrowsAsync<ShellyRpcException>(() => slow.CallAsync("Shelly.GetStatus"))).Code);
        }

        [Fact]
        public async Task WebSocket_SendsHelloAndRaisesStatusEventsAndTheHelloAnswer()
        {
            using var client = new ShellyRpcClient(_server.Address, null, NullLogger.Instance, source: "automatica-test");
            var notifications = new BlockingCollection<(string Method, JObject Params)>();
            client.Notification += (m, p) => notifications.Add((m, p));

            Assert.True(await client.ConnectAsync());
            var hello = JObject.Parse(await _server.ReceiveWebSocketMessageAsync(Timeout));
            Assert.Equal("automatica-test", hello.Value<string>("src"));
            Assert.Equal("Shelly.GetStatus", hello.Value<string>("method"));

            await _server.SendWebSocketMessageAsync($"{{\"id\":{hello["id"]},\"src\":\"shellyplus1pm-a8032ab12345\",\"result\":{{\"switch:0\":{{\"id\":0,\"output\":true}}}}}}", Timeout);
            await _server.SendWebSocketMessageAsync("{\"src\":\"shellyplus1pm-a8032ab12345\",\"dst\":\"automatica-test\",\"method\":\"NotifyStatus\",\"params\":{\"ts\":1.0,\"switch:0\":{\"id\":0,\"output\":false}}}", Timeout);
            await _server.SendWebSocketMessageAsync("not json", Timeout);
            await _server.SendWebSocketMessageAsync("{\"src\":\"x\",\"method\":\"NotifyEvent\",\"params\":{\"events\":[{\"component\":\"input:0\",\"event\":\"single_push\"}]}}", Timeout);

            Assert.True(notifications.TryTake(out var full, Timeout));
            Assert.Equal("NotifyFullStatus", full.Method);
            Assert.True(full.Params.SelectToken("switch:0.output")!.Value<bool>());

            Assert.True(notifications.TryTake(out var partial, Timeout));
            Assert.Equal("NotifyStatus", partial.Method);
            Assert.False(partial.Params.SelectToken("['switch:0'].output")!.Value<bool>());

            Assert.True(notifications.TryTake(out var e, Timeout));
            Assert.Equal("NotifyEvent", e.Method);
            Assert.True(client.IsWebSocketConnected);
        }

        [Fact]
        public async Task WebSocket_401OnHello_ResendsWithTheAuthObject()
        {
            using var client = new ShellyRpcClient(_server.Address, "pw", NullLogger.Instance);
            Assert.True(await client.ConnectAsync());

            var hello = JObject.Parse(await _server.ReceiveWebSocketMessageAsync(Timeout));
            Assert.Null(hello["auth"]);
            var challenge = "{\\\"auth_type\\\": \\\"digest\\\", \\\"nonce\\\": 1625038762, \\\"nc\\\": 1, \\\"realm\\\": \\\"shellyplus1pm-a8032ab12345\\\", \\\"algorithm\\\": \\\"SHA-256\\\"}";
            await _server.SendWebSocketMessageAsync($"{{\"id\":{hello["id"]},\"src\":\"shellyplus1pm-a8032ab12345\",\"error\":{{\"code\":401,\"message\":\"{challenge}\"}}}}", Timeout);

            var second = JObject.Parse(await _server.ReceiveWebSocketMessageAsync(Timeout));
            var auth = (JObject)second["auth"];
            Assert.NotNull(auth);
            var ha1 = ShellyDigestAuth.Sha256Hex("admin:shellyplus1pm-a8032ab12345:pw");
            var ha2 = ShellyDigestAuth.Sha256Hex("dummy_method:dummy_uri");
            Assert.Equal(ShellyDigestAuth.Sha256Hex($"{ha1}:1625038762:1:{auth.Value<long>("cnonce")}:auth:{ha2}"), auth.Value<string>("response"));
            Assert.NotEqual(hello.Value<int>("id"), second.Value<int>("id"));
        }

        [Fact]
        public async Task WebSocket_DeviceUnreachable_ConnectReportsFalseAndKeepsRetrying()
        {
            using var offline = FakeShellyServer.StartOfflineDevice();
            using var client = new ShellyRpcClient(offline.Address, null, NullLogger.Instance, timeout: TimeSpan.FromSeconds(1));
            client.ReconnectDelay = TimeSpan.FromMilliseconds(50);

            Assert.False(await client.ConnectAsync());
            Assert.False(client.IsWebSocketConnected);
            await client.DisconnectAsync();
        }

        [Fact]
        public async Task OutboundServer_RaisesTheFramesOfConnectingDevices()
        {
            using var server = new ShellyOutboundWebSocketServer(0, NullLogger.Instance, IPAddress.Loopback);
            server.Start();
            var received = new BlockingCollection<(string Source, string Method, JObject Params, IPAddress Remote)>();
            server.MessageReceived += (s, m, p, r) => received.Add((s, m, p, r));

            using var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri($"ws://127.0.0.1:{server.Port}/"), CancellationToken.None);
            async Task Send(string text) => await ws.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, CancellationToken.None);

            await Send("{\"src\":\"shellyplusht-c049ef8b1d2e\",\"dst\":\"ws\",\"method\":\"NotifyFullStatus\",\"params\":{\"temperature:0\":{\"id\":0,\"tC\":22.4}}}");
            await Send("{\"src\":\"shellyplusht-c049ef8b1d2e\",\"result\":{}}");
            await Send("{\"src\":\"shellyplusht-c049ef8b1d2e\",\"method\":\"NotifyStatus\",\"params\":{\"humidity:0\":{\"rh\":50}}}");

            Assert.True(received.TryTake(out var first, Timeout));
            Assert.Equal("shellyplusht-c049ef8b1d2e", first.Source);
            Assert.Equal("NotifyFullStatus", first.Method);
            Assert.Equal(22.4, first.Params.SelectToken("['temperature:0'].tC")!.Value<double>());
            Assert.Equal(IPAddress.Loopback, first.Remote.IsIPv4MappedToIPv6 ? first.Remote.MapToIPv4() : first.Remote);

            // frames without method (answers) are not raised
            Assert.True(received.TryTake(out var second, Timeout));
            Assert.Equal("NotifyStatus", second.Method);
        }

        [Fact]
        public async Task OutboundServer_PlainHttpRequest_Gets400()
        {
            using var server = new ShellyOutboundWebSocketServer(0, NullLogger.Instance, IPAddress.Loopback);
            server.Start();
            using var http = new HttpClient { Timeout = Timeout };

            var response = await http.GetAsync($"http://127.0.0.1:{server.Port}/");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
