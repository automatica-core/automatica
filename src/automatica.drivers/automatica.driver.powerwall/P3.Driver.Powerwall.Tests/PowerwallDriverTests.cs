using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using P3.Driver.Powerwall.Driver;
using Xunit;

namespace P3.Driver.Powerwall.Tests
{
    public class PowerwallDriverTests : DriverFactoryTestBase<PowerwallDriverFactory>, IAsyncLifetime
    {
        private const string Password = "ABCDE";

        private FakeHttpServer _server;
        private readonly HttpClient _http = new();

        private int _logins;
        private bool _expireNextToken;
        private int _operationStatus = 200;
        private int _postStatus = 200;
        private int _aggregatesStatus = 200;
        private double _reserveRaw = 24; // 20 % in the app
        private string _mode = "self_consumption";

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

        private static readonly object Aggregates = new
        {
            site = new { instant_power = 1234.0, energy_imported = 1500000.0, energy_exported = 250000.0 },
            battery = new { instant_power = -2500.0, energy_imported = 800000.0, energy_exported = 700000.0 },
            load = new { instant_power = 3100.0, energy_imported = 2000000.0, energy_exported = 0.0 },
            solar = new { instant_power = 4366.0, energy_imported = 0.0, energy_exported = 1200000.0 }
        };

        private (int, string) Handle(RecordedRequest request)
        {
            if (request.Path == "/api/login/Basic")
            {
                using var body = JsonDocument.Parse(request.Body);
                if (body.RootElement.GetProperty("username").GetString() != "customer" || body.RootElement.GetProperty("password").GetString() != Password)
                {
                    return (401, "{\"code\":401,\"error\":\"bad credentials\"}");
                }
                _logins++;
                return (200, JsonSerializer.Serialize(new { email = "x@y", firstname = "Tesla", roles = new[] { "Home_Owner" }, token = $"tok-{_logins}==", provider = "Basic" }));
            }

            if (!request.Headers.TryGetValue("Authorization", out var auth) || auth != $"Bearer tok-{_logins}==")
            {
                return (401, "{\"code\":401,\"error\":\"Token Expired\"}");
            }
            if (_expireNextToken)
            {
                _expireNextToken = false;
                return (401, "{\"code\":401,\"error\":\"Token Expired\"}");
            }

            switch (request.Method, request.Path)
            {
                case ("GET", "/api/meters/aggregates"):
                    return _aggregatesStatus == 200 ? (200, JsonSerializer.Serialize(Aggregates)) : (_aggregatesStatus, "");
                case ("GET", "/api/system_status/soe"):
                    return (200, "{\"percentage\":52.5}");
                case ("GET", "/api/system_status/grid_status"):
                    return (200, "{\"grid_status\":\"SystemIslandedActive\",\"grid_services_active\":false}");
                case ("GET", "/api/operation"):
                    return _operationStatus == 200
                        ? (200, JsonSerializer.Serialize(new { real_mode = _mode, backup_reserve_percent = _reserveRaw }))
                        : (_operationStatus, "");
                case ("POST", "/api/operation"):
                    if (_postStatus != 200)
                    {
                        return (_postStatus, "{\"code\":403,\"error\":\"Unable to POST to resource\",\"message\":\"User does not have adequate access rights\"}");
                    }
                    using (var body = JsonDocument.Parse(request.Body))
                    {
                        _mode = body.RootElement.GetProperty("real_mode").GetString();
                        _reserveRaw = body.RootElement.GetProperty("backup_reserve_percent").GetDouble();
                    }
                    return (200, request.Body);
                case ("GET", "/api/config/completed"):
                    return (202, "");
            }
            return (404, "");
        }

        private NodeInstance CreateGateway(bool allowWrites = false, string password = Password)
        {
            var node = CreateNodeInstance(DriverFactory.DriverGuid);
            node.SetProperty(PowerwallDriverFactory.HostKey, _server.Url);
            node.SetProperty(PowerwallDriverFactory.PasswordKey, password);
            node.SetProperty(PowerwallDriverFactory.EmailKey, "");
            node.SetProperty(PowerwallDriverFactory.PollKey, 3600);
            node.SetProperty(PowerwallDriverFactory.AllowWritesKey, allowWrites);
            node.SetProperty(PowerwallDriverFactory.FingerprintKey, "");
            foreach (var dp in PowerwallDataPoints.All)
            {
                node.InverseThis2ParentNodeInstanceNavigation.Add(CreateNodeInstance(dp.Template));
            }
            return node;
        }

        private async Task<PowerwallDriver> CreateGatewayAsync(NodeInstance node)
        {
            var driver = (PowerwallDriver)await CreateDriver(node);
            driver.Http = _http;
            await driver.Init();
            return driver;
        }

