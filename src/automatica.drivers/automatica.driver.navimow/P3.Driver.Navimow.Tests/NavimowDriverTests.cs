using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Microsoft.AspNetCore.WebUtilities;
using P3.Driver.Navimow.Driver;
using Xunit;

namespace P3.Driver.Navimow.Tests
{
    public class NavimowDriverTests : DriverFactoryTestBase<NavimowDriverFactory>, IAsyncLifetime
    {
        private const string ClientId = "automatica-test";
        private const string ClientSecret = "client-secret-456";
        private const string Code = "code-abc";
        private const string RedirectUri = "https://localhost/callback";
        private const string MowerId = "NM1234567890";
        private const string OtherMowerId = "NM0000000002";

        private FakeHttpServer _server;
        private readonly HttpClient _http = new();
        private readonly ConcurrentQueue<string> _grants = new();
        private readonly ConcurrentQueue<string> _commands = new();
        private int _tokens;
        private bool _issueRefreshToken = true;
        private int _expiresIn = 7200;
        private bool _wrapTokenResponse;
        private string _vehicleState = "isDocked";
        private NodeInstance _status;

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

        private static string Envelope(object payload) => JsonSerializer.Serialize(new { code = 1, desc = "success", data = new { payload } });

        private (int, string) Handle(RecordedRequest request)
        {
            if (request.Path == "/openapi/oauth/getAccessToken")
            {
                var form = QueryHelpers.ParseQuery("?" + request.Body);
                if (form["client_id"] != ClientId || form["client_secret"] != ClientSecret)
                {
                    return (401, "{\"error\":\"invalid_client\"}");
                }
                var grant = form["grant_type"].ToString();
                _grants.Enqueue(grant);
                switch (grant)
                {
                    case "authorization_code" when form["code"] == Code && form["redirect_uri"] == RedirectUri:
                    case "refresh_token" when form["refresh_token"].ToString().StartsWith("refresh-"):
                        break;
                    default:
                        return (400, "{\"error\":\"invalid_grant\",\"error_description\":\"code expired\"}");
                }
                _tokens++;
                var token = new Dictionary<string, object> { ["access_token"] = $"access-{_tokens}", ["token_type"] = "Bearer", ["expires_in"] = _expiresIn };
                if (_issueRefreshToken)
                {
                    token["refresh_token"] = $"refresh-{_tokens}";
                }
                return (200, _wrapTokenResponse ? JsonSerializer.Serialize(new { code = 1, desc = "success", data = token }) : JsonSerializer.Serialize(token));
            }
            if (!request.Headers.TryGetValue("Authorization", out var auth) || !auth.StartsWith("Bearer access-") ||
                !request.Headers.TryGetValue("requestId", out var requestId) || !Guid.TryParse(requestId, out _))
            {
                return (401, "{\"code\":401,\"desc\":\"unauthorized\"}");
            }
            switch (request.Method, request.Path)
            {
                case ("GET", "/openapi/smarthome/authList"):
                    return (200, Envelope(new
                    {
                        devices = new object[]
                        {
                            new { id = MowerId, name = "Front lawn", model = "i105E", firmware_version = "1.2.3", serial_number = "SN1", online = true },
                            new { id = OtherMowerId, name = "Back lawn", model = "X315", firmware_version = "2.0.0", serial_number = "SN2", online = false }
                        }
                    }));
                case ("POST", "/openapi/smarthome/getVehicleStatus"):
                {
                    var ids = JsonNode.Parse(request.Body)!["devices"]!.AsArray().Select(d => d!["id"]!.GetValue<string>()).ToList();
                    return (200, Envelope(new
                    {
                        devices = ids.Select(id => new
                        {
                            id,
                            vehicleState = _vehicleState,
                            capacityRemaining = new[] { new { rawValue = 87, unit = "PERCENTAGE" } },
                            descriptiveCapacityRemaining = "HIGH"
                        })
                    }));
                }
                case ("POST", "/openapi/smarthome/sendCommands"):
                {
                    var command = JsonNode.Parse(request.Body)!["commands"]![0]!;
                    var device = command["devices"]![0]!["id"]!.GetValue<string>();
                    var execution = command["execution"]!;
                    _commands.Enqueue($"{device} {execution["command"]} {execution["params"]!.ToJsonString()}");
                    return (200, Envelope(new { commands = new[] { new { ids = new[] { device }, status = "PENDING" } } }));
                }
            }
            return (404, "{\"code\":404,\"desc\":\"not found\"}");
        }

