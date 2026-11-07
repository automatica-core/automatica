using System;
using System.Security.Cryptography;
using System.Text;
using Automatica.Driver.Shelly.Gen2.Models;
using Newtonsoft.Json;
using Xunit;

namespace Automatica.Driver.Shelly.Unit.Tests
{
    public class ShellyGen2ModelTests
    {
        private const string Notify =
            "{\"ts\":1700000000.5,\"switch:0\":{\"id\":0,\"apower\":10.5,\"current\":0.05,\"output\":true,\"source\":\"button\"}," +
            "\"switch:1\":{\"id\":1,\"apower\":0,\"current\":0,\"output\":false,\"source\":\"init\"}}";

        [Fact]
        public void NotifyStatusEvent_ParsesBothSwitches()
        {
            var e = JsonConvert.DeserializeObject<NotifyStatusEvent>(Notify);

            Assert.Equal(1700000000.5, e.Ts);
            Assert.True(e.Switch0.Output);
            Assert.Equal(10.5, e.Switch0.Power);
            Assert.Equal("button", e.Switch0.Source);
            Assert.False(e.Switch1.Output);
            Assert.Equal(2, e.Switches.Count);
        }

        [Fact]
        public void GetSwitch_ReturnsTheSwitchForTheRelayIndex()
        {
            var e = JsonConvert.DeserializeObject<NotifyStatusEvent>(Notify);

            Assert.Same(e.Switch0, e.GetSwitch(0));
            Assert.Same(e.Switch1, e.GetSwitch(1));
        }

        [Theory]
        [InlineData(2)]
        [InlineData(5)]
        public void GetSwitch_OutOfRange_IsNull(int relayId)
        {
            var e = JsonConvert.DeserializeObject<NotifyStatusEvent>(Notify);

            Assert.Null(e.GetSwitch(relayId));
        }

        [Fact]
        public void GetValueFromSwitch_ReadsTheValue()
        {
            var e = JsonConvert.DeserializeObject<NotifyStatusEvent>(Notify);

            Assert.Equal(10.5, e.GetValueFromSwitch<double?>(0, a => a.Power));
            Assert.False(e.GetValueFromSwitch<bool?>(1, a => a.Output));
        }

        [Fact]
        public void GetValueFromSwitch_MissingSwitch_IsDefault()
        {
            // a notification that only carries switch:1 (e.g. Plus 2PM channel 1 changed)
            var e = JsonConvert.DeserializeObject<NotifyStatusEvent>("{\"ts\":1,\"switch:1\":{\"output\":true}}");

            Assert.Null(e.GetValueFromSwitch<bool?>(0, a => a.Output));
            Assert.True(e.GetValueFromSwitch<bool?>(1, a => a.Output));
            Assert.Null(e.GetValueFromSwitch<bool?>(2, a => a.Output));
        }

        [Fact]
        public void SwitchStatus_ParsesAenergyAndTemperature()
        {
            var s = JsonConvert.DeserializeObject<SwitchStatus>(
                "{\"id\":0,\"source\":\"init\",\"output\":true,\"apower\":8.9,\"voltage\":237.5,\"freq\":50.1,\"current\":0.04,\"pf\":0.71," +
                "\"aenergy\":{\"total\":6.532,\"by_minute\":[45.199,47.141,88.397],\"minute_ts\":1654511972},\"temperature\":{\"tC\":23.5,\"tF\":74.4}}");

            Assert.True(s.Output);
            Assert.Equal(237.5, s.Voltage);
            Assert.Equal(50.1, s.Freq);
            Assert.Equal(0.71, s.Pf);
            Assert.Equal(6.532, s.Aenergy.Total);
            Assert.Equal(3, s.Aenergy.ByMinute.Count);
            Assert.Equal(1654511972, s.Aenergy.MinuteTs);
            Assert.Equal(23.5, s.Temperature.TC);
        }

