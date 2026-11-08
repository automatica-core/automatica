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
using P3.Driver.Miele.Driver;
using Xunit;

namespace P3.Driver.Miele.Tests
{
    public class MieleDriverTests : DriverFactoryTestBase<MieleDriverFactory>, IAsyncLifetime
    {
        private const string WasherId = "000123456789";
        private const string FridgeId = "000987654321";
        private const string ClientId = "client-123";
        private const string ClientSecret = "secret-456";
        private const string RefreshToken = "refresh-1";

        private FakeHttpServer _server;
        private readonly HttpClient _http = new();
        private readonly HttpClient _streamHttp = new() { Timeout = System.Threading.Timeout.InfiniteTimeSpan };
        private int _tokenCounter;
        private int _washerStatus = 5;
        private readonly List<string> _writes = new();
        private readonly Channel<string> _events = Channel.CreateUnbounded<string>();

        public async Task InitializeAsync()
        {
            _server = await FakeHttpServer.StartAsync();
            _server.Handler = Handle;
            _server.StreamHandler = r => r.Path == "/v1/devices/all/events" ? _events.Reader : null;
        }

        public async Task DisposeAsync()
        {
            _events.Writer.TryComplete();
            await _server.DisposeAsync();
            _http.Dispose();
            _streamHttp.Dispose();
        }

        private object Washer(int status) => new
        {
            ident = new { type = new { value_raw = 1, value_localized = "Washing machine" }, deviceName = "", deviceIdentLabel = new { fabNumber = WasherId, techType = "WCI870" } },
            state = new
            {
                ProgramID = new { value_raw = 3, value_localized = "Cottons" },
                status = new { value_raw = status, value_localized = "In use" },
                programPhase = new { value_raw = 260, value_localized = "Main wash" },
                remainingTime = new[] { 1, 15 },
                startTime = new[] { 0, 0 },
                elapsedTime = new[] { 0, 40 },
                targetTemperature = new[] { new { value_raw = 4000, value_localized = (int?)40, unit = "Celsius" }, new { value_raw = -32768, value_localized = (int?)null, unit = "Celsius" } },
                temperature = new[] { new { value_raw = -32768, value_localized = (int?)null, unit = "Celsius" } },
                signalInfo = false,
                signalFailure = false,
                signalDoor = false,
                remoteEnable = new { fullRemoteControl = true, smartGrid = false, mobileStart = true },
                light = 0,
                spinningSpeed = new { unit = "rpm", value_raw = 1400, value_localized = "1400" },
                ecoFeedback = new { currentWaterConsumption = new { unit = "l", value = 12.5 }, currentEnergyConsumption = new { unit = "kWh", value = 0.4 }, energyForecast = 0.2, waterForecast = 0.5 },
                batteryLevel = (int?)null
            }
        };

        private object Fridge() => new
        {
            ident = new { type = new { value_raw = 21, value_localized = "Fridge freezer" }, deviceName = "Kitchen fridge" },
            state = new
            {
                status = new { value_raw = 146, value_localized = "Supercooling superfreezing" },
                targetTemperature = new[] { new { value_raw = 400, value_localized = 4d, unit = "Celsius" }, new { value_raw = -1800, value_localized = -18d, unit = "Celsius" } },
                temperature = new[] { new { value_raw = 512, value_localized = 5.12, unit = "Celsius" }, new { value_raw = -1790, value_localized = -17.9, unit = "Celsius" } },
                signalDoor = true,
                light = 1
            }
        };