        private NodeInstance CreateAccount(string code = Code, string refreshToken = "")
        {
            var node = CreateNodeInstance(DriverFactory.DriverGuid);
            node.SetProperty(NavimowDriverFactory.TokenUrlKey, _server.Url + "/openapi/oauth/getAccessToken");
            node.SetProperty(NavimowDriverFactory.ApiUrlKey, _server.Url);
            node.SetProperty(NavimowDriverFactory.ClientIdKey, ClientId);
            node.SetProperty(NavimowDriverFactory.ClientSecretKey, ClientSecret);
            node.SetProperty(NavimowDriverFactory.RedirectUriKey, RedirectUri);
            node.SetProperty(NavimowDriverFactory.AuthorizationCodeKey, code);
            node.SetProperty(NavimowDriverFactory.RefreshTokenKey, refreshToken);
            _status = CreateNodeInstance(NavimowDriverFactory.StatusTemplate);
            node.InverseThis2ParentNodeInstanceNavigation.Add(_status);
            return node;
        }

        private Dictionary<string, NodeInstance> AddMower(NodeInstance account, string deviceId = MowerId)
        {
            var mower = CreateNodeInstance(NavimowDriverFactory.MowerTemplate);
            mower.SetProperty(NavimowDriverFactory.DeviceIdKey, deviceId);
            account.InverseThis2ParentNodeInstanceNavigation.Add(mower);
            var values = new Dictionary<string, NodeInstance>();
            foreach (var dp in NavimowDataPoints.All)
            {
                var child = CreateNodeInstance(dp.Template);
                mower.InverseThis2ParentNodeInstanceNavigation.Add(child);
                values[dp.Key] = child;
            }
            return values;
        }

        private async Task<NavimowDriver> CreateNavimowAsync(NodeInstance account)
        {
            var driver = (NavimowDriver)await CreateDriver(account);
            driver.Http = _http;
            driver.CommandRefreshDelay = TimeSpan.Zero;
            await driver.Init();
            return driver;
        }

        private object Value(NodeInstance node) => Dispatcher.GetValue(DispatchableType.NodeInstance, node.ObjId)?.Value;

        private Task WriteAsync(NavimowDriver driver, string key, object value) =>
            driver.Mowers.Single(m => m.DeviceId == MowerId).Values.Single(v => v.Key == key).WriteDirectAsync(value);

        private static string TokenState(NodeInstance account) => account.GetProperty(NavimowDriverFactory.TokenStateKey)?.ValueString;

        [Fact]
        public async Task Poll_ExchangesTheCodeAndDispatchesTheStatus()
        {
            var account = CreateAccount();
            var mower = AddMower(account);
            var driver = await CreateNavimowAsync(account);

            await driver.PollAsync();

            Assert.Equal(new[] { "authorization_code" }, _grants);
            Assert.Equal("OK", Value(_status));
            Assert.Equal(true, Value(mower["online"]));
            Assert.Equal("isDocked", Value(mower["state"]));
            Assert.Equal(true, Value(mower["docked"]));
            Assert.Equal(false, Value(mower["mowing"]));
            Assert.Equal(false, Value(mower["paused"]));
            Assert.Equal(false, Value(mower["error"]));
            Assert.Equal(87, Value(mower["battery"]));
            Assert.Equal("1.2.3", Value(mower["firmware"]));

            var state = TokenState(account);
            Assert.Contains("refresh-1", state);
            Assert.DoesNotContain(Code, state);
        }

        [Fact]
        public async Task Poll_OneStatusRequestForAllMowers_DeviceListOnlyOncePerHour()
        {
            var account = CreateAccount();
            AddMower(account);
            AddMower(account, OtherMowerId);
            var driver = await CreateNavimowAsync(account);

            await driver.PollAsync();
            await driver.PollAsync();

            Assert.Equal(2, _server.Requests.Count(r => r.Path == "/openapi/smarthome/getVehicleStatus"));
            Assert.Single(_server.Requests, r => r.Path == "/openapi/smarthome/authList");
            var body = JsonNode.Parse(_server.Requests.First(r => r.Path == "/openapi/smarthome/getVehicleStatus").Body)!;
            Assert.Equal(2, body["devices"]!.AsArray().Count);
        }

        [Fact]
        public async Task Restart_UsesTheStoredLoginAndDoesNotResendTheCode()
        {
            var account = CreateAccount();
            AddMower(account);
            var first = await CreateNavimowAsync(account);
            await first.PollAsync();

            var second = await CreateNavimowAsync(account);
            await second.PollAsync();

            Assert.Equal(new[] { "authorization_code" }, _grants); // the stored access token is still valid
            Assert.Equal("OK", second.Status);
        }

