using System;
using System.Collections.Generic;
using Automatica.Core.Satellite.Abstraction;
using Automatica.Core.Satellite.Abstraction.Model;
using Automatica.Core.Satellite.Runtime;
using Docker.DotNet.Models;
using Newtonsoft.Json;
using Xunit;

namespace Automatica.Core.Satellite.Tests
{
    /// <summary>
    /// The JSON the core sends over MQTT and the models of the web api
    /// </summary>
    public class SatelliteModelTests
    {
        [Fact]
        public void ActionRequest_CurrentCorePayload_IsParsed()
        {
            var id = Guid.Parse("6c1c3c47-3e4a-4b8e-9f0a-1d2e3f405060");
            var json = $@"{{
                ""Action"": 0,
                ""ImageName"": ""automaticacore/automatica-plugin-knx"",
                ""Tag"": ""latest-develop"",
                ""ImageSource"": ""https://hub.docker.com"",
                ""Id"": ""{id}"",
                ""ClientKey"": ""{Infrastructure.TestKeys.PluginKey}"",
                ""BrokerTlsPort"": 8883,
                ""BrokerFingerprint"": ""{new string('a', 64)}"",
                ""ImageDigest"": ""{Infrastructure.TestKeys.Digest}"",
                ""Devices"": [""/dev/ttyUSB0""],
                ""CapAdd"": [""NET_ADMIN""]
            }}";

            var request = JsonConvert.DeserializeObject<ActionRequest>(json);

