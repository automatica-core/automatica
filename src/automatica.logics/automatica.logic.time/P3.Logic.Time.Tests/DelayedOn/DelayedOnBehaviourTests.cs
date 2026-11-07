using System;
using System.Threading.Tasks;
using P3.Logic.Time.DelayedOn;
using P3.Logic.Time.Tests.TestSupport;
using Xunit;

namespace P3.Logic.Time.Tests.DelayedOn
{
    /// <summary>
    /// DelayedOn: a trigger switches on after "delay" seconds. The timer is shortened instead of waiting for it.
    /// </summary>
    public class DelayedOnBehaviourTests : TimeLogicTest<DelayedOnLogicFactory>
    {
        private static readonly Guid Output = DelayedOnLogicFactory.RuleOutput;

        private void Configure(long delay, bool onlyIfTrue = false)
        {
            Interface(DelayedOnLogicFactory.RuleParamDelay).Value = delay;
            Interface(DelayedOnLogicFactory.TriggerOnlyIfTrue).Value = onlyIfTrue;
            Start();
        }

        private void Trigger(object value) => Assert.Empty(Input(DelayedOnLogicFactory.RuleTrigger, value));

        private async Task ElapseAsync(int expectedCount)
        {
            Assert.True(SutTimer.Enabled);
            // one shot - with AutoReset a 1 ms timer can elapse twice before the handler stops it
            SutTimer.AutoReset = false;
            SutTimer.Interval = 1;
            await Poll.UntilAsync(() => Values(Output).Count >= expectedCount);
        }

        [Fact]
        public void Start_PushesTheParameters()
        {
            Configure(3, onlyIfTrue: true);

            var logic = (DelayedOnRule)Sut;
            Assert.Equal(3, logic.Delay);
            Assert.True(logic.TriggerOnlyIfTrue);
        }

        [Fact]
        public async Task Trigger_SwitchesOnAfterTheDelay()
        {
            Configure(3);

            Trigger(true);
            Assert.Empty(Values(Output));
            Assert.Equal(3000, SutTimer.Interval);

            await ElapseAsync(1);

            Assert.Equal(new object[] { true }, Values(Output));
            Assert.False(SutTimer.Enabled);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void DelayZeroOrNegative_SwitchesOnRightAway(long delay)
        {
            Configure(delay);

            Trigger(true);

            Assert.Equal(new object[] { true }, Values(Output));
            Assert.False(SutTimer.Enabled);
        }

        [Fact]
        public async Task Retrigger_WhileRunning_RestartsAndSwitchesOnOnce()
        {
            Configure(2);

            Trigger(true);
            Trigger(true);
            Assert.Empty(Values(Output));

            await ElapseAsync(1);
            await Task.Delay(50);

            Assert.Equal(new object[] { true }, Values(Output));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(0)]
        [InlineData(null)]
        public void TriggerAnyValue_WithoutOnlyIfTrue_StartsTheDelay(object value)
        {
            Configure(2);

            Trigger(value);

            Assert.True(SutTimer.Enabled);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(0)]
        [InlineData("false")]
        public void OnlyIfTrue_FalseTrigger_IsIgnored(object value)
        {
            Configure(0, onlyIfTrue: true);

            Trigger(value);

            Assert.Empty(Values(Output));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(1)]
        [InlineData("true")]
        public void OnlyIfTrue_TrueTrigger_SwitchesOn(object value)
        {
            Configure(0, onlyIfTrue: true);

            Trigger(value);

            Assert.Equal(new object[] { true }, Values(Output));
        }

        [Fact]
        public async Task Reset_WhileRunning_CancelsTheSwitchOn()
        {
            Configure(2);
            Trigger(true);

            var change = Assert.Single(Input(DelayedOnLogicFactory.RuleReset, true));
            Assert.Equal(false, change.Value);
            Assert.False(SutTimer.Enabled);

            // the cancelled timer must not fire later
            SutTimer.Interval = 1;
            await Task.Delay(50);
            Assert.Empty(Values(Output));
        }

        [Fact]
        public void OnlyIfTrue_FalseReset_IsIgnored()
        {
            Configure(2, onlyIfTrue: true);
            Trigger(true);

            Assert.Empty(Input(DelayedOnLogicFactory.RuleReset, false));
            Assert.True(SutTimer.Enabled);
        }

        [Fact]
        public void DelayChange_WhileRunning_RestartsWithTheNewDelay()
        {
            Configure(5);
            Trigger(true);

            Sut.ValueChanged(Interface(DelayedOnLogicFactory.RuleParamDelay), Dispatchable, 9L);

            Assert.True(SutTimer.Enabled);
            Assert.Equal(9000, SutTimer.Interval);
        }

        [Fact]
        public void DelayChange_ToZeroWhileRunning_SwitchesOnRightAway()
        {
            Configure(5);
            Trigger(true);

            Sut.ValueChanged(Interface(DelayedOnLogicFactory.RuleParamDelay), Dispatchable, 0L);

            Assert.Equal(new object[] { true }, Values(Output));
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
            var factory = new DelayedOnLogicFactory();

            Assert.Equal("Time.DelayedOn", factory.LogicName);
            Assert.Equal(5, RuleInstance.RuleInterfaceInstance.Count);
            Assert.Equal(5L, Convert.ToInt64(Interface(DelayedOnLogicFactory.RuleParamDelay).Value));
        }
    }
}
