using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Microsoft.AspNetCore.WebUtilities;
using P3.Driver.Automower.Driver;
using Xunit;

namespace P3.Driver.Automower.Tests
{
    public class AutomowerDriverTests : DriverFactoryTestBase<AutomowerDriverFactory>, IAsyncLifetime
    {
        private const string AppKey = "app-key-123";
        private const string AppSecret = "app-secret-456";
        private const string MowerId = "c7233734-b219-4287-a173-08e3643f89f0";
        private const string OtherMowerId = "1d2e3f40-0000-4000-8000-000000000002";

        private FakeHttpServer _server;
        private readonly HttpClient _http = new();
        private int _tokens;
        private readonly ConcurrentQueue<string> _commands = new();
        private readonly ConcurrentQueue<string> _received = new();
        private readonly ConcurrentQueue<string> _socketAuth = new();
        private readonly Channel<string> _socketMessages = Channel.CreateUnbounded<string>();
        private int _sockets;

        private NodeInstance _status;
        private NodeInstance _socketConnected;

        public async Task InitializeAsync()
        {
            _server = await FakeHttpServer.StartAsync();
            _server.Handler = Handle;
            _server.WebSocketHandler = async (request, socket) =>
            {
                _sockets++;
                _socketAuth.Enqueue(request.Headers.TryGetValue("Authorization", out var auth) ? auth : "");
                await FakeHttpServer.SendTextAsync(socket, "{\"ready\":true,\"connectionId\":\"abc=\"}");
                var receive = Task.Run(async () =>
                {
                    try
                    {
                        while (socket.State == WebSocketState.Open)
                        {
                            var text = await FakeHttpServer.ReceiveTextAsync(socket);
                            if (text == null)
                            {
                                return;
                            }
                            _received.Enqueue(text);
                        }
                    }
                    catch (Exception)
                    {
                        // client went away
                    }
                });
                try
                {
                    await foreach (var message in _socketMessages.Reader.ReadAllAsync())
                    {
                        if (message == "close")
                        {
                            await socket.CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, "going away", default);
                            break;
                        }
                        await FakeHttpServer.SendTextAsync(socket, message);
                    }
                }
                catch (Exception)
                {
                    // client went away
                }
                await receive;
            };
        }

        public async Task DisposeAsync()
        {
            _socketMessages.Writer.TryComplete();
            await _server.DisposeAsync();
            _http.Dispose();
        }

        private static object MowerJson(string id, string name, string activity = "PARKED_IN_CS") => new
        {
            type = "mower",
            id,
            attributes = new
            {
                system = new { name, model = "HUSQVARNA AUTOMOWER® 430X", serialNumber = 123456789 },
                battery = new { batteryPercent = 88 },
                capabilities = new { headlights = true, workAreas = false, position = true, stayOutZones = false },
                mower = new { mode = "MAIN_AREA", activity, inactiveReason = "NONE", state = "RESTRICTED", errorCode = 0, errorCodeTimestamp = 0, isErrorConfirmable = false },
                calendar = new { tasks = Array.Empty<object>() },
                planner = new { nextStartTimestamp = 1762596000000L, @override = new { action = "NOT_ACTIVE" }, restrictedReason = "WEEK_SCHEDULE" },
                metadata = new { connected = true, statusTimestamp = 1762590000000L },
                positions = new[] { new { latitude = 48.30, longitude = 14.28 }, new { latitude = 48.31, longitude = 14.29 } },
                settings = new { cuttingHeight = 5, headlight = new { mode = "EVENING_ONLY" } },
                statistics = new { cuttingBladeUsageTime = 36000, numberOfChargingCycles = 120, numberOfCollisions = 500, totalChargingTime = 400000, totalCuttingTime = 720000, totalRunningTime = 900000, totalSearchingTime = 10000 }
            }
        };

