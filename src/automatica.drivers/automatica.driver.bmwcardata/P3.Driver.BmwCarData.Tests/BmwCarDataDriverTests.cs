using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Microsoft.AspNetCore.WebUtilities;
using P3.Driver.BmwCarData.Driver;
using Xunit;

namespace P3.Driver.BmwCarData.Tests
{
    internal sealed class FakeStream : IBmwStream
    {
        private readonly TaskCompletionSource _disconnected = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public BmwStreamOptions Options { get; private set; }
        public Func<string, byte[], Task> OnMessage { get; private set; }
        public Task Disconnected => _disconnected.Task;

        public Task ConnectAsync(BmwStreamOptions options, Func<string, byte[], Task> onMessage, CancellationToken token)
        {
            Options = options;
            OnMessage = onMessage;
            return Task.CompletedTask;
        }

        public void CloseFromServer() => _disconnected.TrySetResult();

        public Task DisconnectAsync()
        {
            _disconnected.TrySetResult();
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    public class BmwCarDataDriverTests : DriverFactoryTestBase<BmwCarDataDriverFactory>, IAsyncLifetime
    {
        private const string ClientId = "6f1c3a52-1111-4222-8333-944455556666";
        private const string Vin = "WBA11AA0000000001";
        private const string OtherVin = "WMW22BB0000000002";
        private const string Gcid = "a1b2c3d4-0000-4000-8000-000000000001";

        private FakeHttpServer _server;
        private readonly HttpClient _http = new();
        private readonly ConcurrentQueue<string> _grants = new();
        private readonly ConcurrentQueue<FakeStream> _streams = new();
        private readonly List<string> _containers = new();
        private string _challenge;
        private int _pending = 1;
        private int _tokens;
        private string _containerError;

        private NodeInstance _loginStatus;
        private NodeInstance _streamConnected;

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

        private static string Verifier2Challenge(string verifier) => BmwCarDataDriver.CodeChallenge(verifier);

        private string Token()
        {
            _tokens++;
            return JsonSerializer.Serialize(new
            {
                access_token = $"access-{_tokens}", token_type = "Bearer", expires_in = 3599, refresh_token = $"refresh-{_tokens}",
                scope = BmwCarDataDriver.Scopes, id_token = $"id-{_tokens}", gcid = Gcid
            });
        }

        private (int, string) Handle(RecordedRequest request)
        {
            var form = QueryHelpers.ParseQuery("?" + request.Body);
            switch (request.Path)
            {
                case "/gcdm/oauth/device/code":
                    Assert.Equal(ClientId, form["client_id"]);
                    Assert.Equal("device_code", form["response_type"]);
                    Assert.Equal(BmwCarDataDriver.Scopes, form["scope"]);
                    Assert.Equal("S256", form["code_challenge_method"]);
                    _challenge = form["code_challenge"];
                    return (200, JsonSerializer.Serialize(new { user_code = "ABCD-1234", device_code = "device-1", verification_uri = "https://customer.bmwgroup.com/oneid/link", interval = 5, expires_in = 300 }));
                case "/gcdm/oauth/token":
                {
                    var grant = form["grant_type"].ToString();
                    _grants.Enqueue(grant);
                    Assert.Equal(ClientId, form["client_id"]);
                    Assert.False(form.ContainsKey("client_secret"));
                    if (grant == "urn:ietf:params:oauth:grant-type:device_code")
                    {
                        Assert.Equal("device-1", form["device_code"]);
                        Assert.Equal(_challenge, Verifier2Challenge(form["code_verifier"]));
                        if (_pending-- > 0)
                        {
                            return (403, "{\"error\":\"authorization_pending\"}");
                        }
                        return (200, Token());
                    }
                    if (grant == "refresh_token" && form["refresh_token"].ToString().StartsWith("refresh-"))
                    {
                        return (200, Token());
                    }
                    return (400, "{\"error\":\"invalid_grant\"}");
                }
            }

            if (!request.Headers.TryGetValue("Authorization", out var auth) || !auth.StartsWith("Bearer access-"))
            {
                return (401, "{\"exveErrorId\":\"CU-101\",\"exveErrorMsg\":\"Invalid token\"}");
            }
            Assert.Equal("v1", request.Headers["x-version"]);
            switch (request.Method, request.Path)
            {
                case ("GET", "/customers/vehicles/mappings"):
                    return (200, JsonSerializer.Serialize(new[]
                    {
                        new { vin = Vin, mappedSince = "2026-01-01T00:00:00Z", mappingType = "PRIMARY" },
                        new { vin = OtherVin, mappedSince = "2026-01-01T00:00:00Z", mappingType = "PRIMARY" }
                    }));
                case ("GET", "/customers/vehicles/" + OtherVin + "/basicData"):
                    return (200, JsonSerializer.Serialize(new { brand = "MINI", modelName = "Cooper SE" }));
                case ("GET", "/customers/containers"):
                    return (200, JsonSerializer.Serialize(new
                    {
                        containers = new[]
                        {
                            new { containerId = "old000000001", name = "Automatica", purpose = "x", state = "ACTIVE", created = "2026-01-01" },
                            new { containerId = "foreign00001", name = "evcc", purpose = "x", state = "ACTIVE", created = "2026-01-01" },
                            new { containerId = "old000000002", name = "Automatica", purpose = "x", state = "DELETED", created = "2026-01-01" }
                        }
                    }));
                case ("POST", "/customers/containers"):
                {
                    if (_containerError != null)
                    {
                        var error = _containerError;
                        _containerError = null;
                        return (403, JsonSerializer.Serialize(new { exveErrorId = error, exveErrorMsg = "Container limit reached" }));
                    }
                    var body = JsonNode.Parse(request.Body)!;
                    Assert.Equal("Automatica", body["name"]!.GetValue<string>());
                    var id = $"cont{_containers.Count(c => c.StartsWith("cont")) + 1:D9}";
                    _containers.Add(id + " " + string.Join(",", body["technicalDescriptors"]!.AsArray().Select(d => d!.GetValue<string>())));
                    return (201, JsonSerializer.Serialize(new { containerId = id, name = "Automatica", state = "ACTIVE" }));
                }
            }
            if (request.Method == "DELETE" && request.Path.StartsWith("/customers/containers/"))
            {
                _containers.Add("deleted " + request.Path["/customers/containers/".Length..]);
                return (204, "");
            }
            if (request.Method == "GET" && request.Path == $"/customers/vehicles/{Vin}/telematicData")
            {
                Assert.StartsWith("?containerId=cont", request.Query);
                return (200, JsonSerializer.Serialize(new
                {
                    telematicData = new Dictionary<string, object>
                    {
                        ["vehicle.drivetrain.batteryManagement.header"] = new { value = "64", unit = "%", timestamp = "2026-11-08T08:00:00Z" },
                        ["vehicle.drivetrain.electricEngine.kombiRemainingElectricRange"] = new { value = "215", unit = "km", timestamp = "2026-11-08T08:00:00Z" },
                        ["vehicle.vehicle.travelledDistance"] = new { value = "12345.5", unit = "km", timestamp = "2026-11-08T07:00:00Z" },
                        ["vehicle.drivetrain.electricEngine.charging.status"] = new { value = "CHARGINGACTIVE", unit = (string)null, timestamp = "2026-11-08T08:05:00Z" },
                        ["vehicle.cabin.infotainment.navigation.currentLocation.latitude"] = new { value = "48.3069", unit = "degrees", timestamp = "2026-11-08T08:00:00Z" },
                        ["vehicle.cabin.infotainment.navigation.currentLocation.longitude"] = new { value = "14.2858", unit = "degrees", timestamp = "2026-11-08T08:00:00Z" },
                        ["vehicle.cabin.door.lock.status"] = new { value = "SECURED", unit = (string)null, timestamp = "2026-11-08T08:00:00Z" },
                        ["vehicle.cabin.climate.outsideTemperature"] = new { value = "7.5", unit = "°C", timestamp = "2026-11-08T08:00:00Z" }
                    }
                }));
            }
            return (404, "{\"exveErrorId\":\"CU-404\"}");
        }

        private NodeInstance CreateAccount(bool streaming = false)
        {
            var node = CreateNodeInstance(DriverFactory.DriverGuid);
            node.SetProperty(BmwCarDataDriverFactory.AuthUrlKey, _server.Url + "/gcdm/oauth");
            node.SetProperty(BmwCarDataDriverFactory.ApiUrlKey, _server.Url);
            node.SetProperty(BmwCarDataDriverFactory.ClientIdKey, ClientId);
            node.SetProperty(BmwCarDataDriverFactory.StreamingKey, streaming);
            node.SetProperty(BmwCarDataDriverFactory.MqttHostKey, "stream.example.test:9000");
            _loginStatus = CreateNodeInstance(BmwCarDataDriverFactory.LoginStatusTemplate);
            _streamConnected = CreateNodeInstance(BmwCarDataDriverFactory.StreamConnectedTemplate);
            node.InverseThis2ParentNodeInstanceNavigation.Add(_loginStatus);
            node.InverseThis2ParentNodeInstanceNavigation.Add(_streamConnected);
            return node;
        }

        private Dictionary<string, NodeInstance> AddVehicle(NodeInstance account, string vin = Vin, params (string Template, string Key)[] custom)
        {
            var vehicle = CreateNodeInstance(BmwCarDataDriverFactory.VehicleTemplate);
            vehicle.SetProperty(BmwCarDataDriverFactory.VinKey, vin);
            account.InverseThis2ParentNodeInstanceNavigation.Add(vehicle);
            var values = new Dictionary<string, NodeInstance>();
            foreach (var dp in BmwCarDataDataPoints.All.Where(d => !d.IsCustom || d.Key == BmwCarDataDataPoints.LastUpdateKey))
            {
                var child = CreateNodeInstance(dp.Template);
                vehicle.InverseThis2ParentNodeInstanceNavigation.Add(child);
                values[dp.Key] = child;
            }
            foreach (var (template, key) in custom)
            {
                var dp = BmwCarDataDataPoints.All.Single(d => d.Key == template);
                var child = CreateNodeInstance(dp.Template);
                child.SetProperty(BmwCarDataDriverFactory.CustomKeyKey, key);
                vehicle.InverseThis2ParentNodeInstanceNavigation.Add(child);
                values[key] = child;
            }
            return values;
        }

        private async Task<BmwCarDataDriver> CreateBmwAsync(NodeInstance account)
        {
            var driver = (BmwCarDataDriver)await CreateDriver(account);
            driver.Http = _http;
            driver.DeviceLoginInterval = TimeSpan.FromMilliseconds(20);
            driver.MinReconnectDelay = TimeSpan.FromMilliseconds(50);
            driver.StreamFactory = () =>
            {
                var stream = new FakeStream();
                _streams.Enqueue(stream);
                return stream;
            };
            await driver.Init();
            return driver;
        }

        private object Value(NodeInstance node) => Dispatcher.GetValue(DispatchableType.NodeInstance, node.ObjId)?.Value;

        private static string State(NodeInstance account) => account.GetProperty(BmwCarDataDriverFactory.StateKey)?.ValueString;

        private async Task<(NodeInstance Account, BmwCarDataDriver Driver)> LoggedInAsync(Func<NodeInstance, Task> addVehicles = null, bool streaming = false)
        {
            var account = CreateAccount(streaming);
            if (addVehicles != null)
            {
                await addVehicles(account);
            }
            else
            {
                AddVehicle(account);
            }
            var driver = await CreateBmwAsync(account);
            await driver.DeviceLoginAsync();
            return (account, driver);
        }

        [Fact]
        public async Task DeviceLogin_WithPkce_ShowsTheLinkAndStoresTheTokens()
        {
            var account = CreateAccount();
            AddVehicle(account);
            var driver = await CreateBmwAsync(account);

            var login = driver.DeviceLoginAsync();
            await FakeHttpServer.WaitUntilAsync(() => driver.Status?.StartsWith("Login required") == true || login.IsCompleted);
            Assert.Equal("Login required: open https://customer.bmwgroup.com/oneid/link?user_code=ABCD-1234 and confirm the code ABCD-1234", driver.Status);
            await login;

            Assert.Equal("OK", Value(_loginStatus));
            Assert.Equal(new[] { "urn:ietf:params:oauth:grant-type:device_code", "urn:ietf:params:oauth:grant-type:device_code" }, _grants);
            var state = JsonNode.Parse(State(account))!;
            Assert.Equal("refresh-1", state["Refresh"]!.GetValue<string>());
            Assert.Equal(Gcid, state["Gcid"]!.GetValue<string>());
        }

        [Fact]
        public async Task Restart_UsesTheStoredRefreshTokenAndStoresTheRotatedOne()
        {
            var (account, _) = await LoggedInAsync();

            var driver = await CreateBmwAsync(account);
            await driver.RefreshAllAsync();

            Assert.Equal("refresh_token", _grants.Last());
            Assert.Equal("refresh-2", JsonNode.Parse(State(account))!["Refresh"]!.GetValue<string>());
        }

        [Fact]
        public async Task OtherClientId_DiscardsTheStoredLogin()
        {
            var (account, _) = await LoggedInAsync();
            account.SetProperty(BmwCarDataDriverFactory.ClientIdKey, "00000000-0000-4000-8000-000000000000");

            var driver = await CreateBmwAsync(account);

            Assert.False(driver.Api.Tokens.HasCredentials);
        }

        [Fact]
        public async Task Refresh_CreatesTheContainerOnceAndDispatchesTheValues()
        {
            Dictionary<string, NodeInstance> vehicle = null;
            var (account, driver) = await LoggedInAsync(a =>
            {
                vehicle = AddVehicle(a, Vin, (BmwCarDataDataPoints.CustomTextKey, "vehicle.cabin.door.lock.status"),
                    (BmwCarDataDataPoints.CustomNumberKey, "vehicle.cabin.climate.outsideTemperature"));
                return Task.CompletedTask;
            });

            await driver.RefreshAllAsync();
            await driver.RefreshAllAsync();

            var container = Assert.Single(_containers);
            Assert.StartsWith("cont000000001 ", container);
            Assert.Contains("vehicle.drivetrain.batteryManagement.header", container);
            Assert.Contains("vehicle.cabin.door.lock.status", container);
            Assert.Contains("vehicle.cabin.climate.outsideTemperature", container);
            Assert.Equal(2, _server.Requests.Count(r => r.Path.EndsWith("/telematicData")));

            Assert.Equal(64d, Value(vehicle["soc"]));
            Assert.Equal(215d, Value(vehicle["electric_range"]));
            Assert.Equal(12345.5, Value(vehicle["mileage"]));
            Assert.Equal("CHARGINGACTIVE", Value(vehicle["charging_status"]));
            Assert.Equal(48.3069, Value(vehicle["latitude"]));
            Assert.Equal(14.2858, Value(vehicle["longitude"]));
            Assert.Equal(new DateTime(2026, 11, 8, 8, 5, 0, DateTimeKind.Utc), Value(vehicle["last_update"]));
            Assert.Equal("SECURED", Value(vehicle["vehicle.cabin.door.lock.status"]));
            Assert.Equal(7.5, Value(vehicle["vehicle.cabin.climate.outsideTemperature"]));
            Assert.Null(Value(vehicle["target_soc"]));
            Assert.Equal("OK", Value(_loginStatus));
            Assert.Equal("cont000000001", JsonNode.Parse(State(account))!["ContainerId"]!.GetValue<string>());
        }

        [Fact]
        public async Task ChangedKeys_ReplaceTheContainer()
        {
            var (account, driver) = await LoggedInAsync();
            await driver.RefreshAllAsync();

            var vehicleNode = account.InverseThis2ParentNodeInstanceNavigation.Single(n => n.This2NodeTemplate == BmwCarDataDriverFactory.VehicleTemplate);
            var custom = CreateNodeInstance(BmwCarDataDataPoints.All.Single(d => d.Key == BmwCarDataDataPoints.CustomTextKey).Template);
            custom.SetProperty(BmwCarDataDriverFactory.CustomKeyKey, "vehicle.cabin.door.lock.status");
            vehicleNode.InverseThis2ParentNodeInstanceNavigation.Add(custom);
            var restarted = await CreateBmwAsync(account);
            await restarted.RefreshAllAsync();

            Assert.Equal(3, _containers.Count);
            Assert.Equal("deleted cont000000001", _containers[1]);
            Assert.StartsWith("cont000000002 ", _containers[2]);
        }

        [Fact]
        public async Task ContainerLimit_RemovesOwnLeftoversAndRetries()
        {
            var (_, driver) = await LoggedInAsync();
            _containerError = "CU-124";

            await driver.RefreshAllAsync();

            Assert.Equal(new[] { "deleted old000000001" }, _containers.Where(c => c.StartsWith("deleted")));
            Assert.Single(_containers, c => c.StartsWith("cont"));
        }

        [Fact]
        public async Task ConfiguredContainer_IsUsedAsIs()
        {
            var (account, _) = await LoggedInAsync();
            account.SetProperty(BmwCarDataDriverFactory.ContainerIdKey, "contmine00001");
            var driver = await CreateBmwAsync(account);

            await driver.RefreshAllAsync();

            Assert.Empty(_containers);
            Assert.EndsWith("containerId=contmine00001", _server.Requests.Single(r => r.Path.EndsWith("/telematicData")).Query);
        }

        [Fact]
        public async Task ContainerGone_IsCreatedAgainNextTime()
        {
            var (account, driver) = await LoggedInAsync();
            await driver.RefreshAllAsync();
            _server.Handler = r => r.Path.EndsWith("/telematicData") ? (403, "{\"exveErrorId\":\"CU-105\",\"exveErrorMsg\":\"No permission for container\"}") : Handle(r);

            await Assert.ThrowsAsync<BmwCarDataApiException>(() => driver.RefreshAllAsync());
            Assert.Null(JsonNode.Parse(State(account))!["ContainerId"]);
            _server.Handler = Handle;
            await driver.RefreshAllAsync();

            Assert.Equal(2, _containers.Count(c => c.StartsWith("cont")));
        }

        [Fact]
        public async Task DailyLimit_FromTheServer_BlocksUntilMidnightUtc()
        {
            var (_, driver) = await LoggedInAsync();
            _server.Handler = r => r.Path.StartsWith("/customers") ? (429, "{\"exveErrorId\":\"CU-429\",\"exveErrorMsg\":\"Rate limit\"}") : Handle(r);

            await Assert.ThrowsAsync<RateLimitedException>(() => driver.RefreshAllAsync());
            var count = _server.Requests.Count;
            await Assert.ThrowsAsync<RateLimitedException>(() => driver.RefreshAllAsync());

            Assert.Equal(count, _server.Requests.Count);
            Assert.Equal(DateTime.UtcNow.Date.AddDays(1), driver.Api.BlockedUntil.UtcDateTime);
            Assert.Contains("daily request limit", driver.Status);
        }

        [Fact]
        public async Task DailyLimit_IsCountedLocally()
        {
            var (_, driver) = await LoggedInAsync();

            for (var i = 0; i < 25; i++)
            {
                await driver.RefreshAllAsync(); // container + 24 telematic requests
            }
            Assert.Equal(26, driver.Api.RequestsToday);
            for (var i = 0; i < 24; i++)
            {
                await driver.RefreshAllAsync();
            }
            Assert.Equal(BmwCarDataApi.DailyLimit, driver.Api.RequestsToday);
            var count = _server.Requests.Count;
            await Assert.ThrowsAsync<RateLimitedException>(() => driver.RefreshAllAsync());
            Assert.Equal(count, _server.Requests.Count);
        }

        [Theory]
        [InlineData(1, 900, 2160)]
        [InlineData(1, 3600, 3600)]
        [InlineData(3, 900, 6480)]
        public async Task PollInterval_StaysWithinTheDailyBudget(int vehicles, int configured, int expectedSeconds)
        {
            var account = CreateAccount();
            account.SetProperty(BmwCarDataDriverFactory.PollKey, configured);
            for (var i = 0; i < vehicles; i++)
            {
                AddVehicle(account, $"WBA11AA000000000{i}");
            }
            var driver = await CreateBmwAsync(account);

            Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), driver.CalculatePollInterval());
        }

