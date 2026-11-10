using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using P3.Driver.Refoss.Driver;
using Xunit;

namespace P3.Driver.Refoss.Tests
{
    public class RefossDriverTests : DriverFactoryTestBase<RefossDriverFactory>, IAsyncLifetime
    {
        private const string Password = "s3cret";
        private const string Realm = "refoss-em16p-34298f1f4680";

        public enum Auth
        {
            None,
            Header,
            Body
        }

        public enum Shape
        {
            List,
            Wrapped,
            Map
        }

        private FakeHttpServer _server;
        private readonly HttpClient _http = new();

        private Auth _auth = Auth.None;
        private Shape _shape = Shape.List;
        private bool _refuseAll;
        private int _nonce = 1000;
        private bool _configFails;

        public async Task InitializeAsync()
        {
            _server = await FakeHttpServer.StartAsync();
            _server.Handler = Handle;
            _server.ResponseHeaders = (_, status) => status == 401 && _auth == Auth.Header
                ? new Dictionary<string, string> { ["WWW-Authenticate"] = $"Digest qop=\"auth\", realm=\"{Realm}\", nonce=\"{_nonce}\", algorithm=SHA-256" }
                : null;
        }

        public async Task DisposeAsync()
        {
            await _server.DisposeAsync();
            _http.Dispose();
        }

        // the style of the Refoss documentation, trailing commas included
        private static string Entry(int id, double current, double voltage, double power, double pf, double day, double week, double month, double dayRet = 0, double monthRet = 0) =>
            FormattableString.Invariant($"{{\"id\":{id},\"current\":{current},\"voltage\":{voltage},\"power\":{power},\"pf\":{pf},\"month_energy\":{month},\"month_ret_energy\":{monthRet},\"week_energy\":{week},\"week_ret_energy\":0,\"day_energy\":{day},\"day_ret_energy\":{dayRet},}}");

        private static readonly Dictionary<int, string> Status = new()
        {
            [1] = Entry(1, 1.2345, 230.04, 281.66, 0.981, 0.5123, 3.2, 12.34567),
            [7] = Entry(7, 6.5, 231.4, -1500.04, -0.99, 0.1, 0.4, 1.2, dayRet: 4.25, monthRet: 88.1),
            [13] = Entry(13, 0, 229.9, -0.0, 0, 0, 0, 0),
        };

        private (int, string) Handle(RecordedRequest r)
        {
            if (_auth != Auth.None && !Authorized(r))
            {
                return _auth == Auth.Body
                    ? (401, "{\"id\":0,\"error\":{\"code\":401,\"message\":\"{\\\"auth_type\\\": \\\"digest\\\", \\\"nonce\\\": " + _nonce + ", \\\"nc\\\": 1, \\\"realm\\\": \\\"" + Realm + "\\\", \\\"algorithm\\\": \\\"SHA-256\\\"}\"}}")
                    : (401, "");
            }

            var id = Regex.Match(r.Query, @"id=(\d+)") is { Success: true } m ? int.Parse(m.Groups[1].Value) : -1;
            switch (r.Path)
            {
                case "/rpc/Em.Status.Get":
                    if (id == RefossDriver.AllChannels)
                    {
                        if (_refuseAll)
                        {
                            return (400, "{\"error\":{\"code\":-103,\"message\":\"Invalid argument 'id'\"}}");
                        }
                        return (200, _shape switch
                        {
                            Shape.List => "{\"status\":[" + string.Join(",", Status.Values) + ",]}",
                            Shape.Wrapped => "{\"id\":0,\"src\":\"" + Realm + "\",\"result\":{\"status\":[" + string.Join(",", Status.Values) + "]}}",
                            _ => "{" + string.Join(",", Status.Select(s => $"\"em:{s.Key}\":{s.Value}")) + ",\"sys\":{\"uptime\":15951}}"
                        });
                    }
                    return Status.TryGetValue(id, out var single)
                        ? (200, "{\"status\":[" + single + "]}")
                        : (400, "{\"error\":{\"code\":-105,\"message\":\"Argument 'id', value " + id + " not found!\"}}");
                case "/rpc/Em.Config.Get":
                    if (_configFails)
                    {
                        return (404, "{\"error\":{\"code\":404,\"message\":\"No handler for Em.Config.Get\"}}");
                    }
                    return (200, "{\"config\":[" + string.Join(",", Enumerable.Range(1, 18).Select(i => $"{{\"id\":{i},\"name\":\"{(i == 1 ? "Heat pump" : i == 7 ? "PV" : "")}\",\"factor\":1}}")) + "]}");
            }
            return (404, "");
        }