        private static object Value(PowerwallDriver driver, string key) => driver.Values.Single(v => v.DataPoint.Key == key).LastValue;

        private static Task WriteAsync(PowerwallDriver driver, string key, object value) =>
            driver.Values.Single(v => v.DataPoint.Key == key).WriteDirectAsync(value);

        private IEnumerable<RecordedRequest> Posts => _server.Requests.Where(r => r.Method == "POST" && r.Path == "/api/operation");

        [Fact]
        public async Task Poll_DispatchesAllValues_InKwWithTheEnergyMonitorSigns()
        {
            var driver = await CreateGatewayAsync(CreateGateway());

            await driver.PollAsync();

            Assert.Equal(true, Value(driver, "connected"));
            Assert.Equal(1.234, Value(driver, "grid_power"));
            Assert.Equal(4.366, Value(driver, "solar_power"));
            Assert.Equal(2.5, Value(driver, "battery_power")); // the gateway reports -2500 W = charging
            Assert.Equal(3.1, Value(driver, "load_power"));
            Assert.Equal(50.0, Value(driver, "soc")); // (52.5 - 5) / 0.95
            Assert.Equal("SystemIslandedActive", Value(driver, "grid_status"));
            Assert.Equal(false, Value(driver, "grid_connected"));
            Assert.Equal("self_consumption", Value(driver, "operation_mode"));
            Assert.Equal(20.0, Value(driver, "backup_reserve"));
            Assert.Equal(1500.0, Value(driver, "grid_import_energy"));
            Assert.Equal(250.0, Value(driver, "grid_export_energy"));
            Assert.Equal(1200.0, Value(driver, "solar_energy"));
            Assert.Equal(800.0, Value(driver, "battery_charge_energy"));
            Assert.Equal(700.0, Value(driver, "battery_discharge_energy"));
            Assert.Equal(2000.0, Value(driver, "load_energy"));
        }

        [Fact]
        public async Task Login_UsesTheCustomerAccount_AndTheTokenIsReused()
        {
            var driver = await CreateGatewayAsync(CreateGateway());

            await driver.PollAsync();
            await driver.PollAsync();

            Assert.Equal(1, _logins);
            var login = Assert.Single(_server.Requests, r => r.Path == "/api/login/Basic");
            using var body = JsonDocument.Parse(login.Body);
            Assert.Equal("customer", body.RootElement.GetProperty("username").GetString());
            Assert.All(_server.Requests.Where(r => r.Path != "/api/login/Basic"), r => Assert.Equal("Bearer tok-1==", r.Headers["Authorization"]));
        }

        [Fact]
        public async Task ExpiredToken_LogsInAgainOnce()
        {
            var driver = await CreateGatewayAsync(CreateGateway());
            await driver.PollAsync();

            _expireNextToken = true;
            await driver.PollAsync();

            Assert.Equal(2, _logins);
            Assert.Equal(true, Value(driver, "connected"));
        }

        [Fact]
        public async Task WrongPassword_IsNotConnected()
        {
            var driver = await CreateGatewayAsync(CreateGateway(password: "wrong"));

            var ex = await Assert.ThrowsAsync<PowerwallApiException>(() => driver.PollAsync());
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, ex.StatusCode);
            Assert.Equal(false, Value(driver, "connected"));
            Assert.DoesNotContain(_server.Requests, r => r.Path == "/api/meters/aggregates");
        }

        [Fact]
        public async Task GatewayStopped_IsNotConnected_AndKeepsTheLastValues()
        {
            var driver = await CreateGatewayAsync(CreateGateway());
            await driver.PollAsync();

            _aggregatesStatus = 502;
            await Assert.ThrowsAsync<PowerwallApiException>(() => driver.PollAsync());

            Assert.Equal(false, Value(driver, "connected"));
            Assert.Equal(1.234, Value(driver, "grid_power"));
        }

        [Fact]
        public async Task NoOperationEndpoint_Powerwall3_ReadsTheRest_AndStopsAskingForIt()
        {
            _operationStatus = 404;
            var driver = await CreateGatewayAsync(CreateGateway());

            await driver.PollAsync();
            await driver.PollAsync();

            Assert.Equal(50.0, Value(driver, "soc"));
            Assert.Null(Value(driver, "operation_mode"));
            Assert.Single(_server.Requests, r => r.Path == "/api/operation");
        }

        [Fact]
        public async Task Write_SwitchedOff_SendsNothing()
        {
            var driver = await CreateGatewayAsync(CreateGateway(allowWrites: false));
            await driver.PollAsync();

            await WriteAsync(driver, "backup_reserve", 50);

            Assert.Empty(Posts);
            Assert.Equal(20.0, Value(driver, "backup_reserve"));
        }

