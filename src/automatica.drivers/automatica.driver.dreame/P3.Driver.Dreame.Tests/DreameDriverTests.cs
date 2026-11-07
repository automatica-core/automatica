using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Microsoft.AspNetCore.WebUtilities;
using P3.Driver.Dreame.Driver;
using Xunit;

namespace P3.Driver.Dreame.Tests
{
    public class DreameSigningTests
    {
        private const long Ms = 1762520000123;

        // expected values computed with the Python implementation (Tasshack/dreame-vacuum protocol.py _spliced/_signed)
        [Fact]
        public void Splice_Command_MatchesPython()
        {
            var cmd = new JsonObject
            {
                ["did"] = "123456",
                ["id"] = 7,
                ["data"] = new JsonObject { ["did"] = "123456", ["id"] = 7, ["method"] = "get_properties", ["params"] = new JsonArray { new JsonObject { ["did"] = "2.1", ["siid"] = 2, ["piid"] = 1 } } }
            };

            Assert.Equal("data=[did=\"123456\"&id=7&method=\"get_properties\"]&did=123456&id=7", DreameCloud.Splice(cmd, true));
            var signed = DreameCloud.Sign(cmd, Ms);
            Assert.Equal("7f0afa5102e13ab2b6d741d83a9bb306", signed["sign"]!.GetValue<string>());
            Assert.Equal(Ms, signed["timestamp"]!.GetValue<long>());
            // the original is not modified
            Assert.False(cmd.ContainsKey("sign"));
        }

        [Fact]
        public void Splice_TopLevelListsBoolsAndNulls_MatchesPython()
        {
            var p = new JsonObject
            {
                ["did"] = "123456",
                ["keys"] = "2.1,3.1",
                ["flag"] = true,
                ["list"] = new JsonArray { new JsonObject { ["b"] = 2, ["a"] = "ä" } },
                ["n"] = null
            };

            Assert.Equal("did=123456&flag=true&keys=2.1,3.1&list=[{\"a\":\"ä\",\"b\":2}]&n=null", DreameCloud.Splice(p, true));
            Assert.Equal("218ef8fcd249d55b85ef68c099bc93dd", DreameCloud.Sign(p, Ms)["sign"]!.GetValue<string>());
        }

        [Fact]
        public void HashPassword_Md5WithSalt()
        {
            Assert.Equal("ab51518dc498dcac64b000f288be8ea6", DreameCloud.HashPassword("secret"));
        }

        [Fact]
        public void RegionHeader_IsAesEcbOfRegionLangCountry()
        {
            var header = DreameCloud.RegionHeader("eu", "en", "DE");

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(DreameCloud.SignKey);
            Assert.Equal("eu|en|DE", Encoding.UTF8.GetString(aes.DecryptEcb(Convert.FromBase64String(header), PaddingMode.PKCS7)));
        }

        [Theory]
        [InlineData("10000.mt.eu.iot.dreame.tech:19973", "dreame-iot-com-10000/device/sendCommand")]
        [InlineData(null, "dreame-iot-com/device/sendCommand")]
        [InlineData("", "dreame-iot-com/device/sendCommand")]
        public void CommandPath_UsesTheFirstLabelOfTheBindDomain(string bindDomain, string expected)
        {
            Assert.Equal(expected, DreameCloud.CommandPath(bindDomain));
        }

        [Fact]
        public void RegionUrl()
        {
            Assert.Equal("https://eu.iot.dreame.tech:13267", DreameCloud.RegionUrl(null));
            Assert.Equal("https://us.iot.dreame.tech:13267", DreameCloud.RegionUrl(" US "));
        }
    }

    public class DreameDriverTests : DriverFactoryTestBase<DreameDriverFactory>, IAsyncLifetime
    {
        private const string Did = "-111222333";
        private const string BindDomain = "10000.mt.eu.iot.dreame.tech:19973";

        private FakeHttpServer _server;
        private readonly HttpClient _http = new();
        private int _logins;
        private readonly List<JsonElement> _commands = new();
        private readonly Dictionary<string, object> _props = new()
        {
            ["2.1"] = 6, ["2.2"] = 0, ["3.1"] = 87, ["3.2"] = 1, ["4.1"] = 6, ["4.2"] = 42, ["4.3"] = 35, ["4.4"] = 2, ["4.5"] = 3
        };

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