        private (int, string) Handle(RecordedRequest request)
        {
            if (request.Path == "/token")
            {
                var form = QueryHelpers.ParseQuery("?" + request.Body);
                if (form["grant_type"] != "refresh_token" || form["refresh_token"] != $"refresh-{_tokenCounter + 1}" || form["client_id"] != ClientId || form["client_secret"] != ClientSecret)
                {
                    return (400, "{\"error\":\"invalid_grant\"}");
                }
                _tokenCounter++;
                return (200, JsonSerializer.Serialize(new { access_token = $"access-{_tokenCounter}", refresh_token = $"refresh-{_tokenCounter + 1}", expires_in = 2592000, token_type = "Bearer" }));
            }
            if (!request.Headers.TryGetValue("Authorization", out var auth) || !auth.StartsWith("Bearer access-"))
            {
                return (401, "{\"code\":401,\"message\":\"Unauthorized\"}");
            }
            if (request.Method == "GET" && request.Path == "/v1/devices")
            {
                Assert.Equal("de", QueryHelpers.ParseQuery(request.Query)["language"]);
                return (200, JsonSerializer.Serialize(new Dictionary<string, object> { [WasherId] = Washer(_washerStatus), [FridgeId] = Fridge() }));
            }
            if (request.Method == "PUT" && request.Path.StartsWith($"/v1/devices/{WasherId}/"))
            {
                _writes.Add($"{request.Path.Split('/').Last()} {request.Body}");
                return (204, "");
            }
            return (404, "{\"code\":404,\"message\":\"not found\"}");
        }

        private NodeInstance CreateAccount(string refreshToken = RefreshToken)
        {
            var node = CreateNodeInstance(DriverFactory.DriverGuid);
            node.SetProperty(MieleDriverFactory.ApiUrlKey, _server.Url);
            node.SetProperty(MieleDriverFactory.TokenUrlKey, _server.Url + "/token");
            node.SetProperty(MieleDriverFactory.ClientIdKey, ClientId);
            node.SetProperty(MieleDriverFactory.ClientSecretKey, ClientSecret);
            node.SetProperty(MieleDriverFactory.RefreshTokenKey, refreshToken);
            node.SetProperty(MieleDriverFactory.LanguageKey, "de");
            node.SetProperty(MieleDriverFactory.PollKey, 3600);
            _login = CreateNodeInstance(MieleDriverFactory.LoginStatusTemplate);
            _stream = CreateNodeInstance(MieleDriverFactory.StreamConnectedTemplate);
            node.InverseThis2ParentNodeInstanceNavigation.Add(_login);
            node.InverseThis2ParentNodeInstanceNavigation.Add(_stream);
            return node;
        }

        private NodeInstance _login;
        private NodeInstance _stream;

        private Dictionary<string, NodeInstance> AddAppliance(NodeInstance account, string id)
        {
            var appliance = CreateNodeInstance(MieleDriverFactory.ApplianceTemplate);
            appliance.SetProperty(MieleDriverFactory.DeviceIdKey, id);
            account.InverseThis2ParentNodeInstanceNavigation.Add(appliance);
            var values = new Dictionary<string, NodeInstance>();
            foreach (var dp in MieleDataPoints.All)
            {
                var child = CreateNodeInstance(dp.Template);
                appliance.InverseThis2ParentNodeInstanceNavigation.Add(child);
                values[dp.Key] = child;
            }
            return values;
        }

        private async Task<MieleDriver> CreateMieleAsync(NodeInstance account)
        {
            var driver = (MieleDriver)await CreateDriver(account);
            driver.Http = _http;
            driver.StreamHttp = _streamHttp;
            driver.MinReconnectDelay = TimeSpan.FromMilliseconds(50);
            await driver.Init();
            return driver;
        }

        private object Value(NodeInstance node) => Dispatcher.GetValue(DispatchableType.NodeInstance, node.ObjId)?.Value;

        private Task WriteAsync(MieleDriver driver, string key, object value) =>
            driver.Appliances.Single(a => a.DeviceId == WasherId).Values.Single(v => v.Key == key).WriteDirectAsync(value);