        [Fact]
        public async Task NewCodeEntered_ReplacesTheStoredLogin()
        {
            var account = CreateAccount();
            AddMower(account);
            await (await CreateNavimowAsync(account)).PollAsync();

            account.SetProperty(NavimowDriverFactory.AuthorizationCodeKey, "code-new");
            var driver = await CreateNavimowAsync(account);
            await driver.PollAsync();

            Assert.Equal(new[] { "authorization_code", "authorization_code" }, _grants);
            Assert.StartsWith("Login failed", driver.Status); // the fake only knows Code
        }

        [Fact]
        public async Task RejectedCode_IsNotSentAgain()
        {
            var account = CreateAccount("code-wrong");
            AddMower(account);
            var driver = await CreateNavimowAsync(account);

            await driver.PollAsync();
            await driver.PollAsync();

            Assert.Single(_grants);
            Assert.StartsWith("Login required", driver.Status);
            Assert.Contains("client_id=" + ClientId, driver.Status);
            Assert.Contains("redirect_uri=" + Uri.EscapeDataString(RedirectUri), driver.Status);
            Assert.DoesNotContain(_server.Requests, r => r.Path.StartsWith("/openapi/smarthome"));

            var restarted = await CreateNavimowAsync(account);
            await restarted.PollAsync();
            Assert.Single(_grants);
        }

        [Fact]
        public async Task NoLogin_ShowsTheLoginUrl()
        {
            var account = CreateAccount("");
            AddMower(account);
            var driver = await CreateNavimowAsync(account);

            await driver.PollAsync();

            Assert.Empty(_server.Requests);
            Assert.Equal($"Login required: open {NavimowDriverFactory.DefaultLoginUrl}?response_type=code&client_id={ClientId}&state=automatica&redirect_uri={Uri.EscapeDataString(RedirectUri)} and enter the code from the address you are sent to",
                Value(_status));
        }

        [Fact]
        public async Task NoRefreshToken_AccessTokenExpires_AsksForANewLogin()
        {
            _issueRefreshToken = false;
            _expiresIn = 1;
            var account = CreateAccount();
            AddMower(account);
            var driver = await CreateNavimowAsync(account);

            await driver.PollAsync();
            Assert.Equal("OK", driver.Status);
            await Task.Delay(1200);
            var count = _server.Requests.Count;
            await driver.PollAsync();

            Assert.StartsWith("Login required", driver.Status);
            Assert.Equal(count, _server.Requests.Count);
        }

        [Fact]
        public async Task ConfiguredRefreshToken_IsRotatedAndStored()
        {
            var account = CreateAccount("", "refresh-configured");
            AddMower(account);
            var driver = await CreateNavimowAsync(account);

            await driver.PollAsync();

            Assert.Equal(new[] { "refresh_token" }, _grants);
            Assert.Contains("refresh-1", TokenState(account));
            Assert.Equal("OK", driver.Status);
        }

        [Fact]
        public async Task WrappedTokenResponse_IsAccepted()
        {
            _wrapTokenResponse = true;
            var account = CreateAccount();
            var mower = AddMower(account);
            var driver = await CreateNavimowAsync(account);

            await driver.PollAsync();

            Assert.Equal(87, Value(mower["battery"]));
        }

        [Fact]
        public async Task Unauthorized_RefreshesOnceAndRetries()
        {
            var account = CreateAccount();
            var mower = AddMower(account);
            var driver = await CreateNavimowAsync(account);
            await driver.PollAsync();
            var rejected = false;
            _server.Handler = r =>
            {
                if (!rejected && r.Path == "/openapi/smarthome/getVehicleStatus")
                {
                    rejected = true;
                    return (401, "{\"code\":401,\"desc\":\"token expired\"}");
                }
                return Handle(r);
            };
            _vehicleState = "isRunning";

            await driver.PollAsync();

            Assert.Equal(new[] { "authorization_code", "refresh_token" }, _grants);
            Assert.Equal(true, Value(mower["mowing"]));
        }

        [Theory]
        [InlineData("isRunning", "mowing", true)]
        [InlineData("isPaused", "paused", true)]
        [InlineData("isDocking", "returning", true)]
        [InlineData("isLifted", "lifted", true)]
        [InlineData("Error", "error", true)]
        [InlineData("error", "error", true)]
        [InlineData("Offline", "online", false)]
        public async Task States_AreMapped(string vehicleState, string key, bool expected)
        {
            _vehicleState = vehicleState;
            var account = CreateAccount();
            var mower = AddMower(account);
            var driver = await CreateNavimowAsync(account);

            await driver.PollAsync();

            Assert.Equal(expected, Value(mower[key]));
            Assert.Equal(vehicleState, Value(mower["state"]));
        }

