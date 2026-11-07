using System.Collections.Generic;
using System.Linq;
using P3.Driver.HueBridge.Data;
using P3.Driver.HueBridge.EventArgs;
using Xunit;

namespace P3.Driver.HueBridge.Tests
{
    public class HueDriverLightStateTests
    {
        private static HueSwitchLightData Request(bool? on = null, int? bri = null)
        {
            return new HueSwitchLightData { On = on, Bri = bri };
        }

        [Fact]
        public void SetLightState_UnknownLight_ReturnsNull()
        {
            var driver = HueTestFactory.CreateDriverWithLights(1);

            var result = driver.SetLightState(42, Request(on: true), true);

            Assert.Null(result);
        }

        [Fact]
        public void SetLightState_UnknownLight_DoesNotRaiseSwitchLight()
        {
            var driver = HueTestFactory.CreateDriverWithLights(1);
            var raised = 0;
            driver.SwitchLight += (_, _) => raised++;

            driver.SetLightState(42, Request(on: true), true);

            Assert.Equal(0, raised);
        }

        [Fact]
        public void SetLightState_OnWithoutBri_TurnsOnWithBri100()
        {
            var driver = HueTestFactory.CreateDriverWithLights(1);

            var light = driver.SetLightState(1, Request(on: true), true);

            Assert.True(light.State.On);
            Assert.Equal(100, light.State.Bri);
        }

        [Fact]
        public void SetLightState_OnWithBriZero_TurnsOnWithBri100()
        {
            var driver = HueTestFactory.CreateDriverWithLights(1);

            var light = driver.SetLightState(1, Request(on: true, bri: 0), true);

            Assert.True(light.State.On);
            Assert.Equal(100, light.State.Bri);
        }

        [Fact]
        public void SetLightState_OnWithBri_UsesRequestedBri()
        {
            var driver = HueTestFactory.CreateDriverWithLights(1);

            var light = driver.SetLightState(1, Request(on: true, bri: 42), true);

            Assert.True(light.State.On);
            Assert.Equal(42, light.State.Bri);
        }

        [Fact]
        public void SetLightState_Off_TurnsOffWithBriZero()
        {
            var driver = HueTestFactory.CreateDriverWithLights(1);
            driver.SetLightState(1, Request(on: true, bri: 80), false);

            var light = driver.SetLightState(1, Request(on: false), true);

            Assert.False(light.State.On);
            Assert.Equal(0, light.State.Bri);
        }

        [Fact]
        public void SetLightState_OffWithBri_StaysOff()
        {
            var driver = HueTestFactory.CreateDriverWithLights(1);
            driver.SetLightState(1, Request(on: true), false);

            var light = driver.SetLightState(1, Request(on: false, bri: 200), true);

            Assert.False(light.State.On);
            Assert.Equal(0, light.State.Bri);
        }

        [Fact]
        public void SetLightState_BriOnlyOnLightThatIsOff_IsIgnored()
        {
            var driver = HueTestFactory.CreateDriverWithLights(1);

            var light = driver.SetLightState(1, Request(bri: 127), true);

            Assert.NotNull(light);
            Assert.False(light.State.On);
            Assert.Equal(0, light.State.Bri);
        }

        [Fact]
        public void SetLightState_BriOnlyOnLightThatIsOn_ChangesBriAndStaysOn()
        {
            var driver = HueTestFactory.CreateDriverWithLights(1);
            driver.SetLightState(1, Request(on: true), false);

            var light = driver.SetLightState(1, Request(bri: 127), true);

            Assert.True(light.State.On);
            Assert.Equal(127, light.State.Bri);
        }

        [Fact]
        public void SetLightState_BriOnlyZeroOnLightThatIsOn_ClampsToOneAndStaysOn()
        {
            var driver = HueTestFactory.CreateDriverWithLights(1);
            driver.SetLightState(1, Request(on: true), false);

            var light = driver.SetLightState(1, Request(bri: 0), true);

            Assert.True(light.State.On);
            Assert.Equal(1, light.State.Bri);
        }

        [Fact]
        public void SetLightState_BriOnly_DoesNotRaiseSwitchLight()
        {
            var driver = HueTestFactory.CreateDriverWithLights(1);
            driver.SetLightState(1, Request(on: true), false);
            var raised = 0;
            driver.SwitchLight += (_, _) => raised++;

            driver.SetLightState(1, Request(bri: 10), true);

            Assert.Equal(0, raised);
        }

        [Fact]
        public void SetLightState_EmptyRequest_ChangesNothing()
        {
            var driver = HueTestFactory.CreateDriverWithLights(1);
            driver.SetLightState(1, Request(on: true, bri: 33), false);

            var light = driver.SetLightState(1, Request(), true);

            Assert.True(light.State.On);
            Assert.Equal(33, light.State.Bri);
        }