        private (int, string) Handle(RecordedRequest r)
        {
            if (r.Path == "/dreame-auth/oauth/token")
            {
                var form = QueryHelpers.ParseQuery("?" + r.Body);
                var ok = form["grant_type"] == "password"
                    ? form["username"] == "anna@example.com" && form["password"] == DreameCloud.HashPassword("pw")
                    : form["refresh_token"] == $"refresh-{_logins}";
                if (!ok)
                {
                    return (401, "{\"code\":401,\"msg\":\"bad credentials\"}");
                }
                _logins++;
                return (200, JsonSerializer.Serialize(new { access_token = $"token-{_logins}", refresh_token = $"refresh-{_logins}", expires_in = 7200, tenant_id = "000000", uid = "U1", region = "eu", lang = "en", country = "DE" }));
            }

            if (!r.Headers.TryGetValue("dreame-auth", out var auth) || auth != $"token-{_logins}")
            {
                return (401, "{\"code\":401}");
            }

            using var body = JsonDocument.Parse(string.IsNullOrEmpty(r.Body) ? "{}" : r.Body);
            switch (r.Path)
            {
                case "/dreame-user-iot/iotuserbind/device/listV2":
                    return (200, JsonSerializer.Serialize(new
                    {
                        code = 0,
                        success = true,
                        data = new
                        {
                            page = new
                            {
                                records = new object[]
                                {
                                    new { did = Did, model = "dreame.vacuum.r2228o", customName = "Kitchen robot", mac = "AA", deviceInfo = new { displayName = "L10s Ultra" } },
                                    new { did = "999", model = "dreame.vacuum.p2009", customName = "", mac = "BB", deviceInfo = new { displayName = "D9" } },
                                    new { did = "555", model = "dreame.hold.w2422", customName = "Wet & dry", mac = "CC", deviceInfo = new { displayName = "H12" } },
                                }
                            }
                        }
                    }));
                case "/dreame-user-iot/iotuserbind/device/info":
                    return (200, JsonSerializer.Serialize(new { code = 0, data = new { did = body.RootElement.GetProperty("did").GetString(), bindDomain = BindDomain, masterUid = "U1", model = "dreame.vacuum.r2228o" } }));
                case "/dreame-iot-com-10000/device/sendCommand":
                    _commands.Add(body.RootElement.Clone());
                    var data = body.RootElement.GetProperty("data");
                    var method = data.GetProperty("method").GetString();
                    object result = method switch
                    {
                        "get_properties" => data.GetProperty("params").EnumerateArray().Select(p =>
                        {
                            var key = $"{p.GetProperty("siid").GetInt32()}.{p.GetProperty("piid").GetInt32()}";
                            return _props.TryGetValue(key, out var v)
                                ? (object)new { did = p.GetProperty("did").GetString(), siid = p.GetProperty("siid").GetInt32(), piid = p.GetProperty("piid").GetInt32(), code = 0, value = v }
                                : new { did = p.GetProperty("did").GetString(), siid = p.GetProperty("siid").GetInt32(), piid = p.GetProperty("piid").GetInt32(), code = -4004 };
                        }).ToArray(),
                        "set_properties" => new object[] { new { code = 0 } },
                        "action" => new { code = 0, @out = Array.Empty<object>() },
                        _ => null
                    };
                    return (200, JsonSerializer.Serialize(new { code = 0, success = true, data = new { id = data.GetProperty("id").GetInt32(), result } }));
            }
            return (404, "");
        }

        private NodeInstance CreateAccount(string password = "pw")
        {
            var node = CreateNodeInstance(DriverFactory.DriverGuid);
            node.SetProperty(DreameDriverFactory.UsernameKey, "anna@example.com");
            node.SetProperty(DreameDriverFactory.PasswordKey, password);
            node.SetProperty(DreameDriverFactory.PollKey, 3600);
            return node;
        }

        private Dictionary<string, NodeInstance> AddVacuum(NodeInstance account)
        {
            var vacuum = CreateNodeInstance(DreameDriverFactory.VacuumTemplate);
            vacuum.SetProperty(DreameDriverFactory.DidKey, Did);
            account.InverseThis2ParentNodeInstanceNavigation.Add(vacuum);
            var values = new Dictionary<string, NodeInstance>();
            foreach (var dp in DreameDataPoints.All)
            {
                var child = CreateNodeInstance(dp.Template);
                vacuum.InverseThis2ParentNodeInstanceNavigation.Add(child);
                values[dp.Key] = child;
            }
            return values;
        }

