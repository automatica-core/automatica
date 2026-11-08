using System;
using P3.Logic.Time.Pwm;
using P3.Logic.Time.Ramp;
using P3.Logic.Time.Tests.TestSupport;
using Xunit;

namespace P3.Logic.Time.Tests.LoxoneBlocks
{
    public class PwmTests : TimeLogicTest<PwmLogicFactory>
    {
        private static readonly Guid Output = PwmLogicFactory.RuleOutput;

        private void Configure(double period)
        {
            Interface(PwmLogicFactory.RuleParamPeriod).Value = period;
            Start();
        }

        private void V(object value) => Assert.Empty(Input(PwmLogicFactory.RuleValue, value));

        [Fact]
        public void DutyCycle_SplitsThePeriod()
        {
            Configure(10);
            V(2.5);
            Assert.Equal(new object[] { true }, Values(Output));
            Assert.Equal(2500, SutTimer.Interval);

            FireTick();
            Assert.Equal(new object[] { true, false }, Values(Output));
            Assert.Equal(7500, SutTimer.Interval);

            FireTick();
            Assert.Equal(new object[] { true, false, true }, Values(Output));
        }

        [Fact]
        public void NewValue_AppliesFromTheNextPhase()
        {
            Configure(10);
            V(5);
            V(8);
            Assert.Equal(5000, SutTimer.Interval);

            FireTick();
            Assert.Equal(2000, SutTimer.Interval, 6);
        }

        [Fact]
        public void ZeroAndFull_HoldTheOutput()
        {
            Configure(10);
            V(0);
            Assert.Equal(new object[] { false }, Values(Output));
            Assert.False(SutTimer.Enabled);

            V(12);
            Assert.Equal(new object[] { false, true }, Values(Output));
            Assert.False(SutTimer.Enabled);

            // a running cycle ends at 100 % with the next phase change
            V(5);
            FireTick();
            V(10);
            FireTick();
            Assert.Equal(new object[] { false, true, false, true }, Values(Output));
            Assert.False(SutTimer.Enabled);
        }

        [Fact]
        public void Off_StopsAndLocks()
        {
            Configure(10);
            V(5);
            Assert.Empty(Input(PwmLogicFactory.RuleOff, true));
            FireTick();
            V(7);
            Assert.Equal(new object[] { true, false }, Values(Output));

            Assert.Empty(Input(PwmLogicFactory.RuleOff, false));
            Assert.Equal(new object[] { true, false, true }, Values(Output));
            Assert.Equal(7000, SutTimer.Interval);
        }

        [Fact]
        public void InvalidValue_IsIgnored()
        {
            Configure(10);
            V("abc");
            Assert.Empty(Values(Output));
        }
    }

    public class RampControllerTests : TimeLogicTest<RampControllerLogicFactory>
    {
        private static readonly Guid Output = RampControllerLogicFactory.RuleOutput;

        private void Configure(double start = 5, double step = 1, double l1 = 7, double l2 = 3)
        {
            // no real tick may come in between the ones fired by the test
            ((RampControllerRule)Sut).TickMilliseconds = 3_600_000;
            Interface(RampControllerLogicFactory.RuleParamStart).Value = start;
            Interface(RampControllerLogicFactory.RuleParamStep).Value = step;
            Interface(RampControllerLogicFactory.RuleParamLevel1).Value = l1;
            Interface(RampControllerLogicFactory.RuleParamLevel2).Value = l2;
            Start();
        }

        private void Ticks(int count)
        {
            for (var i = 0; i < count; i++)
            {
                FireTick();
            }
        }

        [Fact]
        public void StartsAtStartValue_AndRampsToLevel1()
        {
            Configure();
            Assert.True(SutTimer.Enabled);
            Ticks(3);
            Assert.Equal(new object[] { 5d, 6d, 7d }, Values(Output));
            Assert.False(SutTimer.Enabled);
        }

        [Fact]
        public void Select_RampsToLevel2()
        {
            Configure();
            Ticks(3);
            ClearRecorded();

            Assert.Empty(Input(RampControllerLogicFactory.RuleSelect, true));
            Assert.True(SutTimer.Enabled);
            Ticks(5);
            Assert.Equal(new object[] { 6d, 5d, 4d, 3d }, Values(Output));
        }

        [Fact]
        public void LastStep_StopsAtTheTarget()
        {
            Configure(start: 0, step: 4, l1: 10);
            Ticks(4);
            Assert.Equal(new object[] { 0d, 4d, 8d, 10d }, Values(Output));
        }

        [Fact]
        public void ZeroStep_JumpsToTheLevel()
        {
            Configure(step: 0);
            Ticks(2);
            Assert.Equal(new object[] { 5d, 7d }, Values(Output));
        }

        [Fact]
        public void Stop_SetsStartValue_AndHolds()
        {
            Configure();
            Ticks(3);
            Assert.Empty(Input(RampControllerLogicFactory.RuleStop, true));
            Assert.False(SutTimer.Enabled);
            FireTick();
            Assert.Equal(new object[] { 5d, 6d, 7d, 5d }, Values(Output));

            Assert.Empty(Input(RampControllerLogicFactory.RuleStop, false));
            Ticks(2);
            Assert.Equal(new object[] { 5d, 6d, 7d, 5d, 6d, 7d }, Values(Output));
        }

        [Fact]
        public void Off_SetsZero_ThenRampsFromThere()
        {
            Configure(l1: 2);
            Ticks(1);
            Assert.Empty(Input(RampControllerLogicFactory.RuleOff, true));
            FireTick();
            Assert.Equal(new object[] { 5d, 0d }, Values(Output));

            Assert.Empty(Input(RampControllerLogicFactory.RuleOff, false));
            Ticks(3);
            Assert.Equal(new object[] { 5d, 0d, 1d, 2d }, Values(Output));
        }
    }
}