        /// <summary>
        /// Checks the SHA-256 digest like the device: HA1 = sha256(admin:realm:password), HA2 = sha256(GET:uri)
        /// </summary>
        private bool Authorized(RecordedRequest r)
        {
            if (!r.Headers.TryGetValue("Authorization", out var header) || !header.StartsWith("Digest ", StringComparison.Ordinal))
            {
                return false;
            }
            var parts = Regex.Matches(header, @"(\w+)=(?:""([^""]*)""|([^,\s]*))").ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Success && m.Groups[2].Length > 0 ? m.Groups[2].Value : m.Groups[3].Value);
            if (parts["username"] != "admin" || parts["realm"] != Realm || parts["nonce"] != _nonce.ToString() || parts["uri"] != r.Path + r.Query)
            {
                return false;
            }
            var ha1 = RefossRpcClient.Sha256Hex($"admin:{Realm}:{Password}");
            var ha2 = RefossRpcClient.Sha256Hex($"GET:{parts["uri"]}");
            return parts["response"] == RefossRpcClient.Sha256Hex($"{ha1}:{parts["nonce"]}:{parts["nc"]}:{parts["cnonce"]}:auth:{ha2}");
        }

        private NodeInstance CreateDevice(string password = "", string host = null, params int[] channels)
        {
            var node = CreateNodeInstance(DriverFactory.DriverGuid);
            node.SetProperty(RefossDriverFactory.HostKey, host ?? _server.Url);
            node.SetProperty(RefossDriverFactory.PasswordKey, password);
            node.SetProperty(RefossDriverFactory.PollKey, 3600);
            foreach (var dp in RefossDataPoints.Device)
            {
                node.InverseThis2ParentNodeInstanceNavigation.Add(CreateNodeInstance(dp.Template));
            }
            foreach (var id in channels.Length == 0 ? new[] { 1, 7, 13 } : channels)
            {
                var channel = CreateNodeInstance(RefossDriverFactory.ChannelTemplate);
                channel.SetProperty(RefossDriverFactory.ChannelKey, id);
                foreach (var dp in RefossDataPoints.Channel)
                {
                    channel.InverseThis2ParentNodeInstanceNavigation.Add(CreateNodeInstance(dp.Template));
                }
                node.InverseThis2ParentNodeInstanceNavigation.Add(channel);
            }
            return node;
        }

        private async Task<RefossDriver> CreateDriverAsync(NodeInstance node)
        {
            var driver = (RefossDriver)await CreateDriver(node);
            driver.Http = _http;
            await driver.Init();
            return driver;
        }

        private static object Value(RefossDriver driver, int channel, string key) =>
            driver.Channels.Single(c => c.ChannelId == channel).Values.Single(v => v.DataPoint.Key == key).LastValue;

        private static int Channel(NodeInstance node) => Convert.ToInt32(node.GetPropertyValue(RefossDriverFactory.ChannelKey, null));

        private static object Connected(RefossDriver driver) => driver.Values.Single(v => v.DataPoint.Key == RefossDataPoints.ConnectedKey).LastValue;

        [Theory]
        [InlineData(Shape.List)]
        [InlineData(Shape.Wrapped)]
        [InlineData(Shape.Map)]
        public async Task Poll_ReadsAllChannelsInOneCall(Shape shape)
        {
            _shape = shape;
            var driver = await CreateDriverAsync(CreateDevice());

            await driver.PollAsync();

            var request = Assert.Single(_server.Requests);
            Assert.Equal("/rpc/Em.Status.Get", request.Path);
            Assert.Equal("?id=65535", request.Query);
            Assert.Equal(true, Connected(driver));

            Assert.Equal(281.7, Value(driver, 1, "power"));
            Assert.Equal(1.234, Value(driver, 1, "current"));
            Assert.Equal(230.0, Value(driver, 1, "voltage"));
            Assert.Equal(0.981, Value(driver, 1, "pf"));
            Assert.Equal(0.512, Value(driver, 1, "day_energy"));
            Assert.Equal(3.2, Value(driver, 1, "week_energy"));
            Assert.Equal(12.346, Value(driver, 1, "month_energy"));

            Assert.Equal(-1500.0, Value(driver, 7, "power"));
            Assert.Equal(4.25, Value(driver, 7, "day_ret_energy"));
            Assert.Equal(88.1, Value(driver, 7, "month_ret_energy"));

            Assert.Equal(0.0, Value(driver, 13, "power"));
            Assert.False(double.IsNegative((double)Value(driver, 13, "power")));
        }

        [Fact]
        public async Task Poll_DeviceRefusesAllChannels_ReadsThemOneByOne()
        {
            _refuseAll = true;
            var driver = await CreateDriverAsync(CreateDevice());

            await driver.PollAsync();
            await driver.PollAsync();

            Assert.True(driver.PerChannel);
            Assert.Equal(1, _server.Requests.Count(r => r.Query == "?id=65535"));
            Assert.Equal(2, _server.Requests.Count(r => r.Query == "?id=7"));
            Assert.Equal(true, Connected(driver));
            Assert.Equal(281.7, Value(driver, 1, "power"));
            Assert.Equal(-1500.0, Value(driver, 7, "power"));
        }

        [Fact]
        public async Task Poll_UnknownChannel_IsSkipped()
        {
            _refuseAll = true;
            var driver = await CreateDriverAsync(CreateDevice(channels: new[] { 1, 99 }));

            await driver.PollAsync();

            Assert.Equal(true, Connected(driver));
            Assert.Equal(281.7, Value(driver, 1, "power"));
            Assert.Null(Value(driver, 99, "power"));
        }

