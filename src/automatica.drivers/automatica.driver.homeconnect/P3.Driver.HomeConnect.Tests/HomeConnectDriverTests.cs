using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Microsoft.AspNetCore.WebUtilities;
using P3.Driver.HomeConnect.Driver;
using Xunit;

namespace P3.Driver.HomeConnect.Tests
{
    public class HomeConnectDriverTests : DriverFactoryTestBase<HomeConnectDriverFactory>, IAsyncLifetime
    {
        private const string HaId = "BOSCH-SMV68TX06E-68A40E000001";
        private const string FridgeId = "SIEMENS-KI86SHD40-68A40E000002";
        private const string ClientId = "client-123";
        private const string ClientSecret = "secret-456";
        private const string RefreshToken = "refresh-1";

        private FakeHttpServer _server;
        private readonly HttpClient _http = new();
        private readonly HttpClient _streamHttp = new() { Timeout = System.Threading.Timeout.InfiniteTimeSpan };

        private int _tokenCounter;
        private bool _dishwasherConnected = true;
        private string _activeProgram = "Dishcare.Dishwasher.Program.Eco50";
        private int _devicePolls;
        private readonly List<string> _writes = new();
        private Channel<string> _events = Channel.CreateUnbounded<string>();

        public async Task InitializeAsync()
        {
            _server = await FakeHttpServer.StartAsync();
            _server.Handler = Handle;
            _server.StreamHandler = r => r.Path == "/api/homeappliances/events" && r.Headers.TryGetValue("Authorization", out var a) && a.StartsWith("Bearer access-") ? _events.Reader : null;
        }

        public async Task DisposeAsync()
        {
            _events.Writer.TryComplete();
            await _server.DisposeAsync();
            _http.Dispose();
            _streamHttp.Dispose();
        }

        private static string Items(string list, params object[] items) => JsonSerializer.Serialize(new Dictionary<string, object> { ["data"] = new Dictionary<string, object> { [list] = items } });

