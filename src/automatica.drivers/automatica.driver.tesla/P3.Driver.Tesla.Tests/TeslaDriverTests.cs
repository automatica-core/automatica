using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Microsoft.AspNetCore.WebUtilities;
using P3.Driver.Tesla.Driver;
using Xunit;

namespace P3.Driver.Tesla.Tests
{
    public class TeslaDriverTests : DriverFactoryTestBase<TeslaDriverFactory>, IAsyncLifetime
    {
        private const string Vin = "5YJ3E7EB0KF000001";
        private const string ClientId = "client-123";
        private const string RefreshToken = "EU_refresh_1";

        private FakeHttpServer _server;
        private readonly HttpClient _http = new();

        private string _vehicleState = "online";
        private int _tokenCounter;
        private readonly List<string> _commands = new();
        private object _vehicleData = DefaultVehicleData();

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

        private static object DefaultVehicleData() => new
        {
            id = 1,
            vin = Vin,
            state = "online",
            charge_state = new { battery_level = 81, battery_range = 200.0, charging_state = "Charging", charge_limit_soc = 90, charger_power = 11, time_to_full_charge = 1.25, charge_port_door_open = true },
            climate_state = new { inside_temp = 21.5, outside_temp = 4.0, is_climate_on = false, driver_temp_setting = 21.0 },
            drive_state = new { latitude = 48.3069, longitude = 14.2858, shift_state = (string)null },
            vehicle_state = new { locked = true, sentry_mode = false, odometer = 1000.0 }
        };

        private (int, string) Handle(RecordedRequest request)
        {
            if (request.Path == "/oauth2/v3/token")
            {
                var form = QueryHelpers.ParseQuery("?" + request.Body);
                if (form["refresh_token"] != (_tokenCounter == 0 ? RefreshToken : $"EU_refresh_{_tokenCounter + 1}"))
                {
                    return (401, "{\"error\":\"login_required\"}");
                }
                _tokenCounter++;
                return (200, JsonSerializer.Serialize(new { access_token = $"access-{_tokenCounter}", refresh_token = $"EU_refresh_{_tokenCounter + 1}", expires_in = 28800, token_type = "Bearer" }));
            }

            if (!request.Headers.TryGetValue("Authorization", out var auth) || !auth.StartsWith("Bearer access-"))
            {
                return (401, "{}");
            }

            if (request.Path == $"/api/1/vehicles/{Vin}")
            {
                return (200, JsonSerializer.Serialize(new { response = new { id = 1, vin = Vin, state = _vehicleState } }));
            }
            if (request.Path == $"/api/1/vehicles/{Vin}/vehicle_data")
            {
                return _vehicleState == "online" ? (200, JsonSerializer.Serialize(new { response = _vehicleData })) : (408, "{\"error\":\"vehicle unavailable\"}");
            }
            if (request.Path == $"/api/1/vehicles/{Vin}/wake_up")
            {
                _vehicleState = "online";
                return (200, JsonSerializer.Serialize(new { response = new { state = "online" } }));
            }
            if (request.Path.StartsWith($"/api/1/vehicles/{Vin}/command/"))
            {
                if (_vehicleState != "online")
                {
                    return (408, "{}");
                }
                _commands.Add(request.Path.Split('/').Last() + " " + request.Body);
                return (200, "{\"response\":{\"result\":true,\"reason\":\"\"}}");
            }
            return (404, "");
        }

        private NodeInstance CreateAccount(string commandUrl = "")
        {
            var node = CreateNodeInstance(DriverFactory.DriverGuid);
            node.SetProperty(TeslaDriverFactory.ApiUrlKey, _server.Url);
            node.SetProperty(TeslaDriverFactory.ClientIdKey, ClientId);
            node.SetProperty(TeslaDriverFactory.RefreshTokenKey, RefreshToken);
            node.SetProperty(TeslaDriverFactory.CommandUrlKey, commandUrl);
            node.SetProperty(TeslaDriverFactory.PollKey, 3600);
            return node;
        }

