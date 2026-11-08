using System;
using P3.Logic.Lightning.ConstantBrightness;
using P3.Logic.Lightning.LightingCentral;
using P3.Logic.Lightning.RgbLightingController;
using P3.Logic.Lightning.Tests.TestSupport;
using Xunit;
using C = P3.Logic.Lightning.ConstantBrightness.ConstantBrightnessLogicFactory;
using L = P3.Logic.Lightning.LightingCentral.LightingCentralLogicFactory;
using R = P3.Logic.Lightning.RgbLightingController.RgbLightingControllerLogicFactory;

namespace P3.Logic.Lightning.Tests.RgbAndBrightness
{
    public class RgbLightingControllerTests : LightingLogicTest<RgbLightingControllerLogicFactory>
    {
        private void Configure(string scenes = "#FF0000;0,50,100;100")
        {
            Param(R.Scenes, scenes);
            Start();
        }

        [Theory]
        [InlineData(100L, 100, 0, 0)]
        [InlineData(50100000L, 0, 100, 50)]
        [InlineData(100100100L, 100, 100, 100)]
        [InlineData(999999999L, 100, 100, 100)]
        [InlineData(-5L, 0, 0, 0)]
        public void FromLoxone(long value, int r, int g, int b)
        {
            Assert.Equal(new RgbPercent(r, g, b), RgbPercent.FromLoxone(value));
        }

        [Fact]
        public void Parse_AcceptsHexPercentAndLoxone()
        {
            Assert.Equal(new RgbPercent(100, 50, 0), RgbPercent.Parse("#FF8000"));
            Assert.Equal(new RgbPercent(10, 20, 30), RgbPercent.Parse("10, 20, 30"));
            Assert.Equal(new RgbPercent(1, 2, 3), RgbPercent.Parse(3002001));
            Assert.Equal(new RgbPercent(1, 2, 3), RgbPercent.Parse("3002001"));
            Assert.Null(RgbPercent.Parse("#XYZ"));
            Assert.Null(RgbPercent.Parse("red"));
            Assert.Equal(3002001, new RgbPercent(1, 2, 3).Loxone);
            Assert.Equal("#FF8000", new RgbPercent(100, 50, 0).Hex);
        }

        [Fact]
        public void Plus_CyclesTheScenes_AllOutputs()
        {
            Configure();

            Pulse(R.Plus);
            Assert.Equal(1, Last(R.ActiveScene));
            Assert.Equal(100, Last(R.Red));
            Assert.Equal(0, Last(R.Green));
            Assert.Equal(100, Last(R.Combined));
            Assert.Equal("#FF0000", Last(R.Hex));

            Pulse(R.Plus);
            Assert.Equal(2, Last(R.ActiveScene));
            Assert.Equal(100, Last(R.Blue));
            Assert.Equal(100050000, Last(R.Combined));

            Pulse(R.Plus);
            Pulse(R.Plus);
            Assert.Equal(1, Last(R.ActiveScene));
        }

        [Fact]
        public void Minus_FromOff_StartsWithTheLast()
        {
            Configure();
            Pulse(R.Minus);
            Assert.Equal(3, Last(R.ActiveScene));
            Pulse(R.Minus);
            Assert.Equal(2, Last(R.ActiveScene));
        }

        [Fact]
        public void SceneNumber_DirectColor_AllOn_Reset()
        {
            Configure();

            Input(R.Scene, 2);
            Assert.Equal(2, Last(R.ActiveScene));
            Input(R.Scene, 7);
            Assert.Equal(2, Last(R.ActiveScene));

            Input(R.Color, "#00FF00");
            Assert.Equal(-1, Last(R.ActiveScene));
            Assert.Equal(100, Last(R.Green));

            Pulse(R.AllOn);
            Assert.Equal(99, Last(R.ActiveScene));
            Assert.Equal(100100100, Last(R.Combined));

            Pulse(R.Reset);
            Assert.Equal(0, Last(R.ActiveScene));
            Assert.Equal("#000000", Last(R.Hex));
        }

        [Fact]
        public void Disable_IgnoresEveryInput_AndAHeldButtonDoesNotFireAfterwards()
        {
            Configure();
            Input(R.Disable, true);
            Input(R.Plus, true);
            Input(R.Scene, 1);
            Input(R.Color, "#FFFFFF");
            Assert.Empty(Values(R.ActiveScene));

            Input(R.Disable, false);
            Input(R.Plus, true);
            Assert.Empty(Values(R.ActiveScene));
            Input(R.Plus, false);
            Input(R.Plus, true);
            Assert.Equal(1, Last(R.ActiveScene));
        }