        private (int, string) Handle(RecordedRequest request)
        {
            if (request.Path == "/security/oauth/token")
            {
                var form = QueryHelpers.ParseQuery("?" + request.Body);
                if (form["grant_type"] == "urn:ietf:params:oauth:grant-type:device_code")
                {
                    if (form["device_code"] != "dev-code" || form["client_secret"] != ClientSecret)
                    {
                        return (400, "{\"error\":\"invalid_grant\"}");
                    }
                    if (++_devicePolls < 2)
                    {
                        return (400, "{\"error\":\"authorization_pending\"}");
                    }
                }
                else if (form["refresh_token"] != $"refresh-{_tokenCounter + 1}" || form["client_secret"] != ClientSecret)
                {
                    return (400, "{\"error\":\"invalid_grant\",\"error_description\":\"refresh token expired\"}");
                }
                _tokenCounter++;
                return (200, JsonSerializer.Serialize(new { access_token = $"access-{_tokenCounter}", refresh_token = $"refresh-{_tokenCounter + 1}", expires_in = 86400, token_type = "Bearer", scope = HomeConnectDriver.Scopes }));
            }
            if (request.Path == "/security/oauth/device_authorization")
            {
                return (200, JsonSerializer.Serialize(new { device_code = "dev-code", user_code = "ABCD-1234", verification_uri = "https://verify.home-connect.com", verification_uri_complete = "https://verify.home-connect.com?user_code=ABCD-1234", expires_in = 300, interval = 5 }));
            }

            if (!request.Headers.TryGetValue("Authorization", out var auth) || !auth.StartsWith("Bearer access-"))
            {
                return (401, "{\"error\":{\"key\":\"invalid_token\"}}");
            }
            Assert.Contains(HomeConnectApi.MediaType, request.Headers["Accept"]);

            var dish = $"/api/homeappliances/{HaId}";
            if (request.Method == "GET")
            {
                switch (request.Path)
                {
                    case "/api/homeappliances":
                        return (200, JsonSerializer.Serialize(new
                        {
                            data = new
                            {
                                homeappliances = new object[]
                                {
                                    new { haId = HaId, name = "Dishwasher", brand = "Bosch", type = "Dishwasher", vib = "SMV68TX06E", connected = _dishwasherConnected },
                                    new { haId = FridgeId, name = "Fridge", brand = "Siemens", type = "FridgeFreezer", vib = "KI86SHD40", connected = true }
                                }
                            }
                        }));
                    case var p when p == dish + "/status":
                        return (200, Items("status",
                            new { key = "BSH.Common.Status.OperationState", value = "BSH.Common.EnumType.OperationState.Run" },
                            new { key = "BSH.Common.Status.DoorState", value = "BSH.Common.EnumType.DoorState.Closed" },
                            new { key = "BSH.Common.Status.RemoteControlStartAllowed", value = true },
                            new { key = "BSH.Common.Status.RemoteControlActive", value = true },
                            new { key = "BSH.Common.Status.LocalControlActive", value = false }));
                    case var p when p == dish + "/settings":
                        return (200, Items("settings",
                            new { key = "BSH.Common.Setting.PowerState", value = "BSH.Common.EnumType.PowerState.On" },
                            new { key = "BSH.Common.Setting.ChildLock", value = false },
                            new { key = "Dishcare.Dishwasher.Setting.SpeedOnDemand", value = 3 }));
                    case var p when p == dish + "/programs/active":
                        return _activeProgram == null
                            ? (404, "{\"error\":{\"key\":\"SDK.Error.NoProgramActive\",\"description\":\"There is no program active\"}}")
                            : (200, JsonSerializer.Serialize(new
                            {
                                data = new
                                {
                                    key = _activeProgram,
                                    options = new object[]
                                    {
                                        new { key = "BSH.Common.Option.RemainingProgramTime", value = 5400, unit = "seconds" },
                                        new { key = "BSH.Common.Option.ProgramProgress", value = 20, unit = "%" },
                                        new { key = "Dishcare.Dishwasher.Option.HalfLoad", value = true }
                                    }
                                }
                            }));
                    case var p when p == dish + "/programs/selected":
                        return (200, JsonSerializer.Serialize(new
                        {
                            data = new
                            {
                                key = "Dishcare.Dishwasher.Program.Auto2",
                                options = new object[]
                                {
                                    new { key = "BSH.Common.Option.StartInRelative", value = 0, unit = "seconds" },
                                    new { key = "BSH.Common.Option.RemainingProgramTime", value = 7200, unit = "seconds" },
                                    new { key = "Dishcare.Dishwasher.Option.HalfLoad", value = true }
                                }
                            }
                        }));
                }
            }
            if (request.Path.StartsWith(dish + "/") && request.Method is "PUT" or "DELETE")
            {
                _writes.Add($"{request.Method} {request.Path[dish.Length..]} {request.Body}".TrimEnd());
                return (204, "");
            }
            return (404, "{\"error\":{\"key\":\"SDK.Error.HomeAppliance.Connection.Initialization.Failed\"}}");
        }

        private NodeInstance CreateAccount(string refreshToken = RefreshToken)
        {
            var node = CreateNodeInstance(DriverFactory.DriverGuid);
            node.SetProperty(HomeConnectDriverFactory.ApiUrlKey, _server.Url);
            node.SetProperty(HomeConnectDriverFactory.ClientIdKey, ClientId);
            node.SetProperty(HomeConnectDriverFactory.ClientSecretKey, ClientSecret);
            node.SetProperty(HomeConnectDriverFactory.RefreshTokenKey, refreshToken);
            node.SetProperty(HomeConnectDriverFactory.PollKey, 3600);
            foreach (var key in new[] { HomeConnectDriverFactory.LoginStatusKey, HomeConnectDriverFactory.StreamConnectedKey })
            {
                var child = CreateNodeInstance(key == HomeConnectDriverFactory.LoginStatusKey ? LoginTemplate : StreamTemplate);
                node.InverseThis2ParentNodeInstanceNavigation.Add(child);
                _account[key] = child;
            }
            return node;
        }