        private Dictionary<string, NodeInstance> AddVehicle(NodeInstance account, string vin = Vin)
        {
            var vehicle = CreateNodeInstance(TeslaDriverFactory.VehicleTemplate);
            vehicle.SetProperty(TeslaDriverFactory.VinKey, vin);
            account.InverseThis2ParentNodeInstanceNavigation.Add(vehicle);
            var values = new Dictionary<string, NodeInstance>();
            foreach (var dp in TeslaDataPoints.All)
            {
                var child = CreateNodeInstance(dp.Template);
                vehicle.InverseThis2ParentNodeInstanceNavigation.Add(child);
                values[dp.Key] = child;
            }
            return values;
        }

        private async Task<TeslaDriver> CreateTeslaAsync(NodeInstance account)
        {
            var driver = (TeslaDriver)await CreateDriver(account);
            driver.Http = _http;
            driver.AuthUrl = _server.Url + "/oauth2/v3/token";
            driver.WakePollInterval = TimeSpan.FromMilliseconds(10);
            driver.WakeTimeout = TimeSpan.FromSeconds(2);
            await driver.Init();
            return driver;
        }

        private object Value(NodeInstance node) => Dispatcher.GetValue(DispatchableType.NodeInstance, node.ObjId)?.Value;

        private Task WriteAsync(TeslaDriver driver, string key, object value)
        {
            var node = driver.Vehicles.Single().Values.Single(v => v.DataPoint.Key == key);
            return node.WriteDirectAsync(value);
        }

        [Fact]
        public async Task Poll_Online_DispatchesAllValuesInMetricUnits()
        {
            var account = CreateAccount();
            var values = AddVehicle(account);
            var driver = await CreateTeslaAsync(account);

            await driver.PollAsync();

            Assert.Equal("online", Value(values["state"]));
            Assert.Equal(81, Value(values["battery_level"]));
            Assert.Equal(321.9, Value(values["battery_range"])); // 200 mi
            Assert.Equal("Charging", Value(values["charging_state"]));
            Assert.Equal(true, Value(values["charging"]));
            Assert.Equal(90, Value(values["charge_limit"]));
            Assert.Equal(11d, Value(values["charger_power"]));
            Assert.Equal(1.25, Value(values["time_to_full"]));
            Assert.Equal(true, Value(values["charge_port_open"]));
            Assert.Equal(21.5, Value(values["inside_temp"]));
            Assert.Equal(4d, Value(values["outside_temp"]));
            Assert.Equal(false, Value(values["climate"]));
            Assert.Equal(21d, Value(values["target_temp"]));
            Assert.Equal(true, Value(values["locked"]));
            Assert.Equal(false, Value(values["sentry_mode"]));
            Assert.Equal(1609.3, Value(values["odometer"]));
            Assert.Equal(48.3069, Value(values["latitude"]));
            Assert.Equal(14.2858, Value(values["longitude"]));
            Assert.Equal("P", Value(values["shift_state"]));
        }

        [Fact]
        public async Task Poll_RequestsTheDataEndpointsWithBearerToken()
        {
            var account = CreateAccount();
            AddVehicle(account);
            var driver = await CreateTeslaAsync(account);

            await driver.PollAsync();

            var token = Assert.Single(_server.Requests, r => r.Path == "/oauth2/v3/token");
            var form = QueryHelpers.ParseQuery("?" + token.Body);
            Assert.Equal("refresh_token", form["grant_type"]);
            Assert.Equal(ClientId, form["client_id"]);
            Assert.Equal(RefreshToken, form["refresh_token"]);

            var data = Assert.Single(_server.Requests, r => r.Path.EndsWith("/vehicle_data"));
            Assert.Equal("charge_state;climate_state;drive_state;location_data;vehicle_state", QueryHelpers.ParseQuery(data.Query)["endpoints"]);
            Assert.Equal("Bearer access-1", data.Headers["Authorization"]);
        }

        [Fact]
        public async Task Poll_Asleep_OnlyReadsTheStateAndNeverWakes()
        {
            _vehicleState = "asleep";
            var account = CreateAccount();
            var values = AddVehicle(account);
            var driver = await CreateTeslaAsync(account);

            await driver.PollAsync();

            Assert.Equal("asleep", Value(values["state"]));
            Assert.Null(Value(values["battery_level"]));
            Assert.DoesNotContain(_server.Requests, r => r.Path.EndsWith("/vehicle_data") || r.Path.EndsWith("/wake_up"));
        }