        private (int, string) Handle(RecordedRequest request)
        {
            if (request.Path == "/oauth2/token")
            {
                var form = QueryHelpers.ParseQuery("?" + request.Body);
                if (form["grant_type"] != "client_credentials" || form["client_id"] != AppKey || form["client_secret"] != AppSecret)
                {
                    return (401, "{\"error\":\"invalid_client\",\"error_description\":\"Invalid client\"}");
                }
                _tokens++;
                return (201, JsonSerializer.Serialize(new { access_token = $"access-{_tokens}", scope = "iam:read amc:api", expires_in = 3600, provider = "husqvarna", user_id = "u1", token_type = "Bearer" }));
            }
            if (!request.Headers.TryGetValue("Authorization", out var auth) || !auth.StartsWith("Bearer access-") || request.Headers["X-Api-Key"] != AppKey ||
                request.Headers["Authorization-Provider"] != "husqvarna")
            {
                return (401, "{\"message\":\"Unauthorized\"}");
            }
            if (request.Method == "GET" && request.Path == "/v1/mowers")
            {
                return (200, JsonSerializer.Serialize(new { data = new[] { MowerJson(MowerId, "Lawny"), MowerJson(OtherMowerId, "Backyard", "MOWING") } }));
            }
            if (request.Method == "POST" && request.Path.StartsWith("/v1/mowers/"))
            {
                Assert.StartsWith(AutomowerApi.MediaType, request.Headers["Content-Type"]);
                var parts = request.Path.Split('/');
                var data = JsonNode.Parse(request.Body)!["data"]!;
                if (parts[^1] == "actions")
                {
                    var duration = data["attributes"]?["duration"];
                    _commands.Enqueue($"{parts[3]} {data["type"]}" + (duration != null ? $" {duration}" : ""));
                }
                else
                {
                    Assert.Equal("settings", data["type"]!.GetValue<string>());
                    _commands.Enqueue($"{parts[3]} settings {data["attributes"]!.ToJsonString()}");
                }
                return (202, "");
            }
            return (404, "{\"errors\":[{\"status\":\"404\"}]}");
        }

        private NodeInstance CreateAccount()
        {
            var node = CreateNodeInstance(DriverFactory.DriverGuid);
            node.SetProperty(AutomowerDriverFactory.AuthUrlKey, _server.Url + "/oauth2/token");
            node.SetProperty(AutomowerDriverFactory.ApiUrlKey, _server.Url + "/v1");
            node.SetProperty(AutomowerDriverFactory.WebSocketUrlKey, _server.Url.Replace("http://", "ws://") + "/ws");
            node.SetProperty(AutomowerDriverFactory.ApplicationKeyKey, AppKey);
            node.SetProperty(AutomowerDriverFactory.ApplicationSecretKey, AppSecret);
            _status = CreateNodeInstance(AutomowerDriverFactory.StatusTemplate);
            _socketConnected = CreateNodeInstance(AutomowerDriverFactory.SocketConnectedTemplate);
            node.InverseThis2ParentNodeInstanceNavigation.Add(_status);
            node.InverseThis2ParentNodeInstanceNavigation.Add(_socketConnected);
            return node;
        }

        private Dictionary<string, NodeInstance> AddMower(NodeInstance account, string mowerId, int? minutes = null)
        {
            var mower = CreateNodeInstance(AutomowerDriverFactory.MowerTemplate);
            mower.SetProperty(AutomowerDriverFactory.MowerIdKey, mowerId);
            if (minutes.HasValue)
            {
                mower.SetProperty(AutomowerDriverFactory.DefaultMinutesKey, minutes.Value);
            }
            account.InverseThis2ParentNodeInstanceNavigation.Add(mower);
            var values = new Dictionary<string, NodeInstance>();
            foreach (var dp in AutomowerDataPoints.All)
            {
                var child = CreateNodeInstance(dp.Template);
                mower.InverseThis2ParentNodeInstanceNavigation.Add(child);
                values[dp.Key] = child;
            }
            return values;
        }

        private async Task<AutomowerDriver> CreateAutomowerAsync(NodeInstance account)
        {
            var driver = (AutomowerDriver)await CreateDriver(account);
            driver.Http = _http;
            driver.MinReconnectDelay = TimeSpan.FromMilliseconds(50);
            await driver.Init();
            return driver;
        }

        private object Value(NodeInstance node) => Dispatcher.GetValue(DispatchableType.NodeInstance, node.ObjId)?.Value;

        private Task WriteAsync(AutomowerDriver driver, string key, object value) =>
            driver.Mowers.Single(m => m.MowerId == MowerId).Values.Single(v => v.Key == key).WriteDirectAsync(value);