        private readonly Dictionary<string, NodeInstance> _account = new();
        private static readonly Guid LoginTemplate = new("1d9e0121-7e49-426f-afc6-4a7685ff5148");
        private static readonly Guid StreamTemplate = new("43991f25-830d-4216-9713-0f81c714fd3b");

        private Dictionary<string, NodeInstance> AddAppliance(NodeInstance account, string haId = HaId, params (string Template, string Key)[] custom)
        {
            var appliance = CreateNodeInstance(HomeConnectDriverFactory.ApplianceTemplate);
            appliance.SetProperty(HomeConnectDriverFactory.HaIdKey, haId);
            account.InverseThis2ParentNodeInstanceNavigation.Add(appliance);
            var values = new Dictionary<string, NodeInstance>();
            foreach (var dp in HomeConnectDataPoints.Defaults)
            {
                var child = CreateNodeInstance(dp.Template);
                appliance.InverseThis2ParentNodeInstanceNavigation.Add(child);
                values[dp.Key] = child;
            }
            foreach (var (template, key) in custom)
            {
                var child = CreateNodeInstance(HomeConnectDataPoints.All.Single(d => d.Key == template).Template);
                child.SetProperty(HomeConnectDriverFactory.CustomKeyKey, key);
                appliance.InverseThis2ParentNodeInstanceNavigation.Add(child);
                values[key] = child;
            }
            return values;
        }

        private async Task<HomeConnectDriver> CreateHomeConnectAsync(NodeInstance account)
        {
            var driver = (HomeConnectDriver)await CreateDriver(account);
            driver.Http = _http;
            driver.StreamHttp = _streamHttp;
            driver.DeviceLoginInterval = TimeSpan.FromMilliseconds(10);
            driver.MinReconnectDelay = TimeSpan.FromMilliseconds(50);
            await driver.Init();
            return driver;
        }

        private object Value(NodeInstance node) => Dispatcher.GetValue(DispatchableType.NodeInstance, node.ObjId)?.Value;

        private Task WriteAsync(HomeConnectDriver driver, string key, object value, string customKey = null)
        {
            var node = driver.Appliances.Single(a => a.HaId == HaId).Values.Single(v => v.Key == key && (customKey == null || v.Parameter == customKey));
            return node.WriteDirectAsync(value);
        }

        private Task SendEventAsync(string name, object data, string id = HaId)
        {
            return _events.Writer.WriteAsync($"event: {name}\ndata: {(data == null ? "" : JsonSerializer.Serialize(data))}\nid: {id}\n\n").AsTask();
        }

        [Fact]
        public async Task Refresh_ReadsStatusSettingsAndPrograms()
        {
            var account = CreateAccount();
            var values = AddAppliance(account, HaId, (HomeConnectDataPoints.CustomNumberKey, "Dishcare.Dishwasher.Setting.SpeedOnDemand"),
                (HomeConnectDataPoints.CustomSwitchKey, "Dishcare.Dishwasher.Option.HalfLoad"), (HomeConnectDataPoints.CustomTextKey, "BSH.Common.Status.DoorState"));
            var driver = await CreateHomeConnectAsync(account);

            await driver.RefreshAllAsync();

            Assert.Equal(true, Value(values["connected"]));
            Assert.Equal("Run", Value(values["operation_state"]));
            Assert.Equal("Closed", Value(values["door_state"]));
            Assert.Equal(true, Value(values["remote_start_allowed"]));
            Assert.Equal(true, Value(values["remote_control_active"]));
            Assert.Equal(false, Value(values["local_control_active"]));
            Assert.Equal(true, Value(values["power"]));
            Assert.Equal(true, Value(values["running"]));
            Assert.Equal(false, Value(values["paused"]));
            Assert.Equal("Dishcare.Dishwasher.Program.Eco50", Value(values["active_program"]));
            Assert.Equal("Dishcare.Dishwasher.Program.Auto2", Value(values["selected_program"]));
            Assert.Equal(5400, Value(values["remaining_time"]));
            Assert.Equal(20, Value(values["progress"]));
            Assert.Equal(3d, Value(values["Dishcare.Dishwasher.Setting.SpeedOnDemand"]));
            Assert.Equal(true, Value(values["Dishcare.Dishwasher.Option.HalfLoad"]));
            Assert.Equal("BSH.Common.EnumType.DoorState.Closed", Value(values["BSH.Common.Status.DoorState"]));
        }

