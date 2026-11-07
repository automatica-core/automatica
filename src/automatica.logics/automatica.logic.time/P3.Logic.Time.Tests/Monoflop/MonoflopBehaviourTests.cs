using System;
using System.Threading;
using System.Threading.Tasks;
using P3.Logic.Time.Monoflop;
using P3.Logic.Time.Tests.TestSupport;
using Xunit;

namespace P3.Logic.Time.Tests.Monoflop
{
    /// <summary>
    /// Monoflop: any input switches on, "delay" seconds later it switches off. The pulse timer is fired early instead of waiting for it.
    /// </summary>
    public class MonoflopBehaviourTests : TimeLogicTest<MonoflopLogicFactory>
    {
        private static readonly Guid Output = MonoflopLogicFactory.RuleOutput;

        private void Configure(long delay)
        {
            Interface(MonoflopLogicFactory.RuleParamDelay).Value = delay;
            Start();
        }

        private void Trigger(object value) => Assert.Empty(Input(MonoflopLogicFactory.RuleTrigger, value));

        private System.Threading.Timer PulseTimer => (System.Threading.Timer)GetField("_timer");

        private long Delay => (long)GetField("_delay");

        private async Task ElapseAsync(int expectedCount)
        {
            Assert.NotNull(PulseTimer);
            PulseTimer.Change(0, Timeout.Infinite);
            await Poll.UntilAsync(() => Values(Output).Count >= expectedCount);
        }

        [Fact]
        public async Task Trigger_PulsesOnThenOff()
        {
            Configure(3);

            Trigger(true);
            Assert.Equal(new object[] { true }, Values(Output));

            await ElapseAsync(2);
            Assert.Equal(new object[] { true, false }, Values(Output));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(0)]
        [InlineData("x")]
        [InlineData(null)]
        public void AnyInputValue_StartsAPulse(object value)
        {
            Configure(3);

            Trigger(value);

            Assert.Equal(new object[] { true }, Values(Output));
            Assert.NotNull(PulseTimer);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(10, 10)]
        [InlineData(0, 5)]    // invalid -> 5 s
        [InlineData(-3, 5)]
        public void Delay_IsTakenFromTheParameter(long parameter, long expected)
        {
            Configure(parameter);

            Assert.Equal(expected, Delay);
        }

        [Fact]
        public async Task Retrigger_RestartsThePulse()
        {
            Configure(3);

            Trigger(true);
            var first = PulseTimer;
            Trigger(true);

            Assert.NotSame(first, PulseTimer);
            Assert.Equal(new object[] { true, true }, Values(Output));

            // the replaced timer is disposed and must not switch off
            Assert.False(first.Change(0, Timeout.Infinite));

            await ElapseAsync(3);
            await Task.Delay(50);
            Assert.Equal(new object[] { true, true, false }, Values(Output));
        }

        [Fact]
        public async Task Stop_CancelsTheRunningPulse()
        {
            Configure(3);
            Trigger(true);
            var timer = PulseTimer;

            Stop();

            Assert.False(timer.Change(0, Timeout.Infinite));
            await Task.Delay(50);
            Assert.Equal(new object[] { true }, Values(Output));
        }

        [Fact(Skip = Bugs.MonoflopDelayChangeIgnored)]
        public void DelayChange_WhileRunning_RestartsThePulse()
        {
            Configure(5);
            Trigger(true);
            var first = PulseTimer;

            Sut.ValueChanged(Interface(MonoflopLogicFactory.RuleParamDelay), Dispatchable, 1L);

            Assert.Equal(1, Delay);
            Assert.NotSame(first, PulseTimer);
        }

        [Fact]
        public void DelayChange_WhileIdle_DoesNotPulse()
        {
            Configure(5);

            Sut.ValueChanged(Interface(MonoflopLogicFactory.RuleParamDelay), Dispatchable, 1L);

            Assert.Equal(1, Delay);
            Assert.Empty(Values(Output));
        }

        [Fact]
        public void Factory_Templates()
        {
            var factory = new MonoflopLogicFactory();

            Assert.Equal("Time.Monoflop", factory.LogicName);
            Assert.Equal(3, RuleInstance.RuleInterfaceInstance.Count);
            Assert.Equal(5L, Convert.ToInt64(Interface(MonoflopLogicFactory.RuleParamDelay).Value));
        }
    }
}
