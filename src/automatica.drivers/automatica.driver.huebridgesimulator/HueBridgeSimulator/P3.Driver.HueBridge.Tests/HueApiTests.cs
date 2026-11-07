using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace P3.Driver.HueBridge.Tests
{
    /// <summary>
    /// Requests through the real HueStartup pipeline (SetLightMiddleware and HueController) on a TestServer
    /// </summary>
    public class HueApiTests
    {
        private const string User = HueTestFactory.ValidUser;

        private static Task<HueTestServer> StartAsync(int lights = 2, FakeClock clock = null)
        {
            return HueTestServer.StartAsync(HueTestFactory.CreateAuthorizedDriverWithLights(lights, clock));
        }

        // ---------- PUT /api/{user}/lights/{id}/state ----------

        [Fact]
        public async Task PutLightState_On_SwitchesLightAndReturnsLight()
        {
            await using var server = await StartAsync();

            var (response, json) = await server.PutAsync($"/api/{User}/lights/1/state", "{\"on\":true}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            Assert.True(server.Driver.Lights[1].State.On);
            Assert.Equal(100, server.Driver.Lights[1].State.Bri);
            Assert.False(server.Driver.Lights[2].State.On);
            // the middleware keeps the previous (PascalCase) response format
            Assert.True(json["State"]!.Value<bool>("On"));
            Assert.Equal(100, json["State"]!.Value<int>("Bri"));
        }

        [Fact]
        public async Task PutLightState_OnWithBri_SetsBri()
        {
            await using var server = await StartAsync();

            await server.PutAsync($"/api/{User}/lights/2/state", "{\"on\":true,\"bri\":77}");

            Assert.True(server.Driver.Lights[2].State.On);
            Assert.Equal(77, server.Driver.Lights[2].State.Bri);
        }

        [Fact]
        public async Task PutLightState_Off_SwitchesLightOff()
        {
            await using var server = await StartAsync();
            server.Driver.SetLightState(1, new Data.HueSwitchLightData(true, 50), false);

            await server.PutAsync($"/api/{User}/lights/1/state", "{\"on\":false}");

            Assert.False(server.Driver.Lights[1].State.On);
        }

        [Fact]
        public async Task PutLightState_BriOnlyOnLightThatIsOff_LeavesLightOff()
        {
            await using var server = await StartAsync();

            var (response, _) = await server.PutAsync($"/api/{User}/lights/1/state", "{\"bri\":127}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(server.Driver.Lights[1].State.On);
        }

        [Fact]
        public async Task PutLightState_BriOnlyOnLightThatIsOn_DimsLight()
        {
            await using var server = await StartAsync();
            server.Driver.SetLightState(1, new Data.HueSwitchLightData(true, 100), false);

            await server.PutAsync($"/api/{User}/lights/1/state", "{\"bri\":127}");

            Assert.True(server.Driver.Lights[1].State.On);
            Assert.Equal(127, server.Driver.Lights[1].State.Bri);
        }

        [Fact]
        public async Task PutLightState_RaisesSwitchLight()
        {
            await using var server = await StartAsync();
            var raised = 0;
            server.Driver.SwitchLight += (_, e) => { if (e.Light.Id == 2 && e.State) raised++; };

            await server.PutAsync($"/api/{User}/lights/2/state", "{\"on\":true}");

            Assert.Equal(1, raised);
        }

        [Fact]
        public async Task PutLightState_UnknownUser_ReturnsError1AndDoesNotSwitch()
        {
            await using var server = await StartAsync();

            var (response, json) = await server.PutAsync($"/api/{HueTestFactory.OtherValidUser}/lights/1/state", "{\"on\":true}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            HueAssert.Error(json, 1, "/lights/1/state");
            Assert.False(server.Driver.Lights[1].State.On);
        }

        [Fact]
        public async Task PutLightState_UnknownUser_DoesNotRaiseSwitchLight()
        {
            await using var server = await StartAsync();
            var raised = 0;
            server.Driver.SwitchLight += (_, _) => raised++;

            await server.PutAsync($"/api/{HueTestFactory.OtherValidUser}/lights/1/state", "{\"on\":true}");

            Assert.Equal(0, raised);
        }

        [Fact]
        public async Task PutLightState_InvalidJson_ReturnsError2()
        {
            await using var server = await StartAsync();

            var (_, json) = await server.PutAsync($"/api/{User}/lights/1/state", "{\"on\":tru");

            HueAssert.Error(json, 2, "/lights/1/state");
            Assert.False(server.Driver.Lights[1].State.On);
        }

        [Theory]
        [InlineData("")]
        [InlineData("null")]
        [InlineData("[1,2]")]
        [InlineData("{\"on\":\"maybe\"}")]
        public async Task PutLightState_NotALightStateObject_ReturnsError2(string body)
        {
            await using var server = await StartAsync();

            var (_, json) = await server.PutAsync($"/api/{User}/lights/1/state", body);

            HueAssert.Error(json, 2);
            Assert.False(server.Driver.Lights[1].State.On);
        }

        [Fact]
        public async Task PutLightState_BodyOver4096Bytes_IsRejectedWithError2()
        {
            await using var server = await StartAsync();
            var body = "{\"on\":true}" + new string(' ', 4096);

            var (_, json) = await server.PutAsync($"/api/{User}/lights/1/state", body);

            HueAssert.Error(json, 2);
            Assert.False(server.Driver.Lights[1].State.On);
        }

        [Fact]
        public async Task PutLightState_BodyOfExactly4096Bytes_IsAccepted()
        {
            await using var server = await StartAsync();
            const string json = "{\"on\":true}";
            var body = json + new string(' ', 4096 - json.Length);
            Assert.Equal(4096, body.Length);

            var (_, response) = await server.PutAsync($"/api/{User}/lights/1/state", body);

            Assert.IsType<JObject>(response);
            Assert.True(server.Driver.Lights[1].State.On);
        }

        [Fact]
        public async Task PutLightState_UnknownLight_ReturnsError3()
        {
            await using var server = await StartAsync();

            var (_, json) = await server.PutAsync($"/api/{User}/lights/99/state", "{\"on\":true}");

            HueAssert.Error(json, 3, "/lights/99/state");
        }

        [Fact]
        public async Task PutLightState_NonNumericId_ReturnsError3()
        {
            await using var server = await StartAsync();

            var (_, json) = await server.PutAsync($"/api/{User}/lights/abc/state", "{\"on\":true}");

            HueAssert.Error(json, 3);
        }

        [Fact]
        public async Task PutLightState_ExtraPathSegment_ReturnsError3()
        {
            await using var server = await StartAsync();

            var (_, json) = await server.PutAsync($"/api/{User}/lights/1/extra/state", "{\"on\":true}");

            HueAssert.Error(json, 3);
            Assert.False(server.Driver.Lights[1].State.On);
        }

        [Fact]
        public async Task PutLightState_NotUnderApi_ReturnsError3()
        {
            await using var server = await StartAsync();

            var (_, json) = await server.PutAsync($"/xyz/{User}/lights/1/state", "{\"on\":true}");

            HueAssert.Error(json, 3);
            Assert.False(server.Driver.Lights[1].State.On);
        }

        [Fact]
        public async Task PutLightsAction_IsNotAGroupAction_ReturnsError3()
        {
            await using var server = await StartAsync();

            var (_, json) = await server.PutAsync($"/api/{User}/lights/1/action", "{\"on\":true}");

            HueAssert.Error(json, 3);
            Assert.False(server.Driver.Lights[1].State.On);
        }

        // ---------- PUT /api/{user}/groups/{id}/action ----------

        [Fact]
        public async Task PutGroupAction_Group0OnAndBri_ReturnsOneSuccessPerAttribute()
        {
            await using var server = await StartAsync(3);

            var (_, json) = await server.PutAsync($"/api/{User}/groups/0/action", "{\"on\":true,\"bri\":50}");

            var array = Assert.IsType<JArray>(json);
            Assert.Equal(2, array.Count);
            Assert.True(array[0]["success"]!.Value<bool>("/groups/0/action/on"));
            Assert.Equal(50, array[1]["success"]!.Value<int>("/groups/0/action/bri"));
            Assert.All(server.Driver.Lights.Values, l =>
            {
                Assert.True(l.State.On);
                Assert.Equal(50, l.State.Bri);
            });
        }

        [Fact]
        public async Task PutGroupAction_Group0OffOnly_ReturnsSingleOnEntry()
        {
            await using var server = await StartAsync();
            server.Driver.SetAll(new Data.HueSwitchLightData(true, 100));

            var (_, json) = await server.PutAsync($"/api/{User}/groups/0/action", "{\"on\":false}");

            var array = Assert.IsType<JArray>(json);
            var entry = Assert.Single(array);
            var success = Assert.IsType<JObject>(entry["success"]);
            Assert.Equal("/groups/0/action/on", Assert.Single(success.Properties()).Name);
            Assert.False(success.Value<bool>("/groups/0/action/on"));
            Assert.All(server.Driver.Lights.Values, l => Assert.False(l.State.On));
        }

        [Fact]
        public async Task PutGroupAction_Group0BriOnly_ReturnsSingleBriEntryAndDimsOnlyLightsThatAreOn()
        {
            await using var server = await StartAsync();
            server.Driver.SetLightState(1, new Data.HueSwitchLightData(true, 100), false);

            var (_, json) = await server.PutAsync($"/api/{User}/groups/0/action", "{\"bri\":30}");

            var array = Assert.IsType<JArray>(json);
            var entry = Assert.Single(array);
            Assert.Equal(30, entry["success"]!.Value<int>("/groups/0/action/bri"));
            Assert.Equal(30, server.Driver.Lights[1].State.Bri);
            Assert.False(server.Driver.Lights[2].State.On);
        }

        [Fact]
        public async Task PutGroupAction_Group0EmptyObject_ReturnsEmptyArray()
        {
            await using var server = await StartAsync();

            var (_, json) = await server.PutAsync($"/api/{User}/groups/0/action", "{}");

            Assert.Empty(Assert.IsType<JArray>(json));
        }

        [Fact]
        public async Task PutGroupAction_Group0_RaisesSwitchLightForEveryLight()
        {
            await using var server = await StartAsync(3);
            var ids = new System.Collections.Concurrent.ConcurrentBag<int>();
            server.Driver.SwitchLight += (_, e) => ids.Add(e.Light.Id);

            await server.PutAsync($"/api/{User}/groups/0/action", "{\"on\":true}");

            Assert.Equal(new[] { 1, 2, 3 }, ids.OrderBy(i => i));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(7)]
        public async Task PutGroupAction_OtherGroup_ReturnsError3AndChangesNothing(int group)
        {
            await using var server = await StartAsync();

            var (_, json) = await server.PutAsync($"/api/{User}/groups/{group}/action", "{\"on\":true}");

            HueAssert.Error(json, 3, $"/groups/{group}/action");
            Assert.All(server.Driver.Lights.Values, l => Assert.False(l.State.On));
        }

        [Fact]
        public async Task PutGroupAction_UnknownUser_ReturnsError1AndChangesNothing()
        {
            await using var server = await StartAsync();

            var (_, json) = await server.PutAsync($"/api/{HueTestFactory.OtherValidUser}/groups/0/action", "{\"on\":true}");

            HueAssert.Error(json, 1, "/groups/0/action");
            Assert.All(server.Driver.Lights.Values, l => Assert.False(l.State.On));
        }

        [Fact]
        public async Task PutGroupAction_InvalidJson_ReturnsError2()
        {
            await using var server = await StartAsync();

            var (_, json) = await server.PutAsync($"/api/{User}/groups/0/action", "not json");

            HueAssert.Error(json, 2, "/groups/0/action");
        }

        [Fact]
        public async Task PutGroupState_IsNotAnAction_ReturnsError3()
        {
            await using var server = await StartAsync();

            var (_, json) = await server.PutAsync($"/api/{User}/groups/0/state", "{\"on\":true}");

            HueAssert.Error(json, 3);
            Assert.All(server.Driver.Lights.Values, l => Assert.False(l.State.On));
        }

        // ---------- POST /api (user creation) ----------

        [Fact]
        public async Task PostApi_LinkButtonNotPressed_ReturnsError101()
        {
            await using var server = await StartAsync();

            var (response, json) = await server.PostAsync("/api");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            HueAssert.Error(json, 101);
            Assert.Single(server.Driver.GetUserNames());
        }

        [Fact]
        public async Task PostApi_LinkButtonPressed_ReturnsNewUserThatIsAuthorized()
        {
            await using var server = await StartAsync();
            server.Driver.PressLinkButton();

            var (_, json) = await server.PostAsync("/api");

            var entry = Assert.Single(Assert.IsType<JArray>(json));
            var userName = entry["success"]!.Value<string>("username");
            Assert.Matches("^[0-9a-f]{32}$", userName);
            Assert.True(server.Driver.IsAuthorized(userName));

            var (_, lights) = await server.GetAsync($"/api/{userName}/lights");
            Assert.IsType<JObject>(lights);
        }

        [Fact]
        public async Task PostApi_LinkButtonWindowExpired_ReturnsError101()
        {
            var clock = new FakeClock();
            await using var server = await StartAsync(clock: clock);
            server.Driver.PressLinkButton();
            clock.Advance(HueDriver.DefaultLinkButtonWindow);

            var (_, json) = await server.PostAsync("/api");

            HueAssert.Error(json, 101);
        }

        // ---------- GET ----------

        [Fact]
        public async Task GetUserConfig_Authorized_ReturnsConfigWithoutWhitelistedUsers()
        {
            await using var server = await StartAsync();

            var (response, json) = await server.GetAsync($"/api/{User}/config");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var config = Assert.IsType<JObject>(json);
            Assert.Equal("BSB002", config.Value<string>("modelid"));
            Assert.Equal(server.Driver.BridgeId.ToString(), config.Value<string>("bridgeid"));
            Assert.False(config.Value<bool>("linkbutton"));
            Assert.Empty(Assert.IsType<JObject>(config["whitelist"]));
            Assert.DoesNotContain(User, json.ToString());
        }

        [Fact]
        public async Task GetUserConfig_LinkButtonPressed_ReportsLinkButtonTrue()
        {
            await using var server = await StartAsync();
            server.Driver.PressLinkButton();

            var (_, json) = await server.GetAsync($"/api/{User}/config");

            Assert.True(json.Value<bool>("linkbutton"));
        }

        [Fact]
        public async Task GetUserConfig_UnknownUser_ReturnsError1()
        {
            await using var server = await StartAsync();

            var (response, json) = await server.GetAsync($"/api/{HueTestFactory.OtherValidUser}/config");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            HueAssert.Error(json, 1, "/config");
        }

        [Fact]
        public async Task GetPublicConfig_NoUser_ReturnsBasicBridgeInfo()
        {
            await using var server = await StartAsync();

            var (_, json) = await server.GetAsync("/api/config");

            var config = Assert.IsType<JObject>(json);
            Assert.Equal("Automatica.Core Hue Bridge", config.Value<string>("name"));
            Assert.Equal("BSB002", config.Value<string>("modelid"));
            Assert.Equal(server.Driver.BridgeId.ToString(), config.Value<string>("bridgeid"));
            Assert.False(config.Value<bool>("factorynew"));
            Assert.Null(config["whitelist"]);
        }

        [Fact]
        public async Task GetLights_Authorized_ReturnsAllLightsKeyedById()
        {
            await using var server = await StartAsync(2);
            server.Driver.SetLightState(2, new Data.HueSwitchLightData(true, 60), false);

            var (_, json) = await server.GetAsync($"/api/{User}/lights");

            var lights = Assert.IsType<JObject>(json);
            Assert.Equal(new[] { "1", "2" }, lights.Properties().Select(p => p.Name).OrderBy(n => n));
            Assert.Equal("Light 1", lights["1"]!.Value<string>("name"));
            Assert.Equal("unique-2", lights["2"]!.Value<string>("uniqueid"));
            Assert.True(lights["2"]!["state"]!.Value<bool>("on"));
            Assert.Equal(60, lights["2"]!["state"]!.Value<int>("bri"));
            Assert.Null(lights["1"]!["id"]);
        }

        [Fact]
        public async Task GetLights_UnknownUser_ReturnsError1()
        {
            await using var server = await StartAsync();

            var (_, json) = await server.GetAsync($"/api/{HueTestFactory.OtherValidUser}/lights");

            HueAssert.Error(json, 1, "/lights");
        }

        [Fact]
        public async Task GetLight_Known_ReturnsLight()
        {
            await using var server = await StartAsync();

            var (_, json) = await server.GetAsync($"/api/{User}/lights/1");

            Assert.Equal("Light 1", json.Value<string>("name"));
        }

        [Fact]
        public async Task GetLight_Unknown_ReturnsError3()
        {
            await using var server = await StartAsync();

            var (_, json) = await server.GetAsync($"/api/{User}/lights/5");

            HueAssert.Error(json, 3, "/lights/5");
        }

        [Fact]
        public async Task GetLight_UnknownUser_ReturnsError1()
        {
            await using var server = await StartAsync();

            var (_, json) = await server.GetAsync($"/api/{HueTestFactory.OtherValidUser}/lights/1");

            HueAssert.Error(json, 1, "/lights/1");
        }

        [Fact]
        public async Task GetGroup0_Authorized_ListsAllLights()
        {
            await using var server = await StartAsync(3);

            var (_, json) = await server.GetAsync($"/api/{User}/groups/0");

            var group = Assert.IsType<JObject>(json);
            Assert.Equal("Group 0", group.Value<string>("name"));
            Assert.Equal(new[] { "1", "2", "3" }, group["lights"]!.Values<string>().OrderBy(s => s));
            Assert.NotNull(group["action"]);
        }

        [Fact]
        public async Task GetGroup_Other_ReturnsError3()
        {
            await using var server = await StartAsync();

            var (_, json) = await server.GetAsync($"/api/{User}/groups/2");

            HueAssert.Error(json, 3, "/groups/2");
        }

        [Fact]
        public async Task GetGroup_UnknownUser_ReturnsError1()
        {
            await using var server = await StartAsync();

            var (_, json) = await server.GetAsync($"/api/{HueTestFactory.OtherValidUser}/groups/0");

            HueAssert.Error(json, 1, "/groups/0");
        }

        [Fact]
        public async Task GetFullState_Authorized_ContainsLightsAndConfig()
        {
            await using var server = await StartAsync();

            var (_, json) = await server.GetAsync($"/api/{User}");

            var data = Assert.IsType<JObject>(json);
            Assert.NotNull(data["lights"]?["1"]);
            Assert.Equal("BSB002", data["config"]!.Value<string>("modelid"));
            Assert.Empty(Assert.IsType<JObject>(data["config"]!["whitelist"]));
        }

        [Fact]
        public async Task GetFullState_UnknownUser_ReturnsError1()
        {
            await using var server = await StartAsync();

            var (_, json) = await server.GetAsync($"/api/{HueTestFactory.OtherValidUser}");

            HueAssert.Error(json, 1, "/");
        }

        [Fact]
        public async Task GetRequestOnStatePath_IsNotHandledByTheMiddleware()
        {
            await using var server = await StartAsync();

            var response = await server.Client.GetAsync($"/api/{User}/lights/1/state");
            var text = await response.Content.ReadAsStringAsync();

            Assert.False(server.Driver.Lights[1].State.On);
            Assert.DoesNotContain("\"type\":2", text);
        }
    }
}