        [Fact]
        public async Task Refresh_NoActiveProgram_ClearsProgramValues()
        {
            _activeProgram = null;
            var account = CreateAccount();
            var values = AddAppliance(account);
            var driver = await CreateHomeConnectAsync(account);

            await driver.RefreshAllAsync();

            Assert.Equal("", Value(values["active_program"]));
            Assert.Equal(0, Value(values["remaining_time"]));
        }

        [Fact]
        public async Task Refresh_OfflineAppliance_OnlyReadsTheList()
        {
            _dishwasherConnected = false;
            var account = CreateAccount();
            var values = AddAppliance(account);
            var driver = await CreateHomeConnectAsync(account);

            await driver.RefreshAllAsync();

            Assert.Equal(false, Value(values["connected"]));
            Assert.Null(Value(values["operation_state"]));
            Assert.DoesNotContain(_server.Requests, r => r.Path.Contains("/status"));
        }

        [Fact]
        public async Task Refresh_UsesTheRefreshTokenWithSecretAndPersistsTheNewOne()
        {
            var account = CreateAccount();
            AddAppliance(account);
            var driver = await CreateHomeConnectAsync(account);

            await driver.RefreshAllAsync();
            await driver.RefreshAllAsync();

            var token = Assert.Single(_server.Requests, r => r.Path == "/security/oauth/token");
            var form = QueryHelpers.ParseQuery("?" + token.Body);
            Assert.Equal("refresh_token", form["grant_type"]);
            Assert.Equal(ClientId, form["client_id"]);
            Assert.Equal(ClientSecret, form["client_secret"]);
            Assert.Equal(RefreshToken, form["refresh_token"]);

            var state = account.GetProperty(HomeConnectDriverFactory.TokenStateKey).ValueString;
            Assert.Equal("refresh-2", TokenState.Resolve(state, RefreshToken));
            var restarted = await CreateHomeConnectAsync(account);
            Assert.Equal("refresh-2", restarted.Api.Tokens.RefreshToken);
        }

        [Fact]
        public async Task Refresh_AccessTokenRejected_RefreshesAndRetriesOnce()
        {
            var account = CreateAccount();
            var values = AddAppliance(account);
            var driver = await CreateHomeConnectAsync(account);
            await driver.RefreshAllAsync();

            var inner = _server.Handler;
            _server.Handler = r => r.Path.StartsWith("/api/") && r.Headers["Authorization"] == "Bearer access-1" ? (401, "{}") : inner(r);
            _activeProgram = "Dishcare.Dishwasher.Program.Quick45";
            await driver.RefreshAllAsync();

            Assert.Equal(2, _server.Requests.Count(r => r.Path == "/security/oauth/token"));
            Assert.Equal("Dishcare.Dishwasher.Program.Quick45", Value(values["active_program"]));
        }

        [Fact]
        public async Task RateLimited_PausesAllRequestsUntilRetryAfter()
        {
            var account = CreateAccount();
            AddAppliance(account);
            var driver = await CreateHomeConnectAsync(account);
            var inner = _server.Handler;
            _server.Handler = r => r.Path == "/api/homeappliances" ? (429, "{\"error\":{\"key\":\"429\"}}") : inner(r);

            await Assert.ThrowsAsync<RateLimitedException>(() => driver.RefreshAllAsync());
            var count = _server.Requests.Count;
            await Assert.ThrowsAsync<RateLimitedException>(() => driver.RefreshAllAsync());

            Assert.True(driver.Api.IsBlocked);
            Assert.Equal(count, _server.Requests.Count);
        }