        [Fact]
        public async Task Stream_ConnectsWithGcidAndIdToken_AndAppliesMessages()
        {
            Dictionary<string, NodeInstance> vehicle = null;
            var account = CreateAccount(streaming: true);
            vehicle = AddVehicle(account);
            var driver = await CreateBmwAsync(account);

            await driver.Start();
            await FakeHttpServer.WaitUntilAsync(() => Equals(Value(_streamConnected), true), 10000);
            var stream = _streams.Single();
            Assert.Equal("stream.example.test", stream.Options.Host);
            Assert.Equal(9000, stream.Options.Port);
            Assert.True(stream.Options.Tls);
            Assert.Equal(Gcid, stream.Options.UserName);
            Assert.Equal($"id-{_tokens}", stream.Options.Password);
            Assert.Equal(new[] { $"{Gcid}/{Vin}" }, stream.Options.Topics);
            await FakeHttpServer.WaitUntilAsync(() => Equals(Value(vehicle["soc"]), 64d)); // the initial REST read

            var message = JsonSerializer.Serialize(new
            {
                vin = Vin, entityId = "e1", topic = Vin, timestamp = "2026-11-08T09:00:00Z",
                data = new Dictionary<string, object>
                {
                    ["vehicle.drivetrain.batteryManagement.header"] = new { timestamp = "2026-11-08T09:00:00Z", value = 66, unit = "%" },
                    ["vehicle.drivetrain.electricEngine.charging.status"] = new { timestamp = "2026-11-08T09:00:00Z", value = "FINISHED_FULLY_CHARGED" }
                }
            });
            await stream.OnMessage($"{Gcid}/{Vin}", Encoding.UTF8.GetBytes(message));

            Assert.Equal(66d, Value(vehicle["soc"]));
            Assert.Equal("FINISHED_FULLY_CHARGED", Value(vehicle["charging_status"]));
            Assert.Equal(215d, Value(vehicle["electric_range"]));
            Assert.Equal(new DateTime(2026, 11, 8, 9, 0, 0, DateTimeKind.Utc), Value(vehicle["last_update"]));

            await stream.OnMessage($"{Gcid}/{Vin}", Encoding.UTF8.GetBytes("{\"data\":{\"vehicle.drivetrain.batteryManagement.header\":{\"timestamp\":\"2026-11-08T08:30:00Z\",\"value\":10}}}"));
            Assert.Equal(66d, Value(vehicle["soc"])); // older values don't win

            await driver.Stop();
            Assert.Equal(false, Value(_streamConnected));
        }