        [Theory]
        [InlineData("mowing", true, "action.devices.commands.StartStop {\"on\":true}")]
        [InlineData("mowing", false, "action.devices.commands.Dock {}")]
        [InlineData("paused", true, "action.devices.commands.PauseUnpause {\"on\":false}")]
        [InlineData("paused", false, "action.devices.commands.PauseUnpause {\"on\":true}")]
        [InlineData("dock", true, "action.devices.commands.Dock {}")]
        [InlineData("stop", true, "action.devices.commands.StartStop {\"on\":false}")]
        public async Task Write_SendsTheMatchingCommandAndRefreshes(string key, object value, string expected)
        {
            var account = CreateAccount();
            AddMower(account);
            var driver = await CreateNavimowAsync(account);

            await WriteAsync(driver, key, value);

            Assert.Equal(new[] { $"{MowerId} {expected}" }, _commands);
            await FakeHttpServer.WaitUntilAsync(() => _server.Requests.Any(r => r.Path == "/openapi/smarthome/getVehicleStatus"));
        }

        [Theory]
        [InlineData("dock")]
        [InlineData("stop")]
        public async Task Write_MomentaryReleased_SendsNothing(string key)
        {
            var account = CreateAccount();
            AddMower(account);
            var driver = await CreateNavimowAsync(account);

            await WriteAsync(driver, key, false);

            Assert.Empty(_commands);
            Assert.Empty(_server.Requests);
        }

        [Fact]
        public async Task Command_AlreadyInState_IsSuccess_OtherErrorsThrow()
        {
            var account = CreateAccount();
            AddMower(account);
            var driver = await CreateNavimowAsync(account);
            var errorCode = "alreadyInState";
            _server.Handler = r => r.Path == "/openapi/smarthome/sendCommands"
                ? (200, Envelope(new { commands = new[] { new { ids = new[] { MowerId }, status = "ERROR", errorCode } } }))
                : Handle(r);
            await driver.EnsureLoginAsync();

            await driver.Api.SendCommandAsync(MowerId, NavimowApi.Dock, null);
            errorCode = "deviceOffline";
            var ex = await Assert.ThrowsAsync<NavimowApiException>(() => driver.Api.SendCommandAsync(MowerId, NavimowApi.Dock, null));
            Assert.Contains("deviceOffline", ex.Message);
            await WriteAsync(driver, "dock", true); // logged, not thrown
        }

        [Theory]
        [InlineData(429, "{}")]
        [InlineData(200, "{\"code\":-1,\"desc\":\"Request too frequent\"}")]
        public async Task RateLimited_StopsSendingRequests(int status, string body)
        {
            var account = CreateAccount();
            AddMower(account);
            var driver = await CreateNavimowAsync(account);
            _server.Handler = r => r.Path.StartsWith("/openapi/smarthome/") ? (status, body) : Handle(r);

            await Assert.ThrowsAsync<RateLimitedException>(() => driver.PollAsync());
            var count = _server.Requests.Count;
            await driver.PollAsync();

            Assert.Equal(count, _server.Requests.Count);
            Assert.True(driver.Api.BlockedUntil > DateTimeOffset.UtcNow.AddMinutes(4));
            Assert.Contains("too frequent", driver.Status);
        }

        [Fact]
        public async Task EnvelopeError_IsReported()
        {
            var account = CreateAccount();
            AddMower(account);
            var driver = await CreateNavimowAsync(account);
            _server.Handler = r => r.Path == "/openapi/smarthome/authList" ? (200, "{\"code\":20001,\"desc\":\"device not bound\"}") : Handle(r);

            await Assert.ThrowsAsync<NavimowApiException>(() => driver.PollAsync());

            Assert.Contains("device not bound", driver.Status);
            Assert.False(driver.Api.IsBlocked);
        }

        [Fact]
        public async Task Scan_ListsMowersNotConfiguredYet()
        {
            var account = CreateAccount();
            AddMower(account);
            var driver = await CreateNavimowAsync(account);

            var found = await driver.Scan();

            var back = Assert.Single(found);
            Assert.Equal("Back lawn", back.Name);
            Assert.Equal(OtherMowerId, back.GetProperty(NavimowDriverFactory.DeviceIdKey).ValueString);
            Assert.Equal(NavimowDataPoints.All.Count, back.InverseThis2ParentNodeInstanceNavigation.Count);
        }

        [Fact]
        public async Task Start_PollsImmediately()
        {
            var account = CreateAccount();
            var mower = AddMower(account);
            var driver = await CreateNavimowAsync(account);

            await driver.Start();
            await FakeHttpServer.WaitUntilAsync(() => Equals(Value(mower["battery"]), 87));
            await driver.Stop();
        }
    }
}
