using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Microsoft.AspNetCore.WebUtilities;
using P3.Driver.Mammotion.Driver;
using P3.Driver.Mammotion.Driver.Protocol;
using Xunit;

namespace P3.Driver.Mammotion.Tests
{
    internal sealed class FakeMqtt : IMammotionMqtt
    {
        public MqttCredentials Credentials { get; private set; }
        public IReadOnlyCollection<string> Topics { get; private set; }
        public Func<string, byte[], Task> OnMessage { get; private set; }
        public bool IsConnected { get; set; }
        public int Connects { get; private set; }

        public Task ConnectAsync(MqttCredentials credentials, IReadOnlyCollection<string> topics, Func<string, byte[], Task> onMessage, CancellationToken token)
        {
            Credentials = credentials;
            Topics = topics;
            OnMessage = onMessage;
            IsConnected = true;
            Connects++;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            IsConnected = false;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => new(DisconnectAsync());
    }

    public class MammotionDriverTests : DriverFactoryTestBase<MammotionDriverFactory>, IAsyncLifetime
    {
        private const string IotId = "iot-luba-1";
        private const string Pk = "a1BmXWlsdbA";
        private const string Dn = "Luba-VSLKJX";

        private FakeHttpServer _server;
        private readonly HttpClient _http = new();
        private readonly FakeMqtt _mqtt = new();
        private readonly List<JsonElement> _invokes = new();
        private int _logins;
        private bool _acceptOnlyPlainPassword;

        internal static string Jwt(string payloadJson)
        {
            static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return $"{B64("{\"alg\":\"none\"}")}.{B64(payloadJson)}.sig";
        }

        public async Task InitializeAsync()
        {
            _server = await FakeHttpServer.StartAsync();
            _server.Handler = Handle;
        }

        public async Task DisposeAsync()
        {
            await _server.DisposeAsync();
            _http.Dispose();
        }

        private string AccessToken => Jwt(JsonSerializer.Serialize(new { iot = _server.Url, n = _logins }));

        private (int, string) Handle(RecordedRequest r)
        {
            if (r.Path == "/oauth2/token")
            {
                var q = QueryHelpers.ParseQuery(r.Query);
                var expectedPassword = _acceptOnlyPlainPassword ? "pw" : Convert.ToBase64String(Encoding.UTF8.GetBytes("pw"));
                var ok = q["grant_type"] == "password"
                    ? q["username"] == "anna@example.com" && q["password"] == expectedPassword
                    : q["refresh_token"] == $"refresh-{_logins}";
                if (!ok)
                {
                    return (200, "{\"code\":1001,\"msg\":\"bad credentials\"}");
                }
                _logins++;
                return (200, JsonSerializer.Serialize(new
                {
                    code = 0,
                    data = new { access_token = AccessToken, refresh_token = $"refresh-{_logins}", expires_in = 7200, userInformation = new { userAccount = "5310" } }
                }));
            }

            if (!r.Headers.TryGetValue("Authorization", out var auth) || auth != $"Bearer {AccessToken}")
            {
                return (401, "{\"code\":401}");
            }

            switch (r.Path)
            {
                case "/v1/user/device/page":
                    return (200, JsonSerializer.Serialize(new
                    {
                        code = 0,
                        data = new
                        {
                            records = new object[]
                            {
                                new { iotId = IotId, productKey = Pk, deviceName = Dn, nickName = "Front lawn" },
                                new { iotId = "iot-rtk", productKey = "pk2", deviceName = "RTK-ABC123", nickName = "" },
                                new { iotId = "iot-yuka", productKey = "pk3", deviceName = "Yuka-XYZ", nickName = "" },
                            },
                            total = 3
                        }
                    }));
                case "/v1/mqtt/auth/jwt":
                    return (200, JsonSerializer.Serialize(new { code = 0, data = new { host = "mqtt-eu.mammotion.com", jwt = "mqtt-jwt", clientId = "client-1", username = "user-1" } }));
                case "/v1/mqtt/rpc/thing/service/invoke":
                    using (var doc = JsonDocument.Parse(r.Body))
                    {
                        _invokes.Add(doc.RootElement.Clone());
                    }
                    return (200, "{\"code\":0,\"msg\":\"success\",\"data\":{\"result\":\"ok\"}}");
            }
            return (404, "");
        }

