using System;
using P3.Logic.Time.DelayedPulse;
using P3.Logic.Time.OnOffDelay;
using P3.Logic.Time.SavingOnDelay;
using P3.Logic.Time.Tests.TestSupport;
using P3.Logic.Time.WipingRelay;
using Xunit;

namespace P3.Logic.Time.Tests.LoxoneBlocks
{
    public class OnOffDelayTests : TimeLogicTest<OnOffDelayLogicFactory>
    {
        private static readonly Guid Output = OnOffDelayLogicFactory.RuleOutput;

        private void Configure(double on, double off)
        {
            Interface(OnOffDelayLogicFactory.RuleParamOn).Value = on;
            Interface(OnOffDelayLogicFactory.RuleParamOff).Value = off;
            Start();
        }

        private void Tr(object value) => Assert.Empty(Input(OnOffDelayLogicFactory.RuleTrigger, value));
        private void Off(object value) => Assert.Empty(Input(OnOffDelayLogicFactory.RuleOff, value));

        [Fact]
        public void SwitchesOnAndOffDelayed()
        {
            Configure(2, 3);

            Tr(true);
            Assert.Empty(Values(Output));
            Assert.True(SutTimer.Enabled);
            Assert.Equal(2000, SutTimer.Interval);

            FireTick();
            Assert.Equal(new object[] { true }, Values(Output));

            Tr(false);
            Assert.Equal(3000, SutTimer.Interval);
            FireTick();
            Assert.Equal(new object[] { true, false }, Values(Output));
        }

        [Fact]
        public void ShortInputPulse_IsSwallowed()
        {
            Configure(2, 3);
            Tr(1);
            Tr(0);
            Assert.False(SutTimer.Enabled);

            FireTick();
            Assert.Empty(Values(Output));
        }

        [Fact]
        public void ShortGap_KeepsOutputOn()
        {
            Configure(1, 5);
            Tr(true);
            FireTick();
            Tr(false);
            Tr(true);
            FireTick();
            Assert.Equal(new object[] { true }, Values(Output));
        }

        [Fact]
        public void RepeatedInput_DoesNotRestartTheDelay()
        {
            Configure(2, 3);
            Tr(true);
            Tr(true);
            FireTick();
            Assert.Equal(new object[] { true }, Values(Output));
        }

        [Fact]
        public void ZeroDelays_FollowAtOnce()
        {
            Configure(0, 0);
            Tr(true);
            Tr(false);
            Assert.Equal(new object[] { true, false }, Values(Output));
        }

        [Fact]
        public void Off_ResetsAndLocks_ThenFollowsAgain()
        {
            Configure(1, 1);
            Tr(true);
            FireTick();
            Off(true);
            Assert.Equal(new object[] { true, false }, Values(Output));

            Tr(false);
            Tr(true);
            FireTick();
            Assert.Equal(new object[] { true, false }, Values(Output));

            // Tr is still 1, the on delay starts again after the release
            Off(false);
            Assert.True(SutTimer.Enabled);
            FireTick();
            Assert.Equal(new object[] { true, false, true }, Values(Output));
        }
    }

    public class SavingOnDelayTests : TimeLogicTest<SavingOnDelayLogicFactory>
    {
        private static readonly Guid Output = SavingOnDelayLogicFactory.RuleOutput;

        private void Configure(double delay)
        {
            Interface(SavingOnDelayLogicFactory.RuleParamDelay).Value = delay;
            Start();
        }

        private void Tr(object value) => Assert.Empty(Input(SavingOnDelayLogicFactory.RuleTrigger, value));
        private void Off(object value) => Assert.Empty(Input(SavingOnDelayLogicFactory.RuleOff, value));

        [Fact]
        public void Pulse_IsRemembered_UntilOff()
        {
            Configure(4);
            Tr(true);
            Tr(false);
            Assert.Empty(Values(Output));
            Assert.Equal(4000, SutTimer.Interval);

            FireTick();
            Assert.Equal(new object[] { true }, Values(Output));

            Tr(true);
            Assert.False(SutTimer.Enabled);

            Off(true);
            Off(false);
            Assert.Equal(new object[] { true, false }, Values(Output));
        }

        [Fact]
        public void ZeroDelay_SwitchesOnAtOnce()
        {
            Configure(0);
            Tr(1);
            Assert.Equal(new object[] { true }, Values(Output));
        }