        [Fact]
        public async Task Poll_TwiceWithinTokenLifetime_RefreshesOnce()
        {
            var account = CreateAccount();
            AddVehicle(account);
            var driver = await CreateTeslaAsync(account);

            await driver.PollAsync();
            await driver.PollAsync();

            Assert.Single(_server.Requests, r => r.Path == "/oauth2/v3/token");
        }

        [Fact]
        public async Task Refresh_RotatedTokenIsUsedNextAndPersisted()
        {
            var account = CreateAccount();
            AddVehicle(account);
            var driver = await CreateTeslaAsync(account);

            await driver.PollAsync();

            Assert.Equal("EU_refresh_2", driver.Api.RefreshToken);
            var state = account.GetProperty(TeslaDriverFactory.TokenStateKey).ValueString;
            Assert.Equal("EU_refresh_2", TokenState.Resolve(state, RefreshToken));

            // restart: the rotated token survives, the configured one is already used up
            var restarted = await CreateTeslaAsync(account);
            Assert.Equal("EU_refresh_2", restarted.Api.RefreshToken);
        }

        [Fact]
        public void TokenState_NewTokenEnteredByTheUser_WinsOverTheStoredOne()
        {
            var state = TokenState.Serialize("old-configured", "rotated");

            Assert.Equal("rotated", TokenState.Resolve(state, "old-configured"));
            Assert.Equal("new-configured", TokenState.Resolve(state, "new-configured"));
            Assert.Equal("configured", TokenState.Resolve(null, "configured"));
            Assert.Equal("configured", TokenState.Resolve("{broken", "configured"));
        }

        [Fact]
        public async Task Poll_AccessTokenRejected_RefreshesAndRetriesOnce()
        {
            var account = CreateAccount();
            var values = AddVehicle(account);
            var driver = await CreateTeslaAsync(account);
            await driver.PollAsync();

            var inner = _server.Handler;
            var rejected = 0;
            _server.Handler = r =>
            {
                if (r.Path.StartsWith("/api/") && r.Headers["Authorization"] == "Bearer access-1")
                {
                    rejected++;
                    return (401, "{}");
                }
                return inner(r);
            };
            _vehicleData = DefaultVehicleData();
            await driver.PollAsync();

            Assert.Equal(1, rejected);
            Assert.Equal(2, _server.Requests.Count(r => r.Path == "/oauth2/v3/token"));
            Assert.Equal("online", Value(values["state"]));
        }

        [Fact]
        public async Task Poll_RefreshTokenInvalid_DriverStillStartsAndLogs()
        {
            var account = CreateAccount();
            account.SetProperty(TeslaDriverFactory.RefreshTokenKey, "wrong");
            var values = AddVehicle(account);
            var driver = await CreateTeslaAsync(account);

            Assert.True(await driver.Start());
            await driver.Stop();

            Assert.Null(Value(values["state"]));
        }

        [Fact]
        public async Task Write_Lock_SendsDoorLockAndRereads()
        {
            var account = CreateAccount();
            AddVehicle(account);
            var driver = await CreateTeslaAsync(account);

            await WriteAsync(driver, "locked", false);

            Assert.Equal(new[] { "door_unlock {}" }, _commands);
            Assert.Contains(_server.Requests, r => r.Path.EndsWith("/vehicle_data"));
        }

        [Theory]
        [InlineData("charging", true, "charge_start {}")]
        [InlineData("charging", false, "charge_stop {}")]
        [InlineData("climate", true, "auto_conditioning_start {}")]
        [InlineData("climate", 0, "auto_conditioning_stop {}")]
        [InlineData("charge_limit", 80, "set_charge_limit {\"percent\":80}")]
        [InlineData("charge_limit", 20, "set_charge_limit {\"percent\":50}")]
        [InlineData("charge_limit", "95", "set_charge_limit {\"percent\":95}")]
        [InlineData("sentry_mode", true, "set_sentry_mode {\"on\":true}")]
        [InlineData("target_temp", 22.5, "set_temps {\"driver_temp\":22.5,\"passenger_temp\":22.5}")]
        [InlineData("charge_port_open", true, "charge_port_door_open {}")]
        [InlineData("flash_lights", true, "flash_lights {}")]
        [InlineData("honk", true, "honk_horn {}")]
        public async Task Write_SendsTheMatchingCommand(string key, object value, string expected)
        {
            var account = CreateAccount();
            AddVehicle(account);
            var driver = await CreateTeslaAsync(account);

            await WriteAsync(driver, key, value);

            Assert.Equal(new[] { expected }, _commands);
        }