        [Theory]
        [InlineData(Auth.Header)]
        [InlineData(Auth.Body)]
        public async Task Auth_AnswersTheDigestChallenge_AndReusesIt(Auth auth)
        {
            _auth = auth;
            var driver = await CreateDriverAsync(CreateDevice(Password));

            await driver.PollAsync();
            await driver.PollAsync();

            Assert.Equal(true, Connected(driver));
            Assert.Equal(281.7, Value(driver, 1, "power"));
            // one challenge, then two authorized requests with a growing nonce count
            Assert.Equal(3, _server.Requests.Count);
            var counts = _server.Requests.Skip(1).Select(r => Regex.Match(r.Headers["Authorization"], "nc=([0-9a-f]{8})").Groups[1].Value).ToList();
            Assert.Equal(new[] { "00000001", "00000002" }, counts);
        }

        [Fact]
        public async Task Auth_ExpiredNonce_GetsANewOne()
        {
            _auth = Auth.Header;
            var driver = await CreateDriverAsync(CreateDevice(Password));
            await driver.PollAsync();

            _nonce++;
            await driver.PollAsync();

            Assert.Equal(true, Connected(driver));
            Assert.Equal(4, _server.Requests.Count);
        }

        [Fact]
        public async Task Auth_WrongPassword_IsNotConnected()
        {
            _auth = Auth.Header;
            var driver = await CreateDriverAsync(CreateDevice("wrong"));

            var ex = await Assert.ThrowsAsync<RefossRpcException>(() => driver.PollAsync());

            Assert.Equal(401, ex.Code);
            Assert.Contains("rejected the password", ex.Message);
            Assert.Equal(false, Connected(driver));
            Assert.Equal(2, _server.Requests.Count);
        }

        [Fact]
        public async Task Auth_NoPassword_TellsToSetIt()
        {
            _auth = Auth.Header;
            var driver = await CreateDriverAsync(CreateDevice());

            var ex = await Assert.ThrowsAsync<RefossRpcException>(() => driver.PollAsync());

            Assert.Contains("set the password", ex.Message);
            Assert.Equal(false, Connected(driver));
        }

        [Fact]
        public async Task DeviceOffline_IsNotConnected()
        {
            var driver = await CreateDriverAsync(CreateDevice(host: "127.0.0.1:1"));

            await Assert.ThrowsAsync<HttpRequestException>(() => driver.PollAsync());

            Assert.Equal(false, Connected(driver));
        }

        [Fact]
        public async Task Scan_ListsTheChannelsThatAreNotConfigured_WithTheAppNames()
        {
            var driver = await CreateDriverAsync(CreateDevice(channels: new[] { 13 }));

            var found = await driver.Scan();

            Assert.Equal(17, found.Count);
            Assert.DoesNotContain(found, n => Channel(n) == 13);
            Assert.Equal("A1 Heat pump", found[0].Name);
            Assert.Equal(1, Channel(found[0]));
            Assert.Equal("A2", found[1].Name);
            Assert.Equal("B1 PV", found.Single(n => Channel(n) == 7).Name);
            Assert.Equal("C6", found[^1].Name);
            Assert.All(found, n => Assert.Equal(RefossDataPoints.Channel.Count, n.InverseThis2ParentNodeInstanceNavigation.Count));
        }

        [Fact]
        public async Task Scan_WithoutConfig_UsesTheStatusChannels()
        {
            _configFails = true;
            var driver = await CreateDriverAsync(CreateDevice(channels: new[] { 1 }));

            var found = await driver.Scan();

            Assert.Equal(new[] { "B1", "C1" }, found.Select(n => n.Name));
        }

        [Theory]
        [InlineData(1, "A1")]
        [InlineData(6, "A6")]
        [InlineData(7, "B1")]
        [InlineData(12, "B6")]
        [InlineData(13, "C1")]
        [InlineData(18, "C6")]
        [InlineData(65, "Channel 65")]
        public void ChannelName_FollowsTheCtLabels(int id, string name)
        {
            Assert.Equal(name, RefossDataPoints.ChannelName(id));
        }

        [Theory]
        [InlineData("192.168.1.60", "http://192.168.1.60")]
        [InlineData(" http://em16p.local/ ", "http://em16p.local")]
        [InlineData("", "")]
        public void NormalizeUrl_AddsHttp(string host, string url)
        {
            Assert.Equal(url, RefossRpcClient.NormalizeUrl(host));
        }

        [Fact]
        public void Query_EncodesTheParameters()
        {
            Assert.Equal("?id=65535&name=A%201", RefossRpcClient.Query(new Dictionary<string, object> { ["id"] = 65535, ["name"] = "A 1" }));
        }

        [Fact]
        public void Unwrap_ThrowsTheRpcError()
        {
            var ex = Assert.Throws<RefossRpcException>(() => RefossRpcClient.Unwrap("Em.Status.Get", "{\"error\":{\"code\":-103,\"message\":\"Invalid argument\"}}"));
            Assert.Equal(-103, ex.Code);
        }
    }
}