        private NodeInstance CreateAccount(string password = "pw")
        {
            var node = CreateNodeInstance(DriverFactory.DriverGuid);
            node.SetProperty(MammotionDriverFactory.UsernameKey, "anna@example.com");
            node.SetProperty(MammotionDriverFactory.PasswordKey, password);
            node.SetProperty(MammotionDriverFactory.PollKey, 3600);
            return node;
        }

        private Dictionary<string, NodeInstance> AddMower(NodeInstance account)
        {
            var mower = CreateNodeInstance(MammotionDriverFactory.MowerTemplate);
            mower.SetProperty(MammotionDriverFactory.IotIdKey, IotId);
            account.InverseThis2ParentNodeInstanceNavigation.Add(mower);
            var values = new Dictionary<string, NodeInstance>();
            foreach (var dp in MammotionDataPoints.All)
            {
                var child = CreateNodeInstance(dp.Template);
                mower.InverseThis2ParentNodeInstanceNavigation.Add(child);
                values[dp.Key] = child;
            }
            return values;
        }

        private async Task<MammotionDriver> CreateMammotionAsync(NodeInstance account)
        {
            var driver = (MammotionDriver)await CreateDriver(account);
            driver.Http = _http;
            driver.AuthUrl = _server.Url;
            driver.MqttFactory = () => _mqtt;
            await driver.Init();
            return driver;
        }

        private object Value(NodeInstance node) => Dispatcher.GetValue(DispatchableType.NodeInstance, node.ObjId)?.Value;

        private static List<ProtoField> Content(JsonElement invoke) => ProtoReader.Read(Convert.FromBase64String(invoke.GetProperty("args").GetProperty("content").GetString()!));

        [Fact]
        public async Task Login_SendsSignedQueryWithBase64Password()
        {
            var account = CreateAccount();
            AddMower(account);
            var driver = await CreateMammotionAsync(account);

            await driver.ConnectAsync();

            var login = _server.Requests.First(r => r.Path == "/oauth2/token");
            Assert.Equal("POST", login.Method);
            Assert.Equal("?username=anna%40example.com&password=cHc%3D&client_id=GxebgSt8si6pKqR&grant_type=password&authType=0", login.Query);
            Assert.Equal("GxebgSt8si6pKqR", login.Headers["Ma-App-Key"]);
            Assert.Equal("okhttp/4.9.3", login.Headers["User-Agent"]);
            Assert.Equal("1", login.Headers["Client-Type"]);
            Assert.Matches("^\\d{13}_\\d{7}_1$", login.Headers["Client-Id"]);
            var ts = long.Parse(login.Headers["Ma-Timestamp"]);
            var q = QueryHelpers.ParseQuery(login.Query);
            var expected = MammotionCloud.Sign(ts, q.Select(p => new KeyValuePair<string, string>(p.Key, p.Value.ToString())).ToList());
            Assert.Equal(expected, login.Headers["Ma-Signature"]);
            Assert.Equal(5310, driver.Cloud.UserAccount);
            Assert.Equal(_server.Url.TrimEnd('/'), driver.Cloud.IotUrl);
        }

        [Fact]
        public async Task Login_FallsBackToThePlainPassword()
        {
            _acceptOnlyPlainPassword = true;
            var account = CreateAccount();
            AddMower(account);
            var driver = await CreateMammotionAsync(account);

            await driver.ConnectAsync();

            Assert.Equal(2, _server.Requests.Count(r => r.Path == "/oauth2/token"));
            Assert.True(_mqtt.IsConnected);
        }

        [Fact]
        public async Task Connect_SubscribesTheMowerTopicsAndRequestsReports()
        {
            var account = CreateAccount();
            AddMower(account);
            var driver = await CreateMammotionAsync(account);

            await driver.ConnectAsync();

            Assert.Equal(new MqttCredentials("mqtt-eu.mammotion.com", "client-1", "user-1", "mqtt-jwt"), _mqtt.Credentials);
            Assert.Equal(new[]
            {
                $"/sys/{Pk}/{Dn}/thing/event/+/post",
                $"/sys/proto/{Pk}/{Dn}/thing/event/+/post",
                $"/sys/{Pk}/{Dn}/app/down/thing/status"
            }, _mqtt.Topics);

            var invoke = Assert.Single(_invokes);
            Assert.Equal(IotId, invoke.GetProperty("iotId").GetString());
            Assert.Equal("device_protobuf_sync_service", invoke.GetProperty("identifier").GetString());
            var msg = Content(invoke);
            Assert.Equal(244, ProtoReader.Find(msg, 1)!.Value.Int32);
            Assert.Equal(5310, ProtoReader.Find(msg, 7)!.Value.Int32);
            Assert.NotNull(ProtoReader.Sub(ProtoReader.Sub(msg, 10), 38));
        }