        private static string Event(string type, object attributes) => JsonSerializer.Serialize(new { id = MowerId, type, attributes });

        [Fact]
        public async Task Poll_DispatchesAllValues()
        {
            var account = CreateAccount();
            var mower = AddMower(account, MowerId);
            var driver = await CreateAutomowerAsync(account);

            await driver.PollAsync();

            Assert.Equal(true, Value(mower["connected"]));
            Assert.Equal(88, Value(mower["battery"]));
            Assert.Equal("MAIN_AREA", Value(mower["mode"]));
            Assert.Equal("PARKED_IN_CS", Value(mower["activity"]));
            Assert.Equal("RESTRICTED", Value(mower["state"]));
            Assert.Equal("NONE", Value(mower["inactive_reason"]));
            Assert.Equal(0, Value(mower["error_code"]));
            Assert.Null(Value(mower["error_time"]));
            Assert.Equal(new DateTime(2025, 11, 8, 10, 0, 0), Value(mower["next_start"]));
            Assert.Equal("NOT_ACTIVE", Value(mower["override_action"]));
            Assert.Equal("WEEK_SCHEDULE", Value(mower["restricted_reason"]));
            Assert.Equal(false, Value(mower["mowing"]));
            Assert.Equal(5, Value(mower["cutting_height"]));
            Assert.Equal("EVENING_ONLY", Value(mower["headlight"]));
            Assert.Equal(48.30, Value(mower["latitude"]));
            Assert.Equal(14.28, Value(mower["longitude"]));
            Assert.Equal(200d, Value(mower["cutting_time"]));
            Assert.Equal(250d, Value(mower["running_time"]));
            Assert.Equal(10d, Value(mower["blade_time"]));
            Assert.Equal(120, Value(mower["charging_cycles"]));
            Assert.Equal(500, Value(mower["collisions"]));
        }

        [Fact]
        public async Task WebSocket_SendsTheBearerTokenAndMergesV2Events()
        {
            var account = CreateAccount();
            var mower = AddMower(account, MowerId);
            var driver = await CreateAutomowerAsync(account);

            await driver.Start();
            await FakeHttpServer.WaitUntilAsync(() => Equals(Value(_socketConnected), true));
            Assert.Equal("Bearer access-1", Assert.Single(_socketAuth));
            Assert.Equal("OK", Value(_status));

            await _socketMessages.Writer.WriteAsync(Event("battery-event-v2", new { battery = new { batteryPercent = 77 } }));
            await _socketMessages.Writer.WriteAsync(Event("position-event-v2", new { position = new { latitude = 48.5, longitude = 14.5 } }));
            await _socketMessages.Writer.WriteAsync(Event("cuttingHeight-event-v2", new { cuttingHeight = new { height = 7 } }));
            await _socketMessages.Writer.WriteAsync(Event("headlights-event-v2", new { headlight = new { mode = "ALWAYS_ON" } }));
            await _socketMessages.Writer.WriteAsync(Event("planner-event-v2", new { planner = new { nextStartTimestamp = 1762599600, @override = new { action = "FORCE_MOW" }, restrictedReason = "NONE" } }));
            await _socketMessages.Writer.WriteAsync(Event("message-event-v2", new { message = new { time = 1762599600, code = 1, severity = "WARNING" } }));
            await _socketMessages.Writer.WriteAsync(Event("mower-event-v2", new { mower = new { mode = "MAIN_AREA", activity = "MOWING", inactiveReason = "NONE", state = "IN_OPERATION", errorCode = 0, errorCodeTimestamp = 0 } }));
            await FakeHttpServer.WaitUntilAsync(() => Equals(Value(mower["activity"]), "MOWING"));

            Assert.Equal(true, Value(mower["mowing"]));
            Assert.Equal("IN_OPERATION", Value(mower["state"]));
            Assert.Equal(77, Value(mower["battery"]));
            Assert.Equal(48.5, Value(mower["latitude"]));
            Assert.Equal(7, Value(mower["cutting_height"]));
            Assert.Equal("ALWAYS_ON", Value(mower["headlight"]));
            Assert.Equal("FORCE_MOW", Value(mower["override_action"]));
            Assert.Equal(new DateTime(2025, 11, 8, 11, 0, 0), Value(mower["next_start"]));
            Assert.Equal(200d, Value(mower["cutting_time"])); // REST values the events don't carry stay

            await driver.Stop();
            Assert.Equal(false, Value(_socketConnected));
        }

