using System;
using System.Linq;
using P3.Logic.Lightning.ComfortSwitch;
using P3.Logic.Lightning.Common;
using P3.Logic.Lightning.StairwellLightSwitch;
using P3.Logic.Lightning.Tests.TestSupport;
using Xunit;

namespace P3.Logic.Lightning.Tests.TimedSwitch
{
    public class StairwellLightSwitchTests : LightingLogicTest<StairwellLightSwitchLogicFactory>
    {
        private static readonly TimedSwitchIds Ids = StairwellLightSwitchLogicFactory.Ids;
        private TimedSwitchLogic Switch => (TimedSwitchLogic)Sut;

        private void Configure(double on = 180, double warning = 15, int blinkMs = 500)
        {
            Param(Ids.OnDuration, on);
            Param(Ids.WarningTime, warning);
            Param(Ids.WarningDuration, blinkMs);
            Start();
        }

        private void Trigger() => Pulse(StairwellLightSwitchLogicFactory.Trigger);

        [Fact]
        public void Trigger_OnForTheDuration_WithOneWarningBlink()
        {
            Configure();

            Trigger();
            Assert.Equal(new object[] { true }, Values(Ids.Output));

            AdvanceSeconds(164);
            Assert.Equal(new object[] { true }, Values(Ids.Output));
            AdvanceSeconds(1);
            Assert.Equal(new object[] { true, false }, Values(Ids.Output));
            AdvanceSeconds(0.5);
            Assert.Equal(new object[] { true, false, true }, Values(Ids.Output));
            AdvanceSeconds(14.5);
            Assert.Equal(new object[] { true, false, true, false }, Values(Ids.Output));
            Assert.Null(Switch.NextTick);
        }

        [Fact]
        public void Trigger_Again_RestartsTheTime()
        {
            Configure(60, 0);
            Trigger();
            AdvanceSeconds(50);

            Trigger();
            AdvanceSeconds(50);
            Assert.True(Switch.IsOn);
            AdvanceSeconds(10);
            Assert.False(Switch.IsOn);
        }

        [Fact]
        public void Trigger_DuringTheWarning_TurnsTheLightBackOn()
        {
            Configure(60, 10, 2000);
            Trigger();
            AdvanceSeconds(51);
            Assert.Equal(false, Last(Ids.Output));

            Trigger();

            Assert.Equal(true, Last(Ids.Output));
            Assert.Equal(Now.AddSeconds(60), Switch.OffAt);
        }

        [Fact]
        public void WarningTimeZero_NoBlink()
        {
            Configure(30, 0);
            Trigger();
            AdvanceSeconds(30);
            Assert.Equal(new object[] { true, false }, Values(Ids.Output));
        }

        [Fact]
        public void On_IsPermanent_TriggerDoesNotStartATimer_OffEndsIt()
        {
            Configure(30);
            Pulse(Ids.On);
            Trigger();
            AdvanceSeconds(3600);
            Assert.Equal(true, Last(Ids.Output));
            Assert.True(Switch.IsPermanent);

            Pulse(Ids.Off);
            Assert.Equal(false, Last(Ids.Output));

            Trigger();
            Assert.Equal(true, Last(Ids.Output));
            Assert.NotNull(Switch.OffAt);
        }

        [Fact]
        public void Off_WhileTimed_SwitchesOffAndCancels()
        {
            Configure();
            Trigger();
            Pulse(Ids.Off);

            Assert.Equal(false, Last(Ids.Output));
            Assert.Null(Switch.NextTick);
        }

        [Fact]
        public void PresenceSimulation_SwitchesInTheWindow_AndOffAfterwards()
        {
            Param(Ids.PresenceSimulation.MinActivations, 2);
            Param(Ids.PresenceSimulation.MaxActivations, 2);
            Configure();
            Switch.PresenceSimulation.Simulation.UseRandom(new Random(4));
            Input(Ids.PresenceSimulation.Enable, true);

            AdvanceSeconds(6 * 3600);
            var plan = Switch.PresenceSimulation.Simulation.Plan.ToList();
            Assert.Equal(2, plan.Count);

            Advance(plan[0].Start - Now);
            Assert.True(Switch.IsOn);
            Advance(plan[0].End - Now);
            Assert.False(Switch.IsOn);

            Advance(TimeSpan.FromHours(6));
            Assert.False(Switch.IsOn);
            Assert.Equal(2, Values(Ids.Output).Count(v => (bool)v));
        }

        [Fact]
        public void Recording_OnlyManualChanges()
        {
            Configure(30, 0);
            Trigger();
            AdvanceSeconds(30);

            var events = Switch.PresenceSimulation.Simulation.Events;
            Assert.Equal(new[] { true, false }, events.Select(e => e.On).ToArray());
        }
    }

    public class ComfortSwitchTests : LightingLogicTest<ComfortSwitchLogicFactory>
    {
        private static readonly TimedSwitchIds Ids = ComfortSwitchLogicFactory.Ids;
        private static readonly Guid Tg = ComfortSwitchLogicFactory.Trigger;
        private TimedSwitchLogic Switch => (TimedSwitchLogic)Sut;

        private void Configure()
        {
            Param(Ids.OnDuration, 120.0);
            Param(Ids.WarningTime, 10.0);
            Param(ComfortSwitchLogicFactory.LongClick, 500);
            Start();
        }

        private void ShortPress()
        {
            Input(Tg, true);
            AdvanceSeconds(0.2);
            Input(Tg, false);
        }

        [Fact]
        public void ShortPress_OnForTheDuration_ShortPressAgainOff()
        {
            Configure();

            ShortPress();
            Assert.True(Switch.IsOn);
            Assert.NotNull(Switch.OffAt);

            ShortPress();
            Assert.False(Switch.IsOn);
            Assert.Equal(new object[] { true, false }, Values(Ids.Output));
        }

        [Fact]
        public void ShortPress_TimesOut_WithWarning()
        {
            Configure();
            ShortPress();

            AdvanceSeconds(120);

            Assert.Equal(new object[] { true, false, true, false }, Values(Ids.Output));
        }

        [Fact]
        public void LongPress_PermanentOn_WhileStillHeld()
        {
            Configure();

            Input(Tg, true);
            AdvanceSeconds(0.5);
            Assert.True(Switch.IsPermanent);
            Input(Tg, false);

            AdvanceSeconds(3600);
            Assert.True(Switch.IsOn);

            ShortPress();
            Assert.False(Switch.IsOn);
        }

        [Fact]
        public void ReleasedJustBeforeTheLongClick_IsShort()
        {
            Configure();
            Input(Tg, true);
            AdvanceSeconds(0.49);
            Input(Tg, false);

            Assert.False(Switch.IsPermanent);
            Assert.True(Switch.IsOn);
        }

        [Fact]
        public void OnAndOffInputs()
        {
            Configure();
            Pulse(Ids.On);
            Assert.True(Switch.IsPermanent);
            Pulse(Ids.Off);
            Assert.False(Switch.IsOn);
        }

        [Fact]
        public void Factory_Defaults()
        {
            Assert.Equal(180.0, GetLogicInterfaceByTemplate(Ids.OnDuration).Value);
            Assert.Equal(15.0, GetLogicInterfaceByTemplate(Ids.WarningTime).Value);
            Assert.Equal(500L, Convert.ToInt64(GetLogicInterfaceByTemplate(Ids.WarningDuration).Value));
            Assert.Equal(500L, Convert.ToInt64(GetLogicInterfaceByTemplate(ComfortSwitchLogicFactory.LongClick).Value));
        }
    }
}