        [Fact]
        public async Task Refresh_Washer_DispatchesTheState()
        {
            var account = CreateAccount();
            var values = AddAppliance(account, WasherId);
            var driver = await CreateMieleAsync(account);

            await driver.RefreshAllAsync();

            Assert.Equal(true, Value(values["connected"]));
            Assert.Equal(5, Value(values["status_code"]));
            Assert.Equal("Running", Value(values["status"]));
            Assert.Equal(true, Value(values["power"]));
            Assert.Equal(true, Value(values["running"]));
            Assert.Equal(false, Value(values["paused"]));
            Assert.Equal("Cottons", Value(values["program"]));
            Assert.Equal(3, Value(values["program_id"]));
            Assert.Equal("Main wash", Value(values["program_phase"]));
            Assert.Equal(75, Value(values["remaining_time"]));
            Assert.Equal(40, Value(values["elapsed_time"]));
            Assert.Equal(0, Value(values["start_time"]));
            Assert.Equal(40d, Value(values["target_temperature"]));
            Assert.Null(Value(values["target_temperature_2"]));
            Assert.Null(Value(values["temperature"]));
            Assert.Equal(false, Value(values["door_open"]));
            Assert.Equal(true, Value(values["remote_control"]));
            Assert.Equal(true, Value(values["mobile_start"]));
            Assert.Null(Value(values["light"]));
            Assert.Equal(1400, Value(values["spinning_speed"]));
            Assert.Equal(0.4, Value(values["energy_consumption"]));
            Assert.Equal(12.5, Value(values["water_consumption"]));
            Assert.Null(Value(values["battery_level"]));
        }

        [Fact]
        public async Task Refresh_Fridge_TemperaturesInDegreesAndSuperModes()
        {
            var account = CreateAccount();
            var values = AddAppliance(account, FridgeId);
            var driver = await CreateMieleAsync(account);

            await driver.RefreshAllAsync();

            Assert.Equal(5.12, Value(values["temperature"]));
            Assert.Equal(-17.9, Value(values["temperature_2"]));
            Assert.Equal(4d, Value(values["target_temperature"]));
            Assert.Equal(-18d, Value(values["target_temperature_2"]));
            Assert.Equal(true, Value(values["superfreezing"]));
            Assert.Equal(true, Value(values["supercooling"]));
            Assert.Equal(true, Value(values["door_open"]));
            Assert.Equal(true, Value(values["light"]));
        }

        [Fact]
        public async Task Refresh_RotatedRefreshTokenIsPersisted()
        {
            var account = CreateAccount();
            AddAppliance(account, WasherId);
            var driver = await CreateMieleAsync(account);

            await driver.RefreshAllAsync();
            await driver.RefreshAllAsync();

            Assert.Single(_server.Requests, r => r.Path == "/token");
            Assert.Equal("refresh-2", TokenState.Resolve(account.GetProperty(MieleDriverFactory.TokenStateKey).ValueString, RefreshToken));
            var restarted = await CreateMieleAsync(account);
            Assert.Equal("refresh-2", restarted.Api.Tokens.RefreshToken);
        }

        [Fact]
        public async Task Refresh_ServiceUnavailable_PausesRequests()
        {
            var account = CreateAccount();
            AddAppliance(account, WasherId);
            var driver = await CreateMieleAsync(account);
            var inner = _server.Handler;
            _server.Handler = r => r.Path == "/v1/devices" ? (503, "{\"code\":503,\"message\":\"Too many requests\"}") : inner(r);

            await Assert.ThrowsAsync<RateLimitedException>(() => driver.RefreshAllAsync());
            var count = _server.Requests.Count;
            await Assert.ThrowsAsync<RateLimitedException>(() => driver.RefreshAllAsync());
            Assert.Equal(count, _server.Requests.Count);
        }

        [Fact]
        public async Task Start_InvalidRefreshToken_ReportsTheLoginStatus()
        {
            var account = CreateAccount(refreshToken: "revoked");
            AddAppliance(account, WasherId);
            var driver = await CreateMieleAsync(account);

            await driver.Start();
            await FakeHttpServer.WaitUntilAsync(() => driver.LoginStatus?.StartsWith("Login expired") == true);
            await driver.Stop();

            Assert.StartsWith("Login expired", (string)Value(_login));
        }