        [Fact]
        public async Task DeviceLogin_NoRefreshToken_ShowsTheCodeAndStoresTheToken()
        {
            var account = CreateAccount(refreshToken: "");
            var values = AddAppliance(account);
            var driver = await CreateHomeConnectAsync(account);

            await driver.DeviceLoginAsync();

            var start = Assert.Single(_server.Requests, r => r.Path == "/security/oauth/device_authorization");
            var form = QueryHelpers.ParseQuery("?" + start.Body);
            Assert.Equal(ClientId, form["client_id"]);
            Assert.Equal(HomeConnectDriver.Scopes, form["scope"]);
            Assert.Equal(2, _devicePolls);
            Assert.Equal("OK", driver.LoginStatus);
            Assert.Equal("refresh-2", TokenState.Resolve(account.GetProperty(HomeConnectDriverFactory.TokenStateKey).ValueString, ""));

            await driver.RefreshAllAsync();
            Assert.Equal("Run", Value(values["operation_state"]));
        }

        [Fact]
        public async Task Start_InvalidRefreshToken_StartsADeviceLogin()
        {
            _devicePolls = -1000; // the user never confirms
            var account = CreateAccount(refreshToken: "revoked");
            AddAppliance(account);
            var driver = await CreateHomeConnectAsync(account);

            await driver.Start();
            await FakeHttpServer.WaitUntilAsync(() => driver.LoginStatus?.StartsWith("Login required") == true);
            await driver.Stop();

            Assert.Contains("https://verify.home-connect.com?user_code=ABCD-1234", driver.LoginStatus);
            Assert.Contains("ABCD-1234", (string)Value(_account[HomeConnectDriverFactory.LoginStatusKey]));
        }

        [Fact]
        public async Task EventStream_AppliesStatusNotifyEventAndConnectionChanges()
        {
            var account = CreateAccount();
            var values = AddAppliance(account);
            var driver = await CreateHomeConnectAsync(account);

            await driver.Start();
            await FakeHttpServer.WaitUntilAsync(() => Equals(Value(_account[HomeConnectDriverFactory.StreamConnectedKey]), true));
            Assert.Equal("OK", driver.LoginStatus);

            await _events.Writer.WriteAsync("event: KEEP-ALIVE\ndata: \n\n");
            await SendEventAsync("NOTIFY", new { items = new object[] { new { key = "BSH.Common.Option.RemainingProgramTime", value = 60, haId = HaId, uri = "/x", timestamp = 1 } } });
            await SendEventAsync("STATUS", new { items = new object[] { new { key = "BSH.Common.Status.OperationState", value = "BSH.Common.EnumType.OperationState.Finished", haId = HaId } } });
            await SendEventAsync("EVENT", new { items = new object[] { new { key = "BSH.Common.Event.ProgramFinished", value = "BSH.Common.EnumType.EventPresentState.Present", haId = HaId, level = "hint" } } });
            await SendEventAsync("NOTIFY", new { items = new object[] { new { key = "BSH.Common.Root.ActiveProgram", value = (string)null, haId = HaId } } });
            await FakeHttpServer.WaitUntilAsync(() => Equals(Value(values["active_program"]), ""));

            Assert.Equal("Finished", Value(values["operation_state"]));
            Assert.Equal(false, Value(values["running"]));
            Assert.Equal("BSH.Common.Event.ProgramFinished", Value(values["last_event"]));
            Assert.Equal(0, Value(values["remaining_time"]));

            await SendEventAsync("DISCONNECTED", null);
            await FakeHttpServer.WaitUntilAsync(() => Equals(Value(values["connected"]), false));

            await driver.Stop();
            Assert.Equal(false, Value(_account[HomeConnectDriverFactory.StreamConnectedKey]));
        }