        [Fact]
        public async Task WebSocket_SendsPingsAndReusesTheToken()
        {
            var account = CreateAccount();
            AddMower(account, MowerId);
            var driver = await CreateAutomowerAsync(account);
            driver.PingInterval = TimeSpan.FromMilliseconds(100);

            await driver.Start();
            await FakeHttpServer.WaitUntilAsync(() => _received.Count >= 2);
            await WriteAsync(driver, "pause", true);
            await driver.Stop();

            Assert.All(_received, r => Assert.Equal("ping", r));
            Assert.Equal(1, _tokens); // Husqvarna allows only 3 live tokens per application and user
        }

        [Fact]
        public async Task WebSocket_ClosedByServer_Reconnects()
        {
            var account = CreateAccount();
            AddMower(account, MowerId);
            var driver = await CreateAutomowerAsync(account);

            await driver.Start();
            await FakeHttpServer.WaitUntilAsync(() => _sockets == 1);
            await _socketMessages.Writer.WriteAsync("close");
            await FakeHttpServer.WaitUntilAsync(() => _sockets == 2);
            await driver.Stop();
        }

        [Fact]
        public async Task WebSocket_LifetimeReached_ReconnectsBeforeTheServerLimit()
        {
            var account = CreateAccount();
            AddMower(account, MowerId);
            var driver = await CreateAutomowerAsync(account);
            driver.SocketLifetime = TimeSpan.FromMilliseconds(300);

            await driver.Start();
            await FakeHttpServer.WaitUntilAsync(() => _sockets >= 2);
            await driver.Stop();
        }

        [Theory]
        [InlineData("mowing", true, "Start 45")]
        [InlineData("mowing", false, "ParkUntilNextSchedule")]
        [InlineData("mow_minutes", 90.4, "Start 90")]
        [InlineData("mow_minutes", 0, "ParkUntilNextSchedule")]
        [InlineData("park_minutes", 30, "Park 30")]
        [InlineData("park", true, "ParkUntilFurtherNotice")]
        [InlineData("pause", true, "Pause")]
        [InlineData("resume_schedule", true, "ResumeSchedule")]
        [InlineData("cutting_height", 12, "settings {\"cuttingHeight\":9}")]
        [InlineData("cutting_height", 3, "settings {\"cuttingHeight\":3}")]
        [InlineData("headlight", " always_on ", "settings {\"headlight\":{\"mode\":\"ALWAYS_ON\"}}")]
        public async Task Write_SendsTheMatchingAction(string key, object value, string expected)
        {
            var account = CreateAccount();
            AddMower(account, MowerId, 45);
            var driver = await CreateAutomowerAsync(account);

            await WriteAsync(driver, key, value);

            Assert.Equal(new[] { $"{MowerId} {expected}" }, _commands);
        }

        [Theory]
        [InlineData("park", false)]
        [InlineData("pause", false)]
        [InlineData("resume_schedule", false)]
        [InlineData("park_minutes", 0)]
        public async Task Write_MomentaryReleased_SendsNothing(string key, object value)
        {
            var account = CreateAccount();
            AddMower(account, MowerId);
            var driver = await CreateAutomowerAsync(account);

            await WriteAsync(driver, key, value);

            Assert.Empty(_commands);
            Assert.DoesNotContain(_server.Requests, r => r.Path.StartsWith("/v1/"));
        }

        [Fact]
        public async Task Write_DefaultMinutesNotSet_StartsForAnHour()
        {
            var account = CreateAccount();
            AddMower(account, MowerId);
            var driver = await CreateAutomowerAsync(account);

            await WriteAsync(driver, "mowing", true);

            Assert.Equal(new[] { $"{MowerId} Start 60" }, _commands);
        }

