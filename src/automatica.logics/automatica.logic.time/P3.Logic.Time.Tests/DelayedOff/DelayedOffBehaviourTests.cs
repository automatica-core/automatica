using System;
using System.Threading.Tasks;
using P3.Logic.Time.DelayedOff;
using P3.Logic.Time.Tests.TestSupport;
using Xunit;

namespace P3.Logic.Time.Tests.DelayedOff
{
    /// <summary>
    /// DelayedOff: a trigger switches on right away and off after "delay" seconds. The off timer is shortened instead of waiting for it.
    /// </summary>
    public class DelayedOffBehaviourTests : TimeLogicTest<DelayedOffLogicFactory>
    {
        private static readonly Guid Output = DelayedOffLogicFactory.RuleOutput;

        private void Configure(long delay, bool onlyIfTrue = false)
        {
            Interface(DelayedOffLogicFactory.RuleParamDelay).Value = delay;
            Interface(DelayedOffLogicFactory.TriggerOnlyIfTrue).Value = onlyIfTrue;
            Start();
        }

        private void Trigger(object value) => Assert.Empty(Input(DelayedOffLogicFactory.RuleTrigger, value));

        /// <summary>
        /// Lets the running off timer elapse now (the real System.Timers.Timer, shortened to 1 ms)
        /// </summary>
        private async Task ElapseAsync(int expectedCount)
        {
            Assert.True(SutTimer.Enabled);
            // one shot - with AutoReset a 1 ms timer can elapse twice before the handler stops it
            SutTimer.AutoReset = false;
            SutTimer.Interval = 1;
            await Poll.UntilAsync(() => Values(Output).Count >= expectedCount);
        }

        [Fact]
        public void DefaultDelay_IsFiveSeconds()
        {
            Start();

            Trigger(true);

            Assert.Equal(new object[] { true }, Values(Output));
            Assert.True(SutTimer.Enabled);
            Assert.Equal(5000, SutTimer.Interval);
        }

        [Fact]
        public async Task Trigger_SwitchesOnThenOffAfterTheDelay()
        {
            Configure(3);

            Trigger(true);
            Assert.Equal(new object[] { true }, Values(Output));
            Assert.Equal(3000, SutTimer.Interval);

            await ElapseAsync(2);

            Assert.Equal(new object[] { true, false }, Values(Output));
            Assert.False(SutTimer.Enabled);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void DelayZeroOrNegative_SwitchesOnAndOffRightAway(long delay)
        {
            Configure(delay);

            Trigger(true);

            // off has to be the last value
            Assert.Equal(new object[] { true, false }, Values(Output));
            Assert.False(SutTimer.Enabled);
        }

        [Fact]
        public async Task Retrigger_WhileRunning_RestartsTheDelay()
        {
            Configure(2);

            Trigger(true);
            Trigger(true);

            Assert.Equal(new object[] { true, true }, Values(Output));
            Assert.True(SutTimer.Enabled);

            await ElapseAsync(3);
            Assert.Equal(new object[] { true, true, false }, Values(Output));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(0)]
        [InlineData("x")]
        [InlineData(null)]
        public void TriggerAnyValue_WithoutOnlyIfTrue_SwitchesOn(object value)
        {
            Configure(2);

            Trigger(value);

            Assert.Equal(new object[] { true }, Values(Output));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(0)]
        [InlineData("false")]
        public void OnlyIfTrue_FalseTrigger_IsIgnored(object value)
        {
            Configure(2, onlyIfTrue: true);

            Trigger(value);

            Assert.Empty(Values(Output));
            Assert.False(SutTimer.Enabled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(1)]
        [InlineData("true")]
        [InlineData("True")]
        public void OnlyIfTrue_TrueTrigger_SwitchesOn(object value)
        {
            Configure(2, onlyIfTrue: true);

            Trigger(value);

            Assert.Equal(new object[] { true }, Values(Output));
        }

        [Fact]
        public void OnlyIfTrue_NumericString_IsNotATrigger()
        {
            // Convert.ToBoolean("1") throws, the exception is swallowed by the logic base class
            Configure(2, onlyIfTrue: true);

            Input(DelayedOffLogicFactory.RuleTrigger, "1");

            Assert.Empty(Values(Output));
        }

        [Fact]
        public void Reset_WhileRunning_StopsTheTimerAndSwitchesOff()
        {
            Configure(2);
            Trigger(true);

            var result = Input(DelayedOffLogicFactory.RuleReset, true);

            var change = Assert.Single(result);
            Assert.Equal(false, change.Value);
            Assert.False(SutTimer.Enabled);
        }

        [Fact]
        public void Reset_WithoutRunningTimer_SwitchesOff()
        {
            Configure(2);

            var change = Assert.Single(Input(DelayedOffLogicFactory.RuleReset, false));

            Assert.Equal(false, change.Value);
        }

        [Fact]
        public void OnlyIfTrue_FalseReset_IsIgnored()
        {
            Configure(2, onlyIfTrue: true);
            Trigger(true);

            Assert.Empty(Input(DelayedOffLogicFactory.RuleReset, false));
            Assert.True(SutTimer.Enabled);
        }

        [Fact]
        public void DelayChange_WhileRunning_RestartsWithTheNewDelay()
        {
            Configure(5);
            Trigger(true);
            Assert.Equal(5000, SutTimer.Interval);

            Sut.ValueChanged(Interface(DelayedOffLogicFactory.RuleParamDelay), Dispatchable, 7L);

            Assert.True(SutTimer.Enabled);
            Assert.Equal(7000, SutTimer.Interval);
        }

        [Fact]
        public void DelayChange_ToZeroWhileRunning_SwitchesOffRightAway()
        {
            Configure(5);
            Trigger(true);

            Sut.ValueChanged(Interface(DelayedOffLogicFactory.RuleParamDelay), Dispatchable, 0L);

            Assert.Equal(new object[] { true, false }, Values(Output));
        }

        [Fact]
        public void DelayChange_WhileIdle_DoesNotStartTheTimer()
        {
            Configure(5);

            Sut.ValueChanged(Interface(DelayedOffLogicFactory.RuleParamDelay), Dispatchable, 0L);

            Assert.Empty(Values(Output));
            Assert.False(SutTimer.Enabled);
        }

        [Fact]
        public void Stop_StopsTheRunningTimer()
        {
            Configure(5);
            Trigger(true);
            var timer = SutTimer;

            Stop();

            Assert.False(timer.Enabled);
        }

        [Fact]
        public void Factory_Templates()
        {
            var factory = new DelayedOffLogicFactory();

            Assert.Equal("Time.DelayedOff", factory.LogicName);
            Assert.Equal(5, RuleInstance.RuleInterfaceInstance.Count);
            Assert.Equal(5L, Convert.ToInt64(Interface(DelayedOffLogicFactory.RuleParamDelay).Value));
            Assert.False(Convert.ToBoolean(Interface(DelayedOffLogicFactory.TriggerOnlyIfTrue).Value));
        }
    }
}