        [Fact]
        public async Task ProtoMessage_DispatchesTheMowerState()
        {
            var account = CreateAccount();
            var values = AddMower(account);
            var driver = await CreateMammotionAsync(account);
            await driver.ConnectAsync();

            await _mqtt.OnMessage($"/sys/proto/{Pk}/{Dn}/thing/event/device_protobuf_msg_event/post", MammotionProtocolTests.ReportData(13, 0, 76, 45, 820, 33, 60, 55));

            Assert.Equal(true, Value(values["online"]));
            Assert.Equal(76, Value(values["battery"]));
            Assert.Equal(13, Value(values["device_state"]));
            Assert.Equal("working", Value(values["device_state_text"]));
            Assert.Equal(0, Value(values["charge_state"]));
            Assert.Equal(45, Value(values["mowing_progress"]));
            Assert.Equal(820, Value(values["mowing_area"]));
            Assert.Equal(33, Value(values["remaining_time"]));
            Assert.Equal(55, Value(values["knife_height"]));
        }

        [Fact]
        public async Task JsonEvent_WithBase64Content_IsDecoded()
        {
            var account = CreateAccount();
            var values = AddMower(account);
            var driver = await CreateMammotionAsync(account);
            await driver.ConnectAsync();

            var content = Convert.ToBase64String(MammotionProtocolTests.ReportData(15, 1, 98, 0, 0, 0, 0, 60));
            var json = $"{{\"method\":\"thing.events\",\"params\":{{\"identifier\":\"device_protobuf_msg_event\",\"value\":{{\"content\":\"{content}\"}}}}}}";
            await _mqtt.OnMessage($"/sys/{Pk}/{Dn}/thing/event/device_protobuf_msg_event/post", Encoding.UTF8.GetBytes(json));

            Assert.Equal(98, Value(values["battery"]));
            Assert.Equal("charging", Value(values["device_state_text"]));
            Assert.Equal(1, Value(values["charge_state"]));
        }

        [Fact]
        public async Task PropertyPost_Items_AreApplied()
        {
            var account = CreateAccount();
            var values = AddMower(account);
            var driver = await CreateMammotionAsync(account);
            await driver.ConnectAsync();

            var json = "{\"params\":{\"items\":{\"batteryPercentage\":{\"value\":55,\"time\":1},\"deviceState\":{\"value\":\"19\"},\"knifeHeight\":{\"value\":40}}}}";
            await _mqtt.OnMessage($"/sys/{Pk}/{Dn}/thing/event/property/post", Encoding.UTF8.GetBytes(json));

            Assert.Equal(55, Value(values["battery"]));
            Assert.Equal("paused", Value(values["device_state_text"]));
            Assert.Equal(40, Value(values["knife_height"]));
        }

        [Fact]
        public async Task Status_SetsOnline_AndBrokenMessagesAreIgnored()
        {
            var account = CreateAccount();
            var values = AddMower(account);
            var driver = await CreateMammotionAsync(account);
            await driver.ConnectAsync();

            await _mqtt.OnMessage($"/sys/{Pk}/{Dn}/app/down/thing/status", Encoding.UTF8.GetBytes("{\"action\":\"offline\"}"));
            Assert.Equal(false, Value(values["online"]));

            await _mqtt.OnMessage($"/sys/proto/{Pk}/{Dn}/thing/event/x/post", new byte[] { 0x52, 0x10, 0x01 });
            await _mqtt.OnMessage($"/sys/{Pk}/{Dn}/thing/event/x/post", Encoding.UTF8.GetBytes("{broken"));
            await _mqtt.OnMessage($"/sys/{Pk}/Other-Device/app/down/thing/status", Encoding.UTF8.GetBytes("{\"action\":\"online\"}"));

            await _mqtt.OnMessage($"/sys/{Pk}/{Dn}/app/down/thing/status", Encoding.UTF8.GetBytes("{\"action\":\"online\"}"));
            Assert.Equal(true, Value(values["online"]));
        }

