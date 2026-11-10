using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using P3.Driver.Byd.Driver;
using P3.Driver.Byd.Driver.Crypto;
using Xunit;

namespace P3.Driver.Byd.Tests
{
    internal sealed record BydRequest(string Endpoint, Dictionary<string, string> Outer, Dictionary<string, string> Inner);

    public class BydDriverTests : DriverFactoryTestBase<BydDriverFactory>, IAsyncLifetime
    {
        private const string Vin = "LGXCE4CB0P0123456";
        private const string User = "anna@example.com";

        private FakeHttpServer _server;
        private readonly HttpClient _http = new();
        private readonly List<BydRequest> _requests = new();
        private int _logins;
        private string _expireNext;
        private string _listErrorCode;

        // what the fake car answers
        private object _realtimeTrigger = new { requestSerial = "RS1" };
        private object _realtimeResult = new
        {
            requestSerial = "RS1", onlineState = 1, elecPercent = 81, enduranceMileage = 402.5, totalMileage = 12345, chargeState = 0, tempInCar = 19.5,
            speed = 0, leftFrontDoorLock = 2, rightFrontDoorLock = 2, leftRearDoorLock = 2, rightRearDoorLock = 2, time = 1762520000
        };
        private object _gpsResult = new { requestSerial = "GS1", latitude = 48.2082, longitude = 16.3738, direction = 90 };
        private object _controlTrigger = new { requestSerial = "CS1" };
        private object _controlResult = new { requestSerial = "CS1", controlState = 1 };

        private string SignToken => $"sign-{_logins}";
        private string EncryToken => $"encry-{_logins}";

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

