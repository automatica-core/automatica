using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Microsoft.AspNetCore.WebUtilities;
using P3.Driver.Gardena.Driver;
using Xunit;

namespace P3.Driver.Gardena.Tests
{
    public class GardenaDriverTests : DriverFactoryTestBase<GardenaDriverFactory>, IAsyncLifetime
    {
        private const string AppKey = "app-key-123";
        private const string AppSecret = "app-secret-456";
        private const string LocationId = "loc-1";
        private const string ValveId = "irrigation-1:1";
        private const string MowerId = "mower-2";
        private const string SocketId = "socket-3";
        private const string SensorId = "sensor-4";

        private FakeHttpServer _server;
        private readonly HttpClient _http = new();
        private int _tokens;
        private readonly List<string> _commands = new();
        private readonly Channel<string> _socketMessages = Channel.CreateUnbounded<string>();
        private int _sockets;

        public async Task InitializeAsync()
        {
            _server = await FakeHttpServer.StartAsync();
            _server.Handler = Handle;
            _server.WebSocketHandler = async (request, socket) =>
            {
                _sockets++;
                Assert.Equal("/socket/abc", request.Path);
                await FakeHttpServer.SendTextAsync(socket, JsonSerializer.Serialize(new { id = ValveId, type = "VALVE", attributes = new { activity = new { value = "MANUAL_WATERING", timestamp = "2026-11-08T10:00:00Z" }, duration = new { value = 1800 } } }));
                await foreach (var message in _socketMessages.Reader.ReadAllAsync())
                {
                    if (message == "close")
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, "going away", default);
                        return;
                    }
                    await FakeHttpServer.SendTextAsync(socket, message);
                }
            };
        }

        public async Task DisposeAsync()
        {
            _socketMessages.Writer.TryComplete();
            await _server.DisposeAsync();
            _http.Dispose();
        }

        private static object V(object value) => new { value, timestamp = "2026-11-08T09:00:00Z" };

        private static string Location() => JsonSerializer.Serialize(new
        {
            data = new { id = LocationId, type = "LOCATION", attributes = new { name = V("Garden") } },
            included = new object[]
            {
                new { id = "irrigation-1", type = "DEVICE" },
                new { id = "irrigation-1", type = "COMMON", attributes = new { name = V("Irrigation"), batteryLevel = V(null), rfLinkLevel = V(80), rfLinkState = V("ONLINE"), serial = V("123"), modelType = V("GARDENA smart Irrigation Control") } },
                new { id = ValveId, type = "VALVE", attributes = new { name = V("Lawn"), activity = V("CLOSED"), state = V("OK"), lastErrorCode = V("NO_MESSAGE"), duration = V(0) }, relationships = new { device = new { data = new { id = "irrigation-1", type = "DEVICE" } } } },
                new { id = "irrigation-1:2", type = "VALVE", attributes = new { name = V("Hedge"), activity = V("CLOSED"), state = V("OK") }, relationships = new { device = new { data = new { id = "irrigation-1", type = "DEVICE" } } } },
                new { id = "irrigation-1", type = "VALVE_SET", attributes = new { state = V("OK") } },
                new { id = MowerId, type = "COMMON", attributes = new { name = V("Sileno"), batteryLevel = V(77), batteryState = V("OK"), rfLinkState = V("ONLINE") } },
                new { id = MowerId, type = "MOWER", attributes = new { activity = V("OK_CUTTING"), state = V("OK"), lastErrorCode = V("NO_MESSAGE"), operatingHours = new { value = 412 } } },
                new { id = SocketId, type = "COMMON", attributes = new { name = V("Pond pump"), rfLinkState = V("ONLINE") } },
                new { id = SocketId, type = "POWER_SOCKET", attributes = new { activity = V("OFF"), state = V("OK") } },
                new { id = SensorId, type = "COMMON", attributes = new { name = V("Sensor"), batteryLevel = V(95), rfLinkState = V("ONLINE") } },
                new { id = SensorId, type = "SENSOR", attributes = new { soilHumidity = V(34), soilTemperature = V(12), ambientTemperature = V(15), lightIntensity = V(12000) } }
            }
        });

        private (int, string) Handle(RecordedRequest request)
        {
            if (request.Path == "/oauth2/token")
            {
                var form = QueryHelpers.ParseQuery("?" + request.Body);
                if (form["grant_type"] != "client_credentials" || form["client_id"] != AppKey || form["client_secret"] != AppSecret)
                {
                    return (401, "{\"error\":\"invalid_client\"}");
                }
                _tokens++;
                return (201, JsonSerializer.Serialize(new { access_token = $"access-{_tokens}", scope = "iam:read", expires_in = 3600, provider = "husqvarna", user_id = "u1", token_type = "Bearer" }));
            }
            if (!request.Headers.TryGetValue("Authorization", out var auth) || !auth.StartsWith("Bearer access-") || request.Headers["X-Api-Key"] != AppKey)
            {
                return (401, "{\"message\":\"Unauthorized\"}");
            }
            switch (request.Method, request.Path)
            {
                case ("GET", "/v2/locations"):
                    return (200, JsonSerializer.Serialize(new { data = new[] { new { id = LocationId, type = "LOCATION", attributes = new { name = "Garden" } } } }));
                case ("GET", "/v2/locations/" + LocationId):
                    return (200, Location());
                case ("POST", "/v2/websocket"):
                    Assert.Contains(LocationId, request.Body);
                    return (201, JsonSerializer.Serialize(new { data = new { id = "x", type = "WEBSOCKET", attributes = new { validity = 30, url = _server.Url.Replace("http://", "ws://") + "/socket/abc" } } }));
            }
            if (request.Method == "PUT" && request.Path.StartsWith("/v2/command/"))
            {
                Assert.StartsWith(GardenaApi.MediaType, request.Headers["Content-Type"]);
                using var body = JsonDocument.Parse(request.Body);
                var attributes = body.RootElement.GetProperty("data").GetProperty("attributes");
                _commands.Add($"{Uri.UnescapeDataString(request.Path["/v2/command/".Length..])} {body.RootElement.GetProperty("data").GetProperty("type").GetString()} {attributes.GetProperty("command").GetString()}" +
                              (attributes.TryGetProperty("seconds", out var s) ? $" {s.GetInt32()}" : ""));
                return (202, "");
            }
            return (404, "{\"errors\":[{\"status\":\"404\"}]}");
        }

        private NodeInstance CreateAccount()
        {
            var node = CreateNodeInstance(DriverFactory.DriverGuid);
            node.SetProperty(GardenaDriverFactory.AuthUrlKey, _server.Url + "/oauth2/token");
            node.SetProperty(GardenaDriverFactory.ApiUrlKey, _server.Url + "/v2");
            node.SetProperty(GardenaDriverFactory.ApplicationKeyKey, AppKey);
            node.SetProperty(GardenaDriverFactory.ApplicationSecretKey, AppSecret);
            node.SetProperty(GardenaDriverFactory.LocationIdKey, "");
            _socketConnected = CreateNodeInstance(GardenaDriverFactory.SocketConnectedTemplate);
            _status = CreateNodeInstance(GardenaDriverFactory.StatusTemplate);
            node.InverseThis2ParentNodeInstanceNavigation.Add(_socketConnected);
            node.InverseThis2ParentNodeInstanceNavigation.Add(_status);
            return node;
        }

        private NodeInstance _socketConnected;
        private NodeInstance _status;

        private Dictionary<string, NodeInstance> AddService(NodeInstance account, string kind, string serviceId, int? minutes = null)
        {
            var k = GardenaDataPoints.Kind(kind);
            var service = CreateNodeInstance(k.Template);
            service.SetProperty(GardenaDriverFactory.ServiceIdKey, serviceId);
            if (minutes.HasValue)
            {
                service.SetProperty(GardenaDriverFactory.DefaultMinutesKey, minutes.Value);
            }
            account.InverseThis2ParentNodeInstanceNavigation.Add(service);
            var values = new Dictionary<string, NodeInstance>();
            foreach (var dp in GardenaDataPoints.Of(kind))
            {
                var child = CreateNodeInstance(dp.Template);
                service.InverseThis2ParentNodeInstanceNavigation.Add(child);
                values[dp.Key] = child;
            }
            return values;
        }

        private async Task<GardenaDriver> CreateGardenaAsync(NodeInstance account)
        {
            var driver = (GardenaDriver)await CreateDriver(account);
            driver.Http = _http;
            driver.MinReconnectDelay = TimeSpan.FromMilliseconds(50);
            await driver.Init();
            return driver;
        }

        private object Value(NodeInstance node) => Dispatcher.GetValue(DispatchableType.NodeInstance, node.ObjId)?.Value;

        private Task WriteAsync(GardenaDriver driver, string serviceId, string key, object value) =>
            driver.Services.Single(s => s.ServiceId == serviceId).Values.Single(v => v.Key == key).WriteDirectAsync(value);

        [Fact]
        public async Task Snapshot_DispatchesAllServicesWithCommonValues()
        {
            var account = CreateAccount();
            var valve = AddService(account, GardenaDataPoints.Valve, ValveId);
            var mower = AddService(account, GardenaDataPoints.Mower, MowerId);
            var socket = AddService(account, GardenaDataPoints.PowerSocket, SocketId);
            var sensor = AddService(account, GardenaDataPoints.Sensor, SensorId);
            var driver = await CreateGardenaAsync(account);

            await driver.LoadSnapshotAsync();

            Assert.Equal(false, Value(valve["watering"]));
            Assert.Equal("CLOSED", Value(valve["activity"]));
            Assert.Equal(0, Value(valve["water_minutes"]));
            Assert.Equal("OK", Value(valve["state"]));
            Assert.Equal("NO_MESSAGE", Value(valve["last_error"]));
            Assert.Null(Value(valve["battery_level"]));
            Assert.Equal("ONLINE", Value(valve["rf_link_state"]));

            Assert.Equal(true, Value(mower["mowing"]));
            Assert.Equal("OK_CUTTING", Value(mower["activity"]));
            Assert.Equal(412, Value(mower["operating_hours"]));
            Assert.Equal(77, Value(mower["battery_level"]));
            Assert.Equal("OK", Value(mower["battery_state"]));

            Assert.Equal(false, Value(socket["on"]));
            Assert.Equal("OFF", Value(socket["activity"]));

            Assert.Equal(34, Value(sensor["soil_humidity"]));
            Assert.Equal(12d, Value(sensor["soil_temperature"]));
            Assert.Equal(15d, Value(sensor["ambient_temperature"]));
            Assert.Equal(12000, Value(sensor["light_intensity"]));
            Assert.Equal(95, Value(sensor["battery_level"]));

            var token = Assert.Single(_server.Requests, r => r.Path == "/oauth2/token");
            Assert.Equal(2, driver.Api.RequestCount); // locations + location
        }

        [Fact]
        public async Task WebSocket_PatchesTheStateAndRemovesNullAttributes()
        {
            var account = CreateAccount();
            var valve = AddService(account, GardenaDataPoints.Valve, ValveId);
            var mower = AddService(account, GardenaDataPoints.Mower, MowerId);
            var driver = await CreateGardenaAsync(account);

            await driver.Start();
            await FakeHttpServer.WaitUntilAsync(() => Equals(Value(valve["watering"]), true));
            Assert.Equal(30, Value(valve["water_minutes"]));
            Assert.Equal(true, Value(_socketConnected));
            Assert.Equal("OK", Value(_status));

            await _socketMessages.Writer.WriteAsync(JsonSerializer.Serialize(new { id = MowerId, type = "COMMON", attributes = new { batteryLevel = V(76) } }));
            await _socketMessages.Writer.WriteAsync(JsonSerializer.Serialize(new { id = MowerId, type = "MOWER", attributes = new { activity = V("OK_CHARGING"), lastErrorCode = (object)null } }));
            await FakeHttpServer.WaitUntilAsync(() => Equals(Value(mower["activity"]), "OK_CHARGING"));

            Assert.Equal(false, Value(mower["mowing"]));
            Assert.Equal(76, Value(mower["battery_level"]));
            Assert.Null(driver.GetValue("MOWER", MowerId, "lastErrorCode", false));
            Assert.Equal("OK", Value(mower["state"]));

            await driver.Stop();
            Assert.Equal(false, Value(_socketConnected));
        }

        [Fact]
        public async Task WebSocket_ClosedByServer_ReconnectsWithoutReadingTheLocationAgain()
        {
            var account = CreateAccount();
            AddService(account, GardenaDataPoints.Valve, ValveId);
            var driver = await CreateGardenaAsync(account);

            await driver.Start();
            await FakeHttpServer.WaitUntilAsync(() => _sockets == 1);
            await _socketMessages.Writer.WriteAsync("close");
            await FakeHttpServer.WaitUntilAsync(() => _sockets == 2);
            await driver.Stop();

            Assert.Single(_server.Requests, r => r.Path == "/v2/locations/" + LocationId);
            Assert.Equal(2, _server.Requests.Count(r => r.Path == "/v2/websocket"));
        }

        [Fact]
        public async Task WebSocket_LifetimeReached_ReconnectsBeforeTheServerLimit()
        {
            var account = CreateAccount();
            AddService(account, GardenaDataPoints.Valve, ValveId);
            var driver = await CreateGardenaAsync(account);
            driver.SocketLifetime = TimeSpan.FromMilliseconds(300);

            await driver.Start();
            await FakeHttpServer.WaitUntilAsync(() => _sockets >= 2);
            await driver.Stop();
        }

        [Fact]
        public async Task RateLimited_StopsSendingRequests()
        {
            var account = CreateAccount();
            AddService(account, GardenaDataPoints.Valve, ValveId);
            var driver = await CreateGardenaAsync(account);
            _server.Handler = r => r.Path.StartsWith("/v2/") ? (429, "{\"message\":\"Limit Exceeded\"}") : Handle(r);

            await Assert.ThrowsAsync<RateLimitedException>(() => driver.LoadSnapshotAsync());
            var count = _server.Requests.Count;
            await Assert.ThrowsAsync<RateLimitedException>(() => WriteThrowingAsync(driver));
            Assert.Equal(count, _server.Requests.Count);
            Assert.True(driver.Api.BlockedUntil > DateTimeOffset.UtcNow.AddMinutes(30));
        }

        private Task WriteThrowingAsync(GardenaDriver driver) => driver.Api.CommandAsync(ValveId, new GardenaCommand("VALVE_CONTROL", "STOP_UNTIL_NEXT_TASK"));

        [Theory]
        [InlineData(GardenaDataPoints.Valve, ValveId, "watering", true, "irrigation-1:1 VALVE_CONTROL START_SECONDS_TO_OVERRIDE 600")]
        [InlineData(GardenaDataPoints.Valve, ValveId, "watering", false, "irrigation-1:1 VALVE_CONTROL STOP_UNTIL_NEXT_TASK")]
        [InlineData(GardenaDataPoints.Valve, ValveId, "water_minutes", 15, "irrigation-1:1 VALVE_CONTROL START_SECONDS_TO_OVERRIDE 900")]
        [InlineData(GardenaDataPoints.Valve, ValveId, "water_minutes", 0, "irrigation-1:1 VALVE_CONTROL STOP_UNTIL_NEXT_TASK")]
        [InlineData(GardenaDataPoints.Valve, ValveId, "pause", true, "irrigation-1:1 VALVE_CONTROL PAUSE")]
        [InlineData(GardenaDataPoints.Valve, ValveId, "pause", false, "irrigation-1:1 VALVE_CONTROL UNPAUSE")]
        [InlineData(GardenaDataPoints.Mower, MowerId, "mowing", true, "mower-2 MOWER_CONTROL START_SECONDS_TO_OVERRIDE 600")]
        [InlineData(GardenaDataPoints.Mower, MowerId, "mowing", false, "mower-2 MOWER_CONTROL PARK_UNTIL_NEXT_TASK")]
        [InlineData(GardenaDataPoints.Mower, MowerId, "mow_minutes", 90.4, "mower-2 MOWER_CONTROL START_SECONDS_TO_OVERRIDE 5400")]
        [InlineData(GardenaDataPoints.Mower, MowerId, "park", true, "mower-2 MOWER_CONTROL PARK_UNTIL_FURTHER_NOTICE")]
        [InlineData(GardenaDataPoints.Mower, MowerId, "resume_schedule", true, "mower-2 MOWER_CONTROL START_DONT_OVERRIDE")]
        [InlineData(GardenaDataPoints.PowerSocket, SocketId, "on", true, "socket-3 POWER_SOCKET_CONTROL START_OVERRIDE")]
        [InlineData(GardenaDataPoints.PowerSocket, SocketId, "on", false, "socket-3 POWER_SOCKET_CONTROL STOP_UNTIL_NEXT_TASK")]
        [InlineData(GardenaDataPoints.PowerSocket, SocketId, "on_minutes", 5, "socket-3 POWER_SOCKET_CONTROL START_SECONDS_TO_OVERRIDE 300")]
        public async Task Write_SendsTheMatchingCommand(string kind, string serviceId, string key, object value, string expected)
        {
            var account = CreateAccount();
            AddService(account, kind, serviceId, 10);
            var driver = await CreateGardenaAsync(account);

            await WriteAsync(driver, serviceId, key, value);

            Assert.Equal(new[] { expected }, _commands);
        }

        [Fact]
        public async Task Write_MomentaryReleased_SendsNothing()
        {
            var account = CreateAccount();
            AddService(account, GardenaDataPoints.Mower, MowerId);
            var driver = await CreateGardenaAsync(account);

            await WriteAsync(driver, MowerId, "park", false);

            Assert.Empty(_commands);
            Assert.DoesNotContain(_server.Requests, r => r.Path.StartsWith("/v2/"));
        }

        [Fact]
        public async Task Write_Rejected_DoesNotThrow()
        {
            var account = CreateAccount();
            AddService(account, GardenaDataPoints.Valve, ValveId);
            var driver = await CreateGardenaAsync(account);
            _server.Handler = r => r.Path.StartsWith("/v2/command/") ? (400, "{\"errors\":[{\"title\":\"seconds\"}]}") : Handle(r);

            await WriteAsync(driver, ValveId, "watering", true);
        }

        [Fact]
        public async Task Scan_ListsServicesWithTheirNames()
        {
            var account = CreateAccount();
            AddService(account, GardenaDataPoints.Valve, ValveId);
            var driver = await CreateGardenaAsync(account);

            var found = await driver.Scan();

            Assert.Equal(new[] { "Hedge", "Sileno", "Pond pump", "Sensor" }, found.Select(f => f.Name));
            Assert.Equal("irrigation-1:2", found[0].GetProperty(GardenaDriverFactory.ServiceIdKey).ValueString);
            Assert.Equal(GardenaDataPoints.Kind(GardenaDataPoints.Mower).Template, found[1].This2NodeTemplate);
            Assert.Equal(GardenaDataPoints.Of(GardenaDataPoints.Sensor).Count(), found[3].InverseThis2ParentNodeInstanceNavigation.Count);
        }

        [Fact]
        public async Task Start_WrongSecret_ReportsTheStatus()
        {
            var account = CreateAccount();
            account.SetProperty(GardenaDriverFactory.ApplicationSecretKey, "wrong");
            AddService(account, GardenaDataPoints.Valve, ValveId);
            var driver = await CreateGardenaAsync(account);

            await driver.Start();
            await FakeHttpServer.WaitUntilAsync(() => driver.Status?.Contains("invalid_client") == true);
            await driver.Stop();
        }
    }
}