        [Fact]
        public async Task Write_Rejected_DoesNotThrow()
        {
            var account = CreateAccount();
            AddMower(account, MowerId);
            var driver = await CreateAutomowerAsync(account);
            _server.Handler = r => r.Path.EndsWith("/actions") ? (400, "{\"errors\":[{\"title\":\"Bad request\"}]}") : Handle(r);

            await WriteAsync(driver, "pause", true);
        }

        [Theory]
        [InlineData(429, "{\"message\":\"Too Many Requests\"}")]
        [InlineData(403, "{\"message\":\"Limit Exceeded\"}")]
        public async Task RateLimited_StopsSendingRequests(int status, string body)
        {
            var account = CreateAccount();
            AddMower(account, MowerId);
            var driver = await CreateAutomowerAsync(account);
            _server.Handler = r => r.Path.StartsWith("/v1/") ? (status, body) : Handle(r);

            await Assert.ThrowsAsync<RateLimitedException>(() => driver.PollAsync());
            var count = _server.Requests.Count;
            await Assert.ThrowsAsync<RateLimitedException>(() => driver.PollAsync());
            Assert.Equal(count, _server.Requests.Count);
            Assert.True(driver.Api.BlockedUntil > DateTimeOffset.UtcNow.AddMinutes(10));
        }

        [Fact]
        public async Task Forbidden_WithoutLimitMessage_IsAnError()
        {
            var account = CreateAccount();
            AddMower(account, MowerId);
            var driver = await CreateAutomowerAsync(account);
            _server.Handler = r => r.Path.StartsWith("/v1/") ? (403, "{\"message\":\"Application not connected to the Automower Connect API\"}") : Handle(r);

            var ex = await Assert.ThrowsAsync<AutomowerApiException>(() => driver.PollAsync());
            Assert.Contains("not connected", ex.Message);
            Assert.False(driver.Api.IsBlocked);
        }

        [Fact]
        public async Task ExpiredToken_IsRenewedOnce()
        {
            var account = CreateAccount();
            var mower = AddMower(account, MowerId);
            var driver = await CreateAutomowerAsync(account);
            var rejected = false;
            _server.Handler = r =>
            {
                if (!rejected && r.Path == "/v1/mowers")
                {
                    rejected = true;
                    return (401, "{\"message\":\"Unauthorized\"}");
                }
                return Handle(r);
            };

            await driver.PollAsync();

            Assert.Equal(2, _tokens);
            Assert.Equal(88, Value(mower["battery"]));
        }

        [Fact]
        public async Task Scan_ListsMowersNotConfiguredYet()
        {
            var account = CreateAccount();
            AddMower(account, MowerId);
            var driver = await CreateAutomowerAsync(account);

            var found = await driver.Scan();

            var backyard = Assert.Single(found);
            Assert.Equal("Backyard", backyard.Name);
            Assert.Equal(OtherMowerId, backyard.GetProperty(AutomowerDriverFactory.MowerIdKey).ValueString);
            Assert.Equal(AutomowerDataPoints.All.Count, backyard.InverseThis2ParentNodeInstanceNavigation.Count);
        }

        [Fact]
        public async Task Start_WrongSecret_ReportsTheStatus()
        {
            var account = CreateAccount();
            account.SetProperty(AutomowerDriverFactory.ApplicationSecretKey, "wrong");
            AddMower(account, MowerId);
            var driver = await CreateAutomowerAsync(account);

            await driver.Start();
            await FakeHttpServer.WaitUntilAsync(() => driver.Status?.Contains("invalid_client") == true);
            await driver.Stop();
        }

        [Fact]
        public void LocalTime_AcceptsSecondsAndMilliseconds()
        {
            var expected = new DateTime(2025, 11, 8, 10, 0, 0);
            Assert.Equal(expected, AutomowerDataPoints.LocalTime(new JsonObject { ["t"] = 1762596000 }, "t"));
            Assert.Equal(expected, AutomowerDataPoints.LocalTime(new JsonObject { ["t"] = 1762596000000L }, "t"));
            Assert.Null(AutomowerDataPoints.LocalTime(new JsonObject { ["t"] = 0 }, "t"));
            Assert.Equal(DateTimeKind.Unspecified, ((DateTime)AutomowerDataPoints.LocalTime(new JsonObject { ["t"] = 1762596000 }, "t")).Kind);
        }
    }
}