        private static Dictionary<string, string> Flat(string json)
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.ValueKind == JsonValueKind.Null ? null : p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : p.Value.GetRawText());
        }

        private static (int, string) Reply(object outer) => (200, JsonSerializer.Serialize(new { response = Bangcle.Default.Encode(JsonSerializer.Serialize(outer)) }));

        private (int, string) Handle(RecordedRequest r)
        {
            using var body = JsonDocument.Parse(r.Body);
            var outerJson = Bangcle.Default.Decode(body.RootElement.GetProperty("request").GetString());
            var outer = Flat(outerJson);

            if (r.Path == "/app/account/login")
            {
                Dictionary<string, string> loginInner;
                try
                {
                    loginInner = Flat(BydCrypto.AesDecryptHex(outer["encryData"], BydCrypto.LoginKey("pw")));
                }
                catch (System.Security.Cryptography.CryptographicException)
                {
                    return Reply(new { code = "1003", message = "wrong password" });
                }
                _requests.Add(new BydRequest(r.Path, outer, loginInner));
                _logins++;
                var token = JsonSerializer.Serialize(new { token = new { userId = "U77", signToken = SignToken, encryToken = EncryToken } });
                return Reply(new { code = "0", message = "SUCCESS", respondData = BydCrypto.AesEncryptHex(token, BydCrypto.LoginKey("pw")) });
            }

            var contentKey = BydCrypto.Md5(EncryToken);
            Dictionary<string, string> inner;
            try
            {
                inner = Flat(BydCrypto.AesDecryptHex(outer["encryData"], contentKey));
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                return Reply(new { code = "1005", message = "token invalid" });
            }
            _requests.Add(new BydRequest(r.Path, outer, inner));

            if (_expireNext == r.Path)
            {
                _expireNext = null;
                return Reply(new { code = "1002", message = "session expired" });
            }
            if (_listErrorCode != null && r.Path == "/app/account/getAllListByUserId")
            {
                return Reply(new { code = _listErrorCode, message = $"Service error({_listErrorCode})" });
            }

            object data = r.Path switch
            {
                "/app/account/getAllListByUserId" => new object[]
                {
                    new { vin = Vin, autoAlias = "Seal", modelName = "SEAL", energyType = "0" },
                    new { vin = "LGXCE4CB0P0999999", autoAlias = "", modelName = "ATTO 3", energyType = "0" },
                },
                "/vehicleInfo/vehicle/vehicleRealTimeRequest" => _realtimeTrigger,
                "/vehicleInfo/vehicle/vehicleRealTimeResult" => _realtimeResult,
                "/control/getGpsInfo" => new { requestSerial = "GS1" },
                "/control/getGpsInfoResult" => _gpsResult,
                "/control/remoteControl" => _controlTrigger,
                "/control/remoteControlResult" => _controlResult,
                _ => null
            };
            if (data == null)
            {
                return Reply(new { code = "1001", message = "not supported" });
            }
            return Reply(new { code = "0", respondData = BydCrypto.AesEncryptHex(JsonSerializer.Serialize(data), contentKey) });
        }

        private NodeInstance CreateAccount(string password = "pw", string pin = "123456")
        {
            var node = CreateNodeInstance(DriverFactory.DriverGuid);
            node.SetProperty(BydDriverFactory.UsernameKey, User);
            node.SetProperty(BydDriverFactory.PasswordKey, password);
            node.SetProperty(BydDriverFactory.PinKey, pin);
            node.SetProperty(BydDriverFactory.CountryKey, "at");
            node.SetProperty(BydDriverFactory.PollKey, 3600);
            return node;
        }

        private Dictionary<string, NodeInstance> AddVehicle(NodeInstance account)
        {
            var vehicle = CreateNodeInstance(BydDriverFactory.VehicleTemplate);
            vehicle.SetProperty(BydDriverFactory.VinKey, Vin);
            vehicle.SetProperty(BydDriverFactory.ClimateTemperatureKey, 22);
            vehicle.SetProperty(BydDriverFactory.ClimateDurationKey, 15);
            account.InverseThis2ParentNodeInstanceNavigation.Add(vehicle);
            var values = new Dictionary<string, NodeInstance>();
            foreach (var dp in BydDataPoints.All)
            {
                var child = CreateNodeInstance(dp.Template);
                vehicle.InverseThis2ParentNodeInstanceNavigation.Add(child);
                values[dp.Key] = child;
            }
            return values;
        }

        private async Task<BydDriver> CreateBydAsync(NodeInstance account)
        {
            var driver = (BydDriver)await CreateDriver(account);
            driver.Http = _http;
            driver.BaseUrlOverride = _server.Url;
            driver.PollIntervalOverride = TimeSpan.Zero;
            await driver.Init();
            return driver;
        }

        private object Value(NodeInstance node) => Dispatcher.GetValue(DispatchableType.NodeInstance, node.ObjId)?.Value;

        private Task WriteAsync(BydDriver driver, string key, object value) => driver.Vehicles.Single().Values.Single(v => v.DataPoint.Key == key).WriteDirectAsync(value);

        [Fact]
        public async Task Login_SendsTheSignedEnvelope()
        {
            var account = CreateAccount();
            AddVehicle(account);
            var driver = await CreateBydAsync(account);

            await driver.PollAsync();

            var login = _requests.First(r => r.Endpoint == "/app/account/login");
            Assert.Equal(new[]
            {
                "appName", "countryCode", "encryData", "functionType", "identifier", "identifierType", "imeiMD5", "isAuto", "language", "reqTimestamp",
                "sign", "signKey", "ostype", "imei", "mac", "model", "sdk", "mod", "serviceTime", "checkcode"
            }, login.Outer.Keys);
            Assert.Equal("AT", login.Outer["countryCode"]);
            Assert.Equal("pwdLogin", login.Outer["functionType"]);
            Assert.Equal(User, login.Outer["identifier"]);
            Assert.Equal("pw", login.Outer["signKey"]);
            Assert.Equal(BydCrypto.Md5(User), login.Outer["imeiMD5"]);
            Assert.Equal(BydCrypto.Md5(User), login.Inner["imeiMD5"]);
            Assert.Equal("[1,2]", login.Inner["agreementType"]);

            var signFields = login.Inner.Concat(login.Outer.Where(p => p.Key is "appName" or "countryCode" or "functionType" or "identifier" or "identifierType" or "language" or "reqTimestamp"));
            Assert.Equal(BydCrypto.Sign(signFields, BydCrypto.Md5("pw")), login.Outer["sign"]);
            Assert.Equal(BydCrypto.CheckCode(login.Outer.Where(p => p.Key != "checkcode")), login.Outer["checkcode"]);
        }

        [Fact]
        public async Task TokenRequests_AreSignedWithTheSessionKeys()
        {
            var account = CreateAccount();
            AddVehicle(account);
            var driver = await CreateBydAsync(account);

            await driver.PollAsync();

            var list = _requests.First(r => r.Endpoint == "/app/account/getAllListByUserId");
            Assert.Equal("U77", list.Outer["identifier"]);
            // the sign string written out by hand like pyBYD's dict: imeiMD5 is in the inner payload and the outer fields, but appears once
            var signFields = new Dictionary<string, string>(list.Inner);
            foreach (var key in new[] { "countryCode", "identifier", "imeiMD5", "language", "reqTimestamp" })
            {
                signFields[key] = list.Outer[key];
            }
            var signString = string.Join("&", signFields.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}")) + "&password=" + BydCrypto.Md5("sign-1");
            Assert.Single(System.Text.RegularExpressions.Regex.Matches(signString, "imeiMD5="));
            Assert.Equal(BydCrypto.Sha1Mixed(signString), list.Outer["sign"]);
            Assert.Equal(BydCrypto.CheckCode(list.Outer.Where(p => p.Key != "checkcode")), list.Outer["checkcode"]);
            Assert.Equal(32, list.Inner["random"].Length);
        }

        [Fact]
        public async Task Poll_TriggersAndPollsRealtimeAndGps()
        {
            var account = CreateAccount();
            var values = AddVehicle(account);
            var driver = await CreateBydAsync(account);

            await driver.PollAsync();

            var poll = _requests.Single(r => r.Endpoint == "/vehicleInfo/vehicle/vehicleRealTimeResult");
            Assert.Equal("RS1", poll.Inner["requestSerial"]);
            Assert.Equal(Vin, poll.Inner["vin"]);
            Assert.Equal("0", poll.Inner["energyType"]);
            Assert.Equal(true, Value(values["online"]));
            Assert.Equal(81d, Value(values["battery"]));
            Assert.Equal(402.5, Value(values["range"]));
            Assert.Equal(12345d, Value(values["odometer"]));
            Assert.Equal(false, Value(values["charging"]));
            Assert.Equal(19.5, Value(values["inside_temp"]));
            Assert.Equal(true, Value(values["locked"]));
            Assert.Equal(48.2082, Value(values["latitude"]));
            Assert.Equal(16.3738, Value(values["longitude"]));
        }

        [Fact]
        public async Task Poll_VehicleListFails_StillReadsTheCarAsElectricAndAsksTheListAgain()
        {
            _listErrorCode = "1007";
            var account = CreateAccount();
            var values = AddVehicle(account);
            var driver = await CreateBydAsync(account);

            await driver.PollAsync();

            var poll = _requests.Single(r => r.Endpoint == "/vehicleInfo/vehicle/vehicleRealTimeResult");
            Assert.Equal("0", poll.Inner["energyType"]);
            Assert.Equal(81d, Value(values["battery"]));
            Assert.Equal(48.2082, Value(values["latitude"]));

            _listErrorCode = null;
            await driver.PollAsync();

            Assert.Equal(2, _requests.Count(r => r.Endpoint == "/app/account/getAllListByUserId"));
        }

        [Fact]
        public async Task Poll_ReadyTrigger_SkipsThePolling()
        {
            _realtimeTrigger = new { onlineState = 1, elecPercent = 55, enduranceMileage = 250, leftFrontDoorLock = 1, rightFrontDoorLock = 2, tempInCar = -129 };
            var account = CreateAccount();
            var values = AddVehicle(account);
            var driver = await CreateBydAsync(account);

            await driver.PollAsync();

            Assert.DoesNotContain(_requests, r => r.Endpoint == "/vehicleInfo/vehicle/vehicleRealTimeResult");
            Assert.Equal(55d, Value(values["battery"]));
            Assert.Equal(false, Value(values["locked"]));
            Assert.Null(Value(values["inside_temp"]));
        }

        [Fact]
        public async Task Poll_OfflineCar_PollsAllAttemptsAndReportsOffline()
        {
            _realtimeResult = new { requestSerial = "RS1", onlineState = 2 };
            var account = CreateAccount();
            var values = AddVehicle(account);
            var driver = await CreateBydAsync(account);

            await driver.PollAsync();

            Assert.Equal(10, _requests.Count(r => r.Endpoint == "/vehicleInfo/vehicle/vehicleRealTimeResult"));
            Assert.Equal(false, Value(values["online"]));
            Assert.Null(Value(values["battery"]));
        }

        [Fact]
        public async Task Write_Locked_SendsLockWithPinHash()
        {
            var account = CreateAccount();
            var values = AddVehicle(account);
            var driver = await CreateBydAsync(account);

            await WriteAsync(driver, "locked", true);

            var trigger = _requests.Single(r => r.Endpoint == "/control/remoteControl");
            Assert.Equal("LOCKDOOR", trigger.Inner["commandType"]);
            Assert.Equal(BydCrypto.Md5("123456"), trigger.Inner["commandPwd"]);
            Assert.False(trigger.Inner.ContainsKey("controlParamsMap"));
            Assert.Equal("CS1", _requests.Single(r => r.Endpoint == "/control/remoteControlResult").Inner["requestSerial"]);
            Assert.Equal(true, Value(values["locked"]));

            await WriteAsync(driver, "locked", false);
            Assert.Equal("OPENDOOR", _requests.Last(r => r.Endpoint == "/control/remoteControl").Inner["commandType"]);
            Assert.Equal(false, Value(values["locked"]));
        }

        [Fact]
        public async Task Write_Climate_SendsTemperatureAndDuration()
        {
            var account = CreateAccount();
            var values = AddVehicle(account);
            var driver = await CreateBydAsync(account);

            await WriteAsync(driver, "climate", true);

            var trigger = _requests.Single(r => r.Endpoint == "/control/remoteControl");
            Assert.Equal("OPENAIR", trigger.Inner["commandType"]);
            Assert.Equal(BydCloud.ClimateParams(22, 15), trigger.Inner["controlParamsMap"]);
            Assert.Equal(true, Value(values["climate"]));
        }

        [Theory]
        [InlineData("flash_lights", "FLASHLIGHTNOWHISTLE")]
        [InlineData("find_car", "FINDCAR")]
        public async Task Write_Action(string key, string command)
        {
            var account = CreateAccount();
            AddVehicle(account);
            var driver = await CreateBydAsync(account);

            await WriteAsync(driver, key, false);
            Assert.DoesNotContain(_requests, r => r.Endpoint == "/control/remoteControl");

            await WriteAsync(driver, key, true);
            Assert.Equal(command, _requests.Single(r => r.Endpoint == "/control/remoteControl").Inner["commandType"]);
        }

        [Fact]
        public async Task Write_Rejected_KeepsTheValue()
        {
            _controlResult = new { requestSerial = "CS1", controlState = 2 };
            var account = CreateAccount();
            var values = AddVehicle(account);
            var driver = await CreateBydAsync(account);

            await WriteAsync(driver, "locked", true);

            Assert.Null(Value(values["locked"]));
        }

        [Fact]
        public async Task Write_WithoutPin_SendsNothing()
        {
            var account = CreateAccount(pin: "");
            AddVehicle(account);
            var driver = await CreateBydAsync(account);

            await WriteAsync(driver, "locked", true);

            Assert.DoesNotContain(_requests, r => r.Endpoint == "/control/remoteControl");
        }

        [Fact]
        public async Task SessionExpired_LogsInAgainAndRetries()
        {
            var account = CreateAccount();
            var values = AddVehicle(account);
            var driver = await CreateBydAsync(account);
            await driver.PollAsync();

            _realtimeResult = new { requestSerial = "RS1", onlineState = 1, elecPercent = 60, enduranceMileage = 300 };
            _expireNext = "/vehicleInfo/vehicle/vehicleRealTimeRequest";
            await driver.PollAsync();

            Assert.Equal(2, _logins);
            Assert.Equal(60d, Value(values["battery"]));
        }

        [Fact]
        public async Task WrongPassword_DriverStartsAndReadsNothing()
        {
            var account = CreateAccount(password: "wrong");
            var values = AddVehicle(account);
            var driver = await CreateBydAsync(account);

            Assert.True(await driver.Start());
            await driver.Stop();

            Assert.Equal(0, _logins);
            Assert.Null(Value(values["battery"]));
        }

        [Fact]
        public async Task Scan_ListsCarsThatAreNotConfigured()
        {
            var account = CreateAccount();
            AddVehicle(account);
            var driver = await CreateBydAsync(account);

            var found = Assert.Single(await driver.Scan());

            Assert.Equal("ATTO 3 (LGXCE4CB0P0999999)", found.Name);
            Assert.Equal("LGXCE4CB0P0999999", found.GetProperty(BydDriverFactory.VinKey).ValueString);
            Assert.Equal(BydDataPoints.All.Count, found.InverseThis2ParentNodeInstanceNavigation.Count);
        }
    }
}