        [Fact]
        public void BrokenScenesAreSkipped()
        {
            Assert.Equal(2, RgbLightingControllerLogic.ParseScenes("#FF0000; nope ;#00FF00").Count);
            Assert.Empty(RgbLightingControllerLogic.ParseScenes(""));
        }
    }

    public class ConstantBrightnessTests : LightingLogicTest<ConstantBrightnessLogicFactory>
    {
        private ConstantBrightnessLogic Controller => (ConstantBrightnessLogic)Sut;

        private void Configure()
        {
            Param(C.Target, 500.0);
            Param(C.Hysteresis, 10.0);
            Param(C.Step, 5.0);
            Param(C.Interval, 5.0);
            Start();
        }

        [Fact]
        public void Dark_StepsUpEveryInterval_UntilInTheBand()
        {
            Configure();
            Input(C.Brightness, 200);

            Input(C.Active, true);
            Assert.Equal(5.0, Last(C.Level));
            AdvanceSeconds(5);
            Assert.Equal(10.0, Last(C.Level));

            Input(C.Brightness, 460);
            AdvanceSeconds(5);
            Assert.Equal(10.0, Last(C.Level));
        }

        [Fact]
        public void Bright_StaysOff()
        {
            Configure();
            Input(C.Brightness, 2000);

            Input(C.Active, true);
            AdvanceSeconds(60);

            Assert.Equal(0.0, Last(C.Level));
        }

        [Fact]
        public void TooBright_StepsDown()
        {
            Configure();
            Input(C.Set, 50);
            Input(C.Active, true);
            Input(C.Brightness, 900);

            AdvanceSeconds(5);
            Assert.Equal(45.0, Last(C.Level));
        }

        [Fact]
        public void Level_IsLimitedTo0And100()
        {
            Configure();
            Input(C.Set, 98);
            Input(C.Brightness, 0);
            Input(C.Active, true);
            AdvanceSeconds(30);
            Assert.Equal(100.0, Controller.CurrentLevel);
        }

        [Fact]
        public void ActOff_And_Off_SwitchOff_AndStopTheLoop()
        {
            Configure();
            Input(C.Brightness, 0);
            Input(C.Active, true);
            AdvanceSeconds(10);
            Input(C.Active, false);
            Assert.Equal(0.0, Last(C.Level));
            Assert.Null(Controller.NextTick);

            Input(C.Active, true);
            AdvanceSeconds(10);
            Pulse(C.Off);
            Assert.Equal(0.0, Last(C.Level));
            AdvanceSeconds(60);
            Assert.Equal(0.0, Last(C.Level));
        }

        [Fact]
        public void Set_WithoutAct_JustSetsTheLevel()
        {
            Configure();
            Input(C.Set, 33);
            Assert.Equal(33.0, Last(C.Level));
            Assert.Null(Controller.NextTick);
        }
    }

    public class LightingCentralTests : LightingLogicTest<LightingCentralLogicFactory>
    {
        [Fact]
        public void PulsesAreForwardedAsOneThenZero_OnTheRisingEdge()
        {
            Start();

            Input(L.MoodPlus, true);
            Input(L.MoodPlus, true);
            Input(L.Off, 1);

            Assert.Equal(new object[] { true, false }, Values(L.MoodPlusOut));
            Assert.Equal(new object[] { true, false }, Values(L.OffOut));
        }

        [Fact]
        public void Mood_IsForwardedEveryTime()
        {
            Start();
            Input(L.Mood, 3);
            Input(L.Mood, 3);
            Input(L.Mood, -2);
            Assert.Equal(new object[] { 3, 3 }, Values(L.MoodOut));
        }

        [Fact]
        public void ActiveRooms_AreCounted()
        {
            Start();
            Assert.Equal(0, Last(L.ActiveCount));

            Input(L.Active[0], true);
            Input(L.Active[3], true);
            Input(L.Active[3], true);
            Assert.Equal(2, Last(L.ActiveCount));
            Assert.Equal(true, Last(L.AnyActive));

            Input(L.Active[0], false);
            Input(L.Active[3], false);
            Assert.Equal(0, Last(L.ActiveCount));
            Assert.Equal(false, Last(L.AnyActive));
        }

        [Fact]
        public void DisableMotion_IsForwarded()
        {
            Start();
            Input(L.DisableMotion, true);
            Input(L.DisableMotion, false);
            Assert.Equal(new object[] { true, false }, Values(L.DisableMotionOut));
        }
    }
}