        [Fact]
        public async Task EventStream_DevicesEventUpdatesTheAppliance()
        {
            var account = CreateAccount();
            var values = AddAppliance(account, WasherId);
            var driver = await CreateMieleAsync(account);

            await driver.Start();
            await FakeHttpServer.WaitUntilAsync(() => Equals(Value(_stream), true));
            Assert.Equal("Running", Value(values["status"]));

            await _events.Writer.WriteAsync("event: ping\ndata: ping\n\n");
            await _events.Writer.WriteAsync($"event: devices\ndata: {JsonSerializer.Serialize(new Dictionary<string, object> { [WasherId] = Washer(7) })}\n\n");
            await FakeHttpServer.WaitUntilAsync(() => Equals(Value(values["status"]), "EndProgrammed"));
            Assert.Equal(false, Value(values["running"]));

            await driver.Stop();
            Assert.Equal(false, Value(_stream));
        }

        [Theory]
        [InlineData("power", true, "actions {\"powerOn\":true}")]
        [InlineData("power", false, "actions {\"powerOff\":true}")]
        [InlineData("running", true, "actions {\"processAction\":1}")]
        [InlineData("running", false, "actions {\"processAction\":2}")]
        [InlineData("paused", true, "actions {\"processAction\":3}")]
        [InlineData("paused", false, "actions {\"processAction\":1}")]
        [InlineData("superfreezing", true, "actions {\"processAction\":4}")]
        [InlineData("supercooling", false, "actions {\"processAction\":7}")]
        [InlineData("light", true, "actions {\"light\":1}")]
        [InlineData("light", false, "actions {\"light\":2}")]
        [InlineData("target_temperature", 4.4, "actions {\"targetTemperature\":[{\"zone\":1,\"value\":4}]}")]
        [InlineData("target_temperature_2", -18, "actions {\"targetTemperature\":[{\"zone\":2,\"value\":-18}]}")]
        [InlineData("start_time", 90, "actions {\"startTime\":[1,30]}")]
        [InlineData("ventilation_step", 7, "actions {\"ventilationStep\":4}")]
        [InlineData("program_id", 24, "programs {\"programId\":24}")]
        public async Task Write_SendsTheMatchingAction(string key, object value, string expected)
        {
            var account = CreateAccount();
            AddAppliance(account, WasherId);
            var driver = await CreateMieleAsync(account);

            await WriteAsync(driver, key, value);

            Assert.Equal(new[] { expected }, _writes);
        }

        [Fact]
        public async Task Write_Rejected_DoesNotThrow()
        {
            var account = CreateAccount();
            AddAppliance(account, WasherId);
            var driver = await CreateMieleAsync(account);
            _server.Handler = r => r.Method == "PUT" ? (400, "{\"code\":400,\"message\":\"Action not allowed\"}") : Handle(r);

            await WriteAsync(driver, "running", true);
        }

        [Fact]
        public async Task Scan_ListsAppliancesWithTheValuesTheyReport()
        {
            var account = CreateAccount();
            AddAppliance(account, WasherId);
            var driver = await CreateMieleAsync(account);

            var found = Assert.Single(await driver.Scan());

            Assert.Equal("Kitchen fridge", found.Name);
            Assert.Equal(FridgeId, found.GetProperty(MieleDriverFactory.DeviceIdKey).ValueString);
            var templates = found.InverseThis2ParentNodeInstanceNavigation.Select(n => n.This2NodeTemplate).ToHashSet();
            Assert.Contains(MieleDataPoints.All.Single(d => d.Key == "temperature_2").Template, templates);
            Assert.DoesNotContain(MieleDataPoints.All.Single(d => d.Key == "spinning_speed").Template, templates);
        }

        [Fact]
        public async Task Refresh_ApplianceNoLongerShared_IsDisconnected()
        {
            var account = CreateAccount();
            var values = AddAppliance(account, "000111111111");
            var driver = await CreateMieleAsync(account);

            await driver.RefreshAllAsync();

            Assert.Equal(false, Value(values["connected"]));
        }
    }
}