        [Fact]
        public void Off_CancelsThePendingDelay_AndLocks()
        {
            Configure(4);
            Tr(true);
            Off(true);
            FireTick();
            Tr(true);
            Assert.Equal(new object[] { false }, Values(Output));

            Off(false);
            Tr(true);
            FireTick();
            Assert.Equal(new object[] { false, true }, Values(Output));
        }
    }

    public class EdgeWipingRelayTests : TimeLogicTest<EdgeWipingRelayLogicFactory>
    {
        private static readonly Guid Output = EdgeWipingRelayLogicFactory.RuleOutput;

        private void Configure(double on, double off, int cycles)
        {
            Interface(EdgeWipingRelayLogicFactory.RuleParamOn).Value = on;
            Interface(EdgeWipingRelayLogicFactory.RuleParamOff).Value = off;
            Interface(EdgeWipingRelayLogicFactory.RuleParamCycles).Value = cycles;
            Start();
        }

        private void Tr(object value) => Assert.Empty(Input(EdgeWipingRelayLogicFactory.RuleTrigger, value));

        [Fact]
        public void Pulse_SendsTheConfiguredNumberOfPulses()
        {
            Configure(1, 2, 3);
            Tr(true);
            Assert.Equal(1000, SutTimer.Interval);
            Tr(false);

            FireTick();
            Assert.Equal(2000, SutTimer.Interval);
            for (var i = 0; i < 4; i++)
            {
                FireTick();
            }

            Assert.Equal(new object[] { true, false, true, false, true, false }, Values(Output));
            Assert.False(SutTimer.Enabled);

            FireTick();
            Assert.Equal(6, Values(Output).Count);
        }

        [Fact]
        public void PulseDuringBurst_StartsItAgain()
        {
            Configure(1, 1, 2);
            Tr(true);
            FireTick(); // 1st pulse done
            Tr(true);   // restart
            FireTick();
            FireTick();
            FireTick();

            Assert.Equal(new object[] { true, false, true, false, true, false }, Values(Output));
            Assert.False(SutTimer.Enabled);
        }

        [Fact]
        public void Off_EndsTheBurst()
        {
            Configure(1, 1, 5);
            Tr(true);
            Assert.Empty(Input(EdgeWipingRelayLogicFactory.RuleOff, true));
            FireTick();
            Tr(true);

            Assert.Equal(new object[] { true, false }, Values(Output));
        }

        [Fact]
        public void Parameters_HaveMinimums()
        {
            Configure(0, -1, 0);
            Tr(true);
            Assert.Equal(EdgeWipingRelayRule.MinimumSeconds * 1000, SutTimer.Interval);
            FireTick();
            // at least one pulse
            Assert.Equal(new object[] { true, false }, Values(Output));
            Assert.False(SutTimer.Enabled);
        }
    }

    public class DelayedPulseTests : TimeLogicTest<DelayedPulseLogicFactory>
    {
        private static readonly Guid Output = DelayedPulseLogicFactory.RuleOutput;

        private void Configure(double delay, double pulse)
        {
            Interface(DelayedPulseLogicFactory.RuleParamDelay).Value = delay;
            Interface(DelayedPulseLogicFactory.RuleParamPulse).Value = pulse;
            Start();
        }

        private void P(object value) => Assert.Empty(Input(DelayedPulseLogicFactory.RuleTrigger, value));

        [Fact]
        public void Pulse_ComesAfterTheDelay()
        {
            Configure(5, 0.5);
            P(true);
            P(false);
            Assert.Empty(Values(Output));
            Assert.Equal(5000, SutTimer.Interval);

            FireTick();
            Assert.Equal(new object[] { true }, Values(Output));
            Assert.Equal(500, SutTimer.Interval);

            FireTick();
            Assert.Equal(new object[] { true, false }, Values(Output));
        }

        [Fact]
        public void PulseDuringOutputPulse_IsIgnored()
        {
            Configure(5, 1);
            P(true);
            FireTick();
            P(true);
            Assert.Equal(1000, SutTimer.Interval);
            FireTick();
            FireTick();

            Assert.Equal(new object[] { true, false }, Values(Output));
        }

        [Fact]
        public void ZeroDelay_PulsesAtOnce()
        {
            Configure(0, 1);
            P(1);
            Assert.Equal(new object[] { true }, Values(Output));
        }

        [Fact]
        public void Off_CancelsThePendingPulse()
        {
            Configure(5, 1);
            P(true);
            Assert.Empty(Input(DelayedPulseLogicFactory.RuleOff, true));
            FireTick();
            P(true);

            Assert.Equal(new object[] { false }, Values(Output));
        }
    }
}