        [Theory]
        [InlineData("start", 1)]
        [InlineData("pause", 2)]
        [InlineData("resume", 3)]
        [InlineData("stop", 4)]
        [InlineData("return_to_dock", 5)]
        public async Task Write_Action_InvokesTaskControl(string key, int action)
        {
            var account = CreateAccount();
            AddMower(account);
            var driver = await CreateMammotionAsync(account);

            await driver.Mowers.Single().Values.Single(v => v.DataPoint.Key == key).WriteDirectAsync(true);

            var msg = Content(_invokes.First());
            Assert.Equal(240, ProtoReader.Find(msg, 1)!.Value.Int32);
            var ctrl = ProtoReader.Sub(ProtoReader.Sub(msg, 11), 37);
            Assert.Equal(1, ProtoReader.Find(ctrl, 1)!.Value.Int32);
            Assert.Equal(action, ProtoReader.Find(ctrl, 2)!.Value.Int32);
            // followed by a report request
            Assert.Equal(244, ProtoReader.Find(Content(_invokes.Last()), 1)!.Value.Int32);
        }

        [Fact]
        public async Task Write_False_SendsNothing()
        {
            var account = CreateAccount();
            AddMower(account);
            var driver = await CreateMammotionAsync(account);

            await driver.Mowers.Single().Values.Single(v => v.DataPoint.Key == "start").WriteDirectAsync(false);

            Assert.Empty(_invokes);
        }

        [Fact]
        public async Task Poll_Disconnected_Reconnects_OtherwiseRequestsReports()
        {
            var account = CreateAccount();
            AddMower(account);
            var driver = await CreateMammotionAsync(account);
            await driver.ConnectAsync();

            await driver.PollAsync();
            Assert.Equal(1, _mqtt.Connects);
            Assert.Equal(2, _invokes.Count);

            _mqtt.IsConnected = false;
            await driver.PollAsync();
            Assert.Equal(2, _mqtt.Connects);
        }

        [Fact]
        public async Task Rejected_Token_LogsInAgain()
        {
            var account = CreateAccount();
            AddMower(account);
            var driver = await CreateMammotionAsync(account);
            await driver.ConnectAsync();

            var inner = _server.Handler;
            var rejected = false;
            _server.Handler = r =>
            {
                if (!rejected && r.Path == "/v1/mqtt/rpc/thing/service/invoke")
                {
                    rejected = true;
                    return (401, "{\"code\":401}");
                }
                return inner(r);
            };
            await driver.PollAsync(); // rejected, logged
            await driver.PollAsync();

            Assert.Equal(2, _logins);
            Assert.Equal(2, _invokes.Count);
        }

        [Fact]
        public async Task WrongPassword_DriverStartsAndReadsNothing()
        {
            var account = CreateAccount(password: "wrong");
            var values = AddMower(account);
            var driver = await CreateMammotionAsync(account);

            Assert.True(await driver.Start());
            await driver.Stop();

            Assert.Null(Value(values["battery"]));
            Assert.Equal(0, _mqtt.Connects);
        }

        [Fact]
        public async Task Scan_ListsMowersWithoutRtkAndConfigured()
        {
            var account = CreateAccount();
            AddMower(account);
            var driver = await CreateMammotionAsync(account);

            var found = Assert.Single(await driver.Scan());

            Assert.Equal("Yuka-XYZ", found.Name);
            Assert.Equal("iot-yuka", found.GetProperty(MammotionDriverFactory.IotIdKey).ValueString);
            Assert.Equal(MammotionDataPoints.All.Count, found.InverseThis2ParentNodeInstanceNavigation.Count);
        }

        [Fact]
        public async Task Scan_UsesTheNickName()
        {
            var account = CreateAccount();
            var driver = await CreateMammotionAsync(account);

            var found = await driver.Scan();

            Assert.Equal(new[] { "Front lawn", "Yuka-XYZ" }, found.Select(f => f.Name));
        }
    }
}