        [Fact]
        public async Task EventStream_Dropped_Reconnects()
        {
            var account = CreateAccount();
            AddAppliance(account);
            var driver = await CreateHomeConnectAsync(account);

            await driver.Start();
            await FakeHttpServer.WaitUntilAsync(() => _server.Requests.Count(r => r.Path == "/api/homeappliances/events") == 1);
            var first = _events;
            _events = Channel.CreateUnbounded<string>();
            first.Writer.Complete();
            await FakeHttpServer.WaitUntilAsync(() => _server.Requests.Count(r => r.Path == "/api/homeappliances/events") == 2);
            await driver.Stop();
        }

        [Theory]
        [InlineData("power", true, "PUT /settings/BSH.Common.Setting.PowerState {\"data\":{\"key\":\"BSH.Common.Setting.PowerState\",\"value\":\"BSH.Common.EnumType.PowerState.On\"}}")]
        [InlineData("power", false, "PUT /settings/BSH.Common.Setting.PowerState {\"data\":{\"key\":\"BSH.Common.Setting.PowerState\",\"value\":\"BSH.Common.EnumType.PowerState.Off\"}}")]
        [InlineData("active_program", "Dishcare.Dishwasher.Program.Eco50", "PUT /programs/active {\"data\":{\"key\":\"Dishcare.Dishwasher.Program.Eco50\"}}")]
        [InlineData("active_program", "", "DELETE /programs/active")]
        [InlineData("selected_program", "Dishcare.Dishwasher.Program.Quick45", "PUT /programs/selected {\"data\":{\"key\":\"Dishcare.Dishwasher.Program.Quick45\"}}")]
        [InlineData("running", false, "DELETE /programs/active")]
        [InlineData("paused", true, "PUT /commands/BSH.Common.Command.PauseProgram {\"data\":{\"key\":\"BSH.Common.Command.PauseProgram\",\"value\":true}}")]
        [InlineData("paused", false, "PUT /commands/BSH.Common.Command.ResumeProgram {\"data\":{\"key\":\"BSH.Common.Command.ResumeProgram\",\"value\":true}}")]
        public async Task Write_SendsTheMatchingRequest(string key, object value, string expected)
        {
            var account = CreateAccount();
            AddAppliance(account);
            var driver = await CreateHomeConnectAsync(account);

            await WriteAsync(driver, key, value);

            Assert.Equal(new[] { expected }, _writes);
        }

        [Fact]
        public async Task Write_Running_StartsTheSelectedProgramWithItsUserOptions()
        {
            var account = CreateAccount();
            AddAppliance(account);
            var driver = await CreateHomeConnectAsync(account);

            await WriteAsync(driver, "running", true);

            Assert.Equal(new[] { "PUT /programs/active {\"data\":{\"key\":\"Dishcare.Dishwasher.Program.Auto2\",\"options\":[{\"key\":\"BSH.Common.Option.StartInRelative\",\"value\":0},{\"key\":\"Dishcare.Dishwasher.Option.HalfLoad\",\"value\":true}]}}" }, _writes);
            var put = _server.Requests.Single(r => r.Method == "PUT");
            Assert.StartsWith(HomeConnectApi.MediaType, put.Headers["Content-Type"]);
        }

        [Fact]
        public async Task Write_PowerOffNotSupported_FallsBackToStandby()
        {
            var account = CreateAccount();
            AddAppliance(account);
            var driver = await CreateHomeConnectAsync(account);
            var inner = _server.Handler;
            _server.Handler = r => r.Body.Contains("PowerState.Off") ? (409, "{\"error\":{\"key\":\"SDK.Error.InvalidSettingState\"}}") : inner(r);

            await WriteAsync(driver, "power", false);

            Assert.Equal(new[] { "PUT /settings/BSH.Common.Setting.PowerState {\"data\":{\"key\":\"BSH.Common.Setting.PowerState\",\"value\":\"BSH.Common.EnumType.PowerState.Standby\"}}" }, _writes);
        }