        [Fact]
        public void SetLightState_InvokeTrue_RaisesSwitchLightWithEffectiveBri()
        {
            var driver = HueTestFactory.CreateDriverWithLights(1);
            var events = new List<HueSwitchLightEventArgs>();
            driver.SwitchLight += (_, e) => events.Add(e);

            driver.SetLightState(1, Request(on: true, bri: 0), true);

            var e = Assert.Single(events);
            Assert.True(e.State);
            Assert.Equal(100, e.Brightness);
            Assert.Same(driver.Lights[1], e.Light);
        }

        [Fact]
        public void SetLightState_InvokeTrueOff_RaisesSwitchLightWithStateFalse()
        {
            var driver = HueTestFactory.CreateDriverWithLights(1);
            var events = new List<HueSwitchLightEventArgs>();
            driver.SwitchLight += (_, e) => events.Add(e);

            driver.SetLightState(1, Request(on: false), true);

            var e = Assert.Single(events);
            Assert.False(e.State);
            Assert.Equal(0, e.Brightness);
        }

        [Fact]
        public void SetLightState_InvokeFalse_DoesNotRaiseSwitchLight()
        {
            var driver = HueTestFactory.CreateDriverWithLights(1);
            var raised = 0;
            driver.SwitchLight += (_, _) => raised++;

            var light = driver.SetLightState(1, Request(on: true), false);

            Assert.Equal(0, raised);
            Assert.True(light.State.On);
        }

        [Fact]
        public void SetLightState_ReturnsTheStoredLightInstance()
        {
            var driver = HueTestFactory.CreateDriverWithLights(2);

            var light = driver.SetLightState(2, Request(on: true), false);

            Assert.Same(driver.Lights[2], light);
            Assert.False(driver.Lights[1].State.On);
        }

        [Fact]
        public void SetAll_On_SwitchesEveryLightAndRaisesOneEventPerLight()
        {
            var driver = HueTestFactory.CreateDriverWithLights(3);
            var events = new List<HueSwitchLightEventArgs>();
            driver.SwitchLight += (_, e) => events.Add(e);

            driver.SetAll(Request(on: true, bri: 50));

            Assert.All(driver.Lights.Values, l =>
            {
                Assert.True(l.State.On);
                Assert.Equal(50, l.State.Bri);
            });
            Assert.Equal(new[] { 1, 2, 3 }, events.Select(e => e.Light.Id).OrderBy(i => i));
        }

        [Fact]
        public void SetAll_Off_SwitchesEveryLightOff()
        {
            var driver = HueTestFactory.CreateDriverWithLights(2);
            driver.SetAll(Request(on: true));

            driver.SetAll(Request(on: false));

            Assert.All(driver.Lights.Values, l => Assert.False(l.State.On));
        }

        [Fact]
        public void SetAll_BriOnly_DimsOnlyLightsThatAreOn()
        {
            var driver = HueTestFactory.CreateDriverWithLights(2);
            driver.SetLightState(1, Request(on: true), false);

            driver.SetAll(Request(bri: 20));

            Assert.True(driver.Lights[1].State.On);
            Assert.Equal(20, driver.Lights[1].State.Bri);
            Assert.False(driver.Lights[2].State.On);
            Assert.Equal(0, driver.Lights[2].State.Bri);
        }

        [Fact]
        public void SetAll_NoLights_DoesNothing()
        {
            var driver = HueTestFactory.CreateDriver();

            driver.SetAll(Request(on: true));

            Assert.Empty(driver.Lights);
        }

        [Fact]
        public void AddLight_AssignsSequentialIdsStartingAtOne()
        {
            var driver = HueTestFactory.CreateDriver();

            var first = driver.AddLight(new HueLight("a", "ua"));
            var second = driver.AddLight(new HueLight("b", "ub"));

            Assert.Equal(1, first);
            Assert.Equal(2, second);
            Assert.Equal(1, driver.Lights[1].Id);
            Assert.Equal("b", driver.Lights[2].Name);
        }

        [Fact]
        public void NewLight_IsOff()
        {
            var light = new HueLight("a", "ua");

            Assert.False(light.State.On);
            Assert.Equal(0, light.State.Bri);
        }

        [Fact]
        public void HueSwitchLightData_Deserialize_BriOnly_LeavesOnNull()
        {
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<HueSwitchLightData>("{\"bri\":127}");

            Assert.Null(data.On);
            Assert.Equal(127, data.Bri);
        }

        [Fact]
        public void HueSwitchLightData_Deserialize_OnOnly_LeavesBriNull()
        {
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<HueSwitchLightData>("{\"on\":false}");

            Assert.False(data.On);
            Assert.Null(data.Bri);
        }
    }
}