        private async Task<DreameDriver> CreateDreameAsync(NodeInstance account)
        {
            var driver = (DreameDriver)await CreateDriver(account);
            driver.Http = _http;
            driver.BaseUrlOverride = _server.Url;
            await driver.Init();
            return driver;
        }

        private object Value(NodeInstance node) => Dispatcher.GetValue(DispatchableType.NodeInstance, node.ObjId)?.Value;

        private Task WriteAsync(DreameDriver driver, string key, object value) => driver.Vacuums.Single().Values.Single(v => v.DataPoint.Key == key).WriteDirectAsync(value);

        [Fact]
        public async Task Login_SendsTheFormWithSaltedPasswordAndAppHeaders()
        {
            var account = CreateAccount();
            AddVacuum(account);
            var driver = await CreateDreameAsync(account);

            await driver.PollAsync();

            var login = _server.Requests.First(r => r.Path == "/dreame-auth/oauth/token");
            Assert.Equal("POST", login.Method);
            Assert.StartsWith("application/x-www-form-urlencoded", login.Headers["Content-Type"]);
            Assert.Equal($"grant_type=password&scope=all&platform=ANDROID&type=account&username=anna%40example.com&password={DreameCloud.HashPassword("pw")}", login.Body);
            Assert.Equal("Basic ZHJlYW1lX2FwcHYxOkFQXmR2QHpAU1FZVnhOODg=", login.Headers["authorization"]);
            Assert.Equal("000000", login.Headers["tenant-id"]);
            Assert.StartsWith("cv=a_102060300;canvasHash=", login.Headers["dreame-meta"]);
            Assert.False(login.Headers.ContainsKey("dreame-rlc"));
        }

        [Fact]
        public async Task Requests_AfterLogin_AreSignedAndCarryTokenAndRegionHeader()
        {
            var account = CreateAccount();
            AddVacuum(account);
            var driver = await CreateDreameAsync(account);

            await driver.PollAsync();

            var info = _server.Requests.First(r => r.Path == "/dreame-user-iot/iotuserbind/device/info");
            Assert.Equal("token-1", info.Headers["dreame-auth"]);
            Assert.Equal(DreameCloud.RegionHeader("eu", "en", "DE"), info.Headers["dreame-rlc"]);
            var body = JsonNode.Parse(info.Body)!.AsObject();
            var ms = body["timestamp"]!.GetValue<long>();
            var sign = body["sign"]!.GetValue<string>();
            body.Remove("timestamp");
            body.Remove("sign");
            Assert.Equal(DreameCloud.Sign(body, ms)["sign"]!.GetValue<string>(), sign);
        }

        [Fact]
        public async Task Poll_ReadsAllPropertiesInOneCommandAndDispatches()
        {
            var account = CreateAccount();
            var values = AddVacuum(account);
            var driver = await CreateDreameAsync(account);

            await driver.PollAsync();

            var command = Assert.Single(_commands);
            Assert.Equal(Did, command.GetProperty("did").GetString());
            Assert.Equal("get_properties", command.GetProperty("data").GetProperty("method").GetString());
            Assert.Equal(9, command.GetProperty("data").GetProperty("params").GetArrayLength());
            Assert.Equal(6, Value(values["state"]));
            Assert.Equal("charging", Value(values["state_text"]));
            Assert.Equal(0, Value(values["error"]));
            Assert.Equal(87, Value(values["battery"]));
            Assert.Equal(1, Value(values["charging_status"]));
            Assert.Equal(6, Value(values["status"]));
            Assert.Equal(42, Value(values["cleaning_time"]));
            Assert.Equal(35, Value(values["cleaned_area"]));
            Assert.Equal(2, Value(values["suction_level"]));
            Assert.Equal(3, Value(values["water_volume"]));
        }

        [Fact]
        public async Task Poll_UnsupportedPropertyIsSkipped()
        {
            _props.Remove("4.5");
            var account = CreateAccount();
            var values = AddVacuum(account);
            var driver = await CreateDreameAsync(account);

            await driver.PollAsync();

            Assert.Null(Value(values["water_volume"]));
            Assert.Equal(87, Value(values["battery"]));
        }