        [Theory]
        [InlineData(HomeConnectDataPoints.CustomNumberKey, "Refrigeration.FridgeFreezer.Setting.SetpointTemperatureRefrigerator", 4, "PUT /settings/Refrigeration.FridgeFreezer.Setting.SetpointTemperatureRefrigerator {\"data\":{\"key\":\"Refrigeration.FridgeFreezer.Setting.SetpointTemperatureRefrigerator\",\"value\":4}}")]
        [InlineData(HomeConnectDataPoints.CustomNumberKey, "Cooking.Oven.Option.SetpointTemperature", 180.5, "PUT /programs/active/options/Cooking.Oven.Option.SetpointTemperature {\"data\":{\"key\":\"Cooking.Oven.Option.SetpointTemperature\",\"value\":180.5}}")]
        [InlineData(HomeConnectDataPoints.CustomSwitchKey, "BSH.Common.Setting.ChildLock", true, "PUT /settings/BSH.Common.Setting.ChildLock {\"data\":{\"key\":\"BSH.Common.Setting.ChildLock\",\"value\":true}}")]
        [InlineData(HomeConnectDataPoints.CustomSwitchKey, "BSH.Common.Command.OpenDoor", true, "PUT /commands/BSH.Common.Command.OpenDoor {\"data\":{\"key\":\"BSH.Common.Command.OpenDoor\",\"value\":true}}")]
        [InlineData(HomeConnectDataPoints.CustomTextKey, "Cooking.Common.Setting.Lighting", "true", "PUT /settings/Cooking.Common.Setting.Lighting {\"data\":{\"key\":\"Cooking.Common.Setting.Lighting\",\"value\":\"true\"}}")]
        public async Task Write_CustomValue_GoesToSettingOptionOrCommand(string template, string hcKey, object value, string expected)
        {
            var account = CreateAccount();
            AddAppliance(account, HaId, (template, hcKey));
            var driver = await CreateHomeConnectAsync(account);

            await WriteAsync(driver, template, value, hcKey);

            Assert.Equal(new[] { expected }, _writes);
        }

        [Fact]
        public async Task Write_CustomStatus_IsReadOnly()
        {
            var account = CreateAccount();
            AddAppliance(account, HaId, (HomeConnectDataPoints.CustomNumberKey, "Cooking.Oven.Status.CurrentCavityTemperature"));
            var driver = await CreateHomeConnectAsync(account);

            await WriteAsync(driver, HomeConnectDataPoints.CustomNumberKey, 100, "Cooking.Oven.Status.CurrentCavityTemperature");

            Assert.Empty(_writes);
        }

        [Fact]
        public async Task Write_CommandRejected_DoesNotThrow()
        {
            var account = CreateAccount();
            AddAppliance(account);
            var driver = await CreateHomeConnectAsync(account);
            _server.Handler = r => r.Method == "PUT" ? (403, "{\"error\":{\"key\":\"SDK.Error.RemoteStartNotActivated\"}}") : Handle(r);

            await WriteAsync(driver, "active_program", "Dishcare.Dishwasher.Program.Eco50");
        }

        [Fact]
        public async Task Scan_ListsAppliancesThatAreNotConfigured()
        {
            var account = CreateAccount();
            AddAppliance(account);
            var driver = await CreateHomeConnectAsync(account);

            var found = Assert.Single(await driver.Scan());

            Assert.Equal("Siemens Fridge", found.Name);
            Assert.Equal(FridgeId, found.GetProperty(HomeConnectDriverFactory.HaIdKey).ValueString);
            Assert.Equal(HomeConnectDataPoints.Defaults.Count, found.InverseThis2ParentNodeInstanceNavigation.Count);
        }

        [Fact]
        public async Task Sse_ParsesMultiLineDataAndComments()
        {
            var events = new List<SseEvent>();
            var text = ": comment\nevent: STATUS\ndata: {\"a\":\ndata: 1}\nid: X\n\nevent: KEEP-ALIVE\ndata:\n\n";
            await using var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));

            await Sse.ReadAsync(stream, e => { events.Add(e); return Task.CompletedTask; }, TimeSpan.FromSeconds(5), default);

            Assert.Equal(new SseEvent("STATUS", "{\"a\":\n1}", "X"), events[0]);
            Assert.Equal("KEEP-ALIVE", events[1].Event);
        }
    }
}