        [Theory]
        [InlineData("{\"code\":401,\"message\":\"{\\\"nonce\\\":42,\\\"realm\\\":\\\"r1\\\",\\\"algorithm\\\":\\\"SHA-256\\\"}\"}")]
        [InlineData("{\"message\":\"{\\\"nonce\\\":42,\\\"realm\\\":\\\"r1\\\",\\\"algorithm\\\":\\\"SHA-256\\\"}\",\"code\":401}")]
        public void ErrorModel_401_ParsesTheAuthChallenge_RegardlessOfPropertyOrder(string json)
        {
            var error = JsonConvert.DeserializeObject<ErrorModel>(json);

            var auth = Assert.IsType<AuthModel>(error.MessageObject);
            Assert.Equal("r1", auth.Realm);
            Assert.Equal(42, auth.Nonce);
        }

        [Fact]
        public void ErrorModel_OtherCodes_KeepThePlainMessage()
        {
            var error = JsonConvert.DeserializeObject<ErrorModel>("{\"code\":-103,\"message\":\"Invalid argument\"}");

            Assert.Equal(-103, error.Code);
            Assert.Equal("Invalid argument", error.Message);
            Assert.Null(error.MessageObject);
        }

        [Fact]
        public void ResponseModel_ParsesNotifications()
        {
            var r = JsonConvert.DeserializeObject<ResponseModel<object>>("{\"src\":\"dev\",\"method\":\"NotifyStatus\",\"params\":{\"ts\":1}}");

            Assert.Equal("NotifyStatus", r.Method);
            Assert.Equal("dev", r.Src);
            Assert.NotNull(r.Params);
            Assert.Null(r.Error);
        }

        [Fact]
        public void RpcModel_OmitsAuthWhenNotSet()
        {
            var json = JsonConvert.SerializeObject(new RpcModel { Id = "1", Method = "Switch.GetStatus", Source = "src" });

            Assert.DoesNotContain("auth", json);
            Assert.Contains("\"src\":\"src\"", json);
        }

        [Fact]
        public void CreateAuthModel_ComputesTheShellySha256Digest()
        {
            var auth = AuthModel.CreateAuthModel("pw", "shellypro4pm-f008d1d8b8b8", 1625038762, 313273957, "admin", "SHA-256");

            static string Sha(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s))).ToLowerInvariant();
            var expected = Sha($"{Sha("admin:shellypro4pm-f008d1d8b8b8:pw")}:1625038762:1:313273957:auth:{Sha("dummy_method:dummy_uri")}");

            Assert.Equal(expected, auth.Response);
            Assert.Equal(64, auth.Response.Length);
            Assert.Equal("admin", auth.Username);
            Assert.Equal("SHA-256", auth.Algorithm);
            Assert.Equal(313273957, auth.Cnonce);
        }

        [Fact]
        public void CreateAuthModel_DifferentPassword_DifferentResponse()
        {
            var a = AuthModel.CreateAuthModel("pw1", "realm", 1, 2, "admin", "SHA-256");
            var b = AuthModel.CreateAuthModel("pw2", "realm", 1, 2, "admin", "SHA-256");

            Assert.NotEqual(a.Response, b.Response);
        }

        [Fact]
        public void ShellyResult_FactoriesSetTheFlags()
        {
            var ok = ShellyResult<int>.Success(5, "m");
            var fail = ShellyResult<int>.Failure("f");
            var transient = ShellyResult<int>.TransientFailure("t");

            Assert.True(ok.IsSuccess);
            Assert.False(ok.IsTransient);
            Assert.Equal(5, ok.Value);
            Assert.Equal("m", ok.Message);

            Assert.False(fail.IsSuccess);
            Assert.False(fail.IsTransient);
            Assert.Equal(0, fail.Value);

            Assert.False(transient.IsSuccess);
            Assert.True(transient.IsTransient);
            Assert.Equal("t", transient.Message);
        }
    }
}