        [Theory]
        [InlineData("start", 2, 1)]
        [InlineData("pause", 2, 2)]
        [InlineData("stop", 4, 2)]
        [InlineData("return_to_dock", 3, 1)]
        [InlineData("locate", 7, 1)]
        public async Task Write_Action_SendsSiidAiid(string key, int siid, int aiid)
        {
            var account = CreateAccount();
            AddVacuum(account);
            var driver = await CreateDreameAsync(account);

            await WriteAsync(driver, key, true);

            var action = _commands.First().GetProperty("data");
            Assert.Equal("action", action.GetProperty("method").GetString());
            var p = action.GetProperty("params");
            Assert.Equal(Did, p.GetProperty("did").GetString());
            Assert.Equal(siid, p.GetProperty("siid").GetInt32());
            Assert.Equal(aiid, p.GetProperty("aiid").GetInt32());
            Assert.Equal(0, p.GetProperty("in").GetArrayLength());
            // and reads the state again
            Assert.Equal("get_properties", _commands.Last().GetProperty("data").GetProperty("method").GetString());
        }

        [Fact]
        public async Task Write_ActionFalse_SendsNothing()
        {
            var account = CreateAccount();
            AddVacuum(account);
            var driver = await CreateDreameAsync(account);

            await WriteAsync(driver, "start", false);

            Assert.Empty(_commands);
        }

        [Fact]
        public async Task Write_SuctionLevel_SetsProperty()
        {
            var account = CreateAccount();
            AddVacuum(account);
            var driver = await CreateDreameAsync(account);

            await WriteAsync(driver, "suction_level", 3L);

            var set = _commands.First().GetProperty("data");
            Assert.Equal("set_properties", set.GetProperty("method").GetString());
            var p = set.GetProperty("params")[0];
            Assert.Equal(4, p.GetProperty("siid").GetInt32());
            Assert.Equal(4, p.GetProperty("piid").GetInt32());
            Assert.Equal(3, p.GetProperty("value").GetInt32());
        }

        [Fact]
        public async Task Session_Rejected_LogsInAgainWithTheRefreshToken()
        {
            var account = CreateAccount();
            var values = AddVacuum(account);
            var driver = await CreateDreameAsync(account);
            await driver.PollAsync();

            var before = _server.Requests.Count(r => r.Path == "/dreame-auth/oauth/token");
            _props["3.1"] = 50;
            var inner = _server.Handler;
            var rejectedOnce = false;
            _server.Handler = r =>
            {
                if (!rejectedOnce && r.Path.StartsWith("/dreame-iot-com"))
                {
                    rejectedOnce = true;
                    return (401, "{\"code\":401}");
                }
                return inner(r);
            };
            await driver.PollAsync();

            var logins = _server.Requests.Where(r => r.Path == "/dreame-auth/oauth/token").Skip(before).ToList();
            var relogin = Assert.Single(logins);
            Assert.Contains("grant_type=refresh_token", relogin.Body);
            Assert.Equal(50, Value(values["battery"]));
        }

        [Fact]
        public async Task Login_WrongPassword_DriverStartsAndReadsNothing()
        {
            var account = CreateAccount(password: "wrong");
            var values = AddVacuum(account);
            var driver = await CreateDreameAsync(account);

            Assert.True(await driver.Start());
            await driver.Stop();

            Assert.Null(Value(values["battery"]));
        }

        [Fact]
        public async Task Scan_ListsVacuumsWithNamesAndDataPoints()
        {
            var account = CreateAccount();
            var driver = await CreateDreameAsync(account);

            var found = await driver.Scan();

            Assert.Equal(2, found.Count); // the wet & dry vacuum cleaner is not a robot
            Assert.Equal("Kitchen robot", found[0].Name);
            Assert.Equal(Did, found[0].GetProperty(DreameDriverFactory.DidKey).ValueString);
            Assert.Equal("D9", found[1].Name);
            Assert.Equal(DreameDataPoints.All.Count, found[0].InverseThis2ParentNodeInstanceNavigation.Count);
        }

        [Fact]
        public async Task Scan_SkipsConfiguredVacuums()
        {
            var account = CreateAccount();
            AddVacuum(account);
            var driver = await CreateDreameAsync(account);

            var found = await driver.Scan();

            Assert.Equal("999", Assert.Single(found).GetProperty(DreameDriverFactory.DidKey).ValueString);
        }
    }
}