        [Fact]
        public async Task Stream_ClosedByBroker_ReconnectsWithAFreshIdToken()
        {
            var account = CreateAccount(streaming: true);
            AddVehicle(account);
            var driver = await CreateBmwAsync(account);
            driver.MinReconnectDelay = TimeSpan.FromMilliseconds(20);

            await driver.Start();
            await FakeHttpServer.WaitUntilAsync(() => _streams.Count == 1 && _streams.Single().Options != null, 10000);
            var first = _streams.Single();
            first.CloseFromServer();
            await FakeHttpServer.WaitUntilAsync(() => _streams.Count == 2 && _streams.Last().Options != null);
            await driver.Stop();

            Assert.NotEqual(first.Options.Password, _streams.Last().Options.Password);
        }

        [Fact]
        public async Task Stream_TokenLifetimeReached_Reconnects()
        {
            var account = CreateAccount(streaming: true);
            AddVehicle(account);
            var driver = await CreateBmwAsync(account);
            driver.StreamTokenLifetime = TimeSpan.FromMilliseconds(300);

            await driver.Start();
            await FakeHttpServer.WaitUntilAsync(() => _streams.Count >= 2, 10000);
            await driver.Stop();
        }

        [Fact]
        public async Task ExpiredLogin_StartsANewDeviceLogin()
        {
            var (account, _) = await LoggedInAsync(streaming: false);
            var state = JsonNode.Parse(State(account))!;
            state["Refresh"] = "revoked";
            account.SetProperty(BmwCarDataDriverFactory.StateKey, state.ToJsonString());
            account.SetProperty(BmwCarDataDriverFactory.StreamingKey, true);
            _pending = 100;
            var driver = await CreateBmwAsync(account);

            await driver.Start();
            await FakeHttpServer.WaitUntilAsync(() => driver.Status?.StartsWith("Login required") == true, 10000);
            await driver.Stop();

            Assert.Contains("refresh_token", _grants);
        }

        [Fact]
        public async Task Scan_ListsMappedVehiclesNotConfiguredYet()
        {
            var (_, driver) = await LoggedInAsync();

            var found = await driver.Scan();

            var mini = Assert.Single(found);
            Assert.Equal("MINI Cooper SE", mini.Name);
            Assert.Equal(OtherVin, mini.GetProperty(BmwCarDataDriverFactory.VinKey).ValueString);
            Assert.Equal(BmwCarDataDataPoints.All.Count(d => d.DefaultCreated), mini.InverseThis2ParentNodeInstanceNavigation.Count);
        }
    }
}