        [Fact]
        public async Task Write_ButtonReleased_SendsNothing()
        {
            var account = CreateAccount();
            AddVehicle(account);
            var driver = await CreateTeslaAsync(account);

            await WriteAsync(driver, "flash_lights", false);

            Assert.Empty(_commands);
            Assert.DoesNotContain(_server.Requests, r => r.Path.StartsWith("/api/"));
        }

        [Fact]
        public async Task Write_Asleep_WakesFirstThenSendsTheCommand()
        {
            _vehicleState = "asleep";
            var account = CreateAccount();
            var values = AddVehicle(account);
            var driver = await CreateTeslaAsync(account);

            await WriteAsync(driver, "climate", true);

            Assert.Single(_server.Requests, r => r.Path.EndsWith("/wake_up"));
            Assert.Equal(new[] { "auto_conditioning_start {}" }, _commands);
            Assert.Equal("online", Value(values["state"]));
        }

        [Fact]
        public async Task Write_Wake_OnlyWakesTheVehicle()
        {
            _vehicleState = "asleep";
            var account = CreateAccount();
            AddVehicle(account);
            var driver = await CreateTeslaAsync(account);

            await WriteAsync(driver, "wake", true);

            Assert.Single(_server.Requests, r => r.Path.EndsWith("/wake_up"));
            Assert.Empty(_commands);
        }

        [Fact]
        public async Task Write_NeverWakes_GivesUpWithoutCommand()
        {
            _vehicleState = "asleep";
            var inner = _server.Handler;
            _server.Handler = r => r.Path.EndsWith("/wake_up") ? (200, "{\"response\":{\"state\":\"asleep\"}}") : inner(r);
            var account = CreateAccount();
            AddVehicle(account);
            var driver = await CreateTeslaAsync(account);

            await WriteAsync(driver, "locked", true);

            Assert.Empty(_commands);
        }

        [Fact]
        public async Task Write_CommandProxyConfigured_CommandsGoToTheProxy()
        {
            await using var proxy = await FakeHttpServer.StartAsync();
            proxy.Handler = _ => (200, "{\"response\":{\"result\":true,\"reason\":\"\"}}");
            var account = CreateAccount(commandUrl: proxy.Url + "/");
            AddVehicle(account);
            var driver = await CreateTeslaAsync(account);

            await WriteAsync(driver, "locked", true);

            var command = Assert.Single(proxy.Requests);
            Assert.Equal($"/api/1/vehicles/{Vin}/command/door_lock", command.Path);
            Assert.Equal("Bearer access-1", command.Headers["Authorization"]);
            Assert.Empty(_commands);
        }

        [Fact]
        public async Task Write_CommandRejected_DoesNotThrow()
        {
            var inner = _server.Handler;
            _server.Handler = r => r.Path.Contains("/command/") ? (200, "{\"response\":{\"result\":false,\"reason\":\"vehicle_command_protocol_required\"}}") : inner(r);
            var account = CreateAccount();
            AddVehicle(account);
            var driver = await CreateTeslaAsync(account);

            await WriteAsync(driver, "locked", true);
        }

        [Fact]
        public async Task Configure_CreatesAllDataPoints()
        {
            var account = CreateAccount();
            AddVehicle(account);
            var driver = await CreateTeslaAsync(account);

            var vehicle = Assert.Single(driver.Vehicles);
            Assert.Equal(Vin, vehicle.Vin);
            Assert.Equal(TeslaDataPoints.All.Count, vehicle.Values.Count);
        }
    }
}