        [Fact]
        public async Task WriteReserve_PostsTheFullOperation_OnTheGatewayScale()
        {
            var driver = await CreateGatewayAsync(CreateGateway(allowWrites: true));
            await driver.PollAsync();

            await WriteAsync(driver, "backup_reserve", 50);

            var post = Assert.Single(Posts);
            using var body = JsonDocument.Parse(post.Body);
            Assert.Equal("self_consumption", body.RootElement.GetProperty("real_mode").GetString());
            Assert.Equal(52.5, body.RootElement.GetProperty("backup_reserve_percent").GetDouble()); // 50 * 0.95 + 5
            Assert.Contains(_server.Requests, r => r.Path == "/api/config/completed");
            Assert.Equal(50.0, Value(driver, "backup_reserve"));
        }

        [Fact]
        public async Task WriteMode_KeepsTheReserve()
        {
            var driver = await CreateGatewayAsync(CreateGateway(allowWrites: true));
            await driver.PollAsync();

            await WriteAsync(driver, "operation_mode", "Backup");

            var post = Assert.Single(Posts);
            using var body = JsonDocument.Parse(post.Body);
            Assert.Equal("backup", body.RootElement.GetProperty("real_mode").GetString());
            Assert.Equal(24, body.RootElement.GetProperty("backup_reserve_percent").GetDouble());
            Assert.Equal("backup", Value(driver, "operation_mode"));
        }

        [Fact]
        public async Task WriteReserve_ClampsToTheAppRange()
        {
            var driver = await CreateGatewayAsync(CreateGateway(allowWrites: true));

            await WriteAsync(driver, "backup_reserve", 150);

            using var body = JsonDocument.Parse(Assert.Single(Posts).Body);
            Assert.Equal(100, body.RootElement.GetProperty("backup_reserve_percent").GetDouble());
        }

        [Fact]
        public async Task WriteRefused_KeepsTheGatewayValues()
        {
            _postStatus = 403;
            var driver = await CreateGatewayAsync(CreateGateway(allowWrites: true));
            await driver.PollAsync();

            await WriteAsync(driver, "backup_reserve", 80);

            Assert.Single(Posts);
            Assert.Equal(20.0, Value(driver, "backup_reserve"));
            Assert.Equal(24, _reserveRaw);
        }

        [Fact]
        public async Task WriteUnknownMode_SendsNothing()
        {
            var driver = await CreateGatewayAsync(CreateGateway(allowWrites: true));

            await WriteAsync(driver, "operation_mode", "off_grid");

            Assert.Empty(Posts);
        }

        [Fact]
        public void AppPercent_HidesTheGatewayReserve_AndNeverGoesBelowZero()
        {
            Assert.Equal(0, PowerwallDataPoints.AppPercent(3.9));
            Assert.Equal(100, PowerwallDataPoints.AppPercent(100));
            Assert.Equal(24, PowerwallDataPoints.RawPercent(20));
        }

        [Theory]
        [InlineData("192.168.1.50", "https://192.168.1.50")]
        [InlineData(" 192.168.1.50/ ", "https://192.168.1.50")]
        [InlineData("http://127.0.0.1:8080", "http://127.0.0.1:8080")]
        public void Address_DefaultsToHttps(string input, string expected)
        {
            Assert.Equal(expected, PowerwallApi.NormalizeUrl(input));
        }

        private static X509Certificate2 SelfSigned(string name)
        {
            using var key = ECDsa.Create();
            var request = new CertificateRequest($"CN={name}", key, HashAlgorithmName.SHA256);
            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        }

        [Fact]
        public void Certificate_EmptyPin_TrustsAndPinsTheFirstCertificate()
        {
            using var cert = SelfSigned("powerwall");
            string pinned = null;

            Assert.True(PowerwallApi.Validate(cert, SslPolicyErrors.RemoteCertificateChainErrors, "", p => pinned = p));
            Assert.Equal(PowerwallApi.Fingerprint(cert), pinned);
        }

        [Fact]
        public void Certificate_WithPin_OnlyThatCertificateIsAccepted()
        {
            using var cert = SelfSigned("powerwall");
            using var other = SelfSigned("attacker");
            var pin = string.Join(":", Enumerable.Range(0, 32).Select(i => PowerwallApi.Fingerprint(cert).Substring(i * 2, 2))).ToLowerInvariant();

            Assert.True(PowerwallApi.Validate(cert, SslPolicyErrors.RemoteCertificateChainErrors, pin, _ => throw new InvalidOperationException()));
            Assert.False(PowerwallApi.Validate(other, SslPolicyErrors.RemoteCertificateChainErrors, pin, _ => throw new InvalidOperationException()));
        }
    }
}