            Assert.Equal(SlaveAction.Start, request.Action);
            Assert.Equal("automaticacore/automatica-plugin-knx", request.ImageName);
            Assert.Equal("latest-develop", request.Tag);
            Assert.Equal(id, request.Id);
            Assert.Equal(Infrastructure.TestKeys.PluginKey, request.ClientKey);
            Assert.Equal(8883, request.BrokerTlsPort);
            Assert.Equal(new string('a', 64), request.BrokerFingerprint);
            Assert.Equal(Infrastructure.TestKeys.Digest, request.ImageDigest);
            Assert.Equal(new[] { "/dev/ttyUSB0" }, request.Devices);
            Assert.Equal(new[] { "NET_ADMIN" }, request.CapAdd);
        }

        [Fact]
        public void ActionRequest_OldCorePayload_HasNoCredentialsOrTls()
        {
            var request = JsonConvert.DeserializeObject<ActionRequest>(
                @"{""Action"":1,""ImageName"":""automaticacore/x"",""Tag"":""1.0"",""ImageSource"":""https://hub.docker.com"",""Id"":""00000000-0000-0000-0000-000000000001""}");

            Assert.Equal(SlaveAction.Stop, request.Action);
            Assert.Null(request.ClientKey);
            Assert.Equal(0, request.BrokerTlsPort);
            Assert.Null(request.ImageDigest);
            Assert.Null(request.Devices);
        }

        [Theory]
        [InlineData("\"Start\"", SlaveAction.Start)]
        [InlineData("\"stop\"", SlaveAction.Stop)]
        [InlineData("2", SlaveAction.Restart)]
        [InlineData("3", SlaveAction.Kill)]
        public void ActionRequest_ActionAsNameOrNumber(string action, SlaveAction expected)
        {
            var request = JsonConvert.DeserializeObject<ActionRequest>($@"{{""Action"":{action}}}");

            Assert.Equal(expected, request.Action);
        }

        [Fact]
        public void ActionRequest_PrivilegedOrBindFieldsInThePayload_HaveNowhereToGo()
        {
            // a payload cannot ask for privileged mode, mounts or the docker socket: the model has no such fields
            var request = JsonConvert.DeserializeObject<ActionRequest>(
                @"{""Action"":0,""ImageName"":""automaticacore/x"",""Privileged"":true,""Binds"":[""/var/run/docker.sock:/var/run/docker.sock""],""HostConfig"":{""Privileged"":true}}");

            Assert.Equal("automaticacore/x", request.ImageName);
            Assert.Null(typeof(ActionRequest).GetProperty("Privileged"));
            Assert.Null(typeof(ActionRequest).GetProperty("Binds"));
            Assert.Null(typeof(ActionRequest).GetProperty("HostConfig"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("null")]
        public void ActionRequest_EmptyOrNullPayload_DeserializesToNull(string json)
        {
            Assert.Null(JsonConvert.DeserializeObject<ActionRequest>(json));
        }

        [Theory]
        [InlineData("{")]
        [InlineData("not json")]
        [InlineData(@"{""Action"":""Explode""}")]
        [InlineData(@"{""Id"":""not-a-guid""}")]
        public void ActionRequest_InvalidPayload_Throws(string json)
        {
            Assert.ThrowsAny<JsonException>(() => JsonConvert.DeserializeObject<ActionRequest>(json));
        }

        [Fact]
        public void ActionRequestArray_IsParsed()
        {
            var requests = JsonConvert.DeserializeObject<ActionRequest[]>(
                @"[{""Action"":0,""ImageName"":""automaticacore/a""},{""Action"":1,""ImageName"":""automaticacore/b""}]");

            Assert.Equal(2, requests.Length);
            Assert.Equal(SlaveAction.Stop, requests[1].Action);
        }

        [Fact]
        public void RunningContainerInfo_NeverSerializesTheCredentials()
        {
            var info = new RunningContainerInfo
            {
                DockerId = "abc",
                RequestId = "req",
                Image = "automaticacore/x:1",
                CreateContainerParameters = new CreateContainerParameters { Env = new List<string> { $"AUTOMATICA_SLAVE_PASSWORD={Infrastructure.TestKeys.PluginKey}" } },
                Request = new ActionRequest { ClientKey = Infrastructure.TestKeys.PluginKey }
            };

            var newtonsoft = JsonConvert.SerializeObject(info);
            var systemText = System.Text.Json.JsonSerializer.Serialize(info);

            Assert.DoesNotContain(Infrastructure.TestKeys.PluginKey, newtonsoft);
            Assert.DoesNotContain(Infrastructure.TestKeys.PluginKey, systemText);
            Assert.Contains("abc", systemText);
            Assert.Contains("automaticacore/x:1", systemText);
        }

        [Fact]
        public void Config_Defaults()
        {
            var config = new Automatica.Core.Satellite.Abstraction.Model.Config();

            Assert.Equal(5010, config.Port);
            Assert.Equal(8883, config.TlsPort);
            Assert.Null(config.UseTls);
            Assert.False(config.HasClientKey);
        }

        [Theory]
        [InlineData("AB:CD:EF:01:23:45:67:89:AB:CD:EF:01:23:45:67:89:AB:CD:EF:01:23:45:67:89:AB:CD:EF:01:23:45:67:89")]
        [InlineData("abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789")]
        [InlineData(" ab cd ef 01 23 45 67 89 ab cd ef 01 23 45 67 89 ab cd ef 01 23 45 67 89 ab cd ef 01 23 45 67 89 ")]
        public void NormalizeFingerprint_AcceptsCommonFormats(string input)
        {
            Assert.Equal("abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789", SatelliteRuntime.NormalizeFingerprint(input));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("abcdef0123456789abcdef0123456789abcdef0123456789abcdef012345678")] // 63
        [InlineData("abcdef0123456789abcdef0123456789abcdef0123456789abcdef01234567890")] // 65
        [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
        public void NormalizeFingerprint_RejectsWrongLengthOrNoHex(string input)
        {
            Assert.Null(SatelliteRuntime.NormalizeFingerprint(input));
        }

        [Theory]
        [InlineData("slave/sat1/action", "slave/sat1/action", true)]
        [InlineData("slave/sat1/action", "slave/sat2/action", false)]
        [InlineData("slave/sat1/actions", "slave/sat1/action", false)]
        [InlineData("slave/sat1/action/x", "slave/sat1/action", false)]
        [InlineData("watchdog/sat1/a/b", "watchdog/sat1/+", false)]
        [InlineData("dispatcher/x", "dispatcher/#", true)]
        public void TopicMatch_ExactAndWildcard(string topic, string filter, bool expected)
        {
            Assert.Equal(expected, ApplicationMessageReceivedHandler.IsMatch(topic, filter));
        }
    }
}
