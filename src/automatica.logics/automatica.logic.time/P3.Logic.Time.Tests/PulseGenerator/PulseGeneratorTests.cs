using System;
using P3.Logic.Time.PulseGenerator;
using P3.Logic.Time.Tests.TestSupport;
using Xunit;

namespace P3.Logic.Time.Tests.PulseGenerator
{
    /// <summary>
    /// Pulse generator: EN = 1 starts with 1, then the output alternates per timer tick. The ticks are fired by the test.
    /// </summary>
    public class PulseGeneratorTests : TimeLogicTest<PulseGeneratorLogicFactory>
    {
        private static readonly Guid Output = PulseGeneratorLogicFactory.RuleOutput;

        private void Configure(double on, double off)
        {
            Interface(PulseGeneratorLogicFactory.RuleParamOn).Value = on;
            Interface(PulseGeneratorLogicFactory.RuleParamOff).Value = off;
            Start();
        }

        private void Enable(object value) => Assert.Empty(Input(PulseGeneratorLogicFactory.RuleEnable, value));

        [Fact]
        public void Enable_StartsWithOn_ThenAlternates()
        {
            Configure(2, 3);

            Enable(true);
            Assert.Equal(new object[] { true }, Values(Output));
            Assert.True(SutTimer.Enabled);
            Assert.Equal(2000, SutTimer.Interval);

            FireTick();
            Assert.Equal(new object[] { true, false }, Values(Output));
            Assert.Equal(3000, SutTimer.Interval);

            FireTick();
            Assert.Equal(new object[] { true, false, true }, Values(Output));
            Assert.Equal(2000, SutTimer.Interval);
        }

        [Fact]
        public void Disable_StopsAndSendsOff()
        {
            Configure(1, 1);
            Enable(1);
            Enable(0);

            Assert.Equal(new object[] { true, false }, Values(Output));
            Assert.False(SutTimer.Enabled);

            // a late tick after the stop does nothing
            FireTick();
            Assert.Equal(new object[] { true, false }, Values(Output));
        }

        [Fact]
        public void RepeatedEnable_DoesNotRestart()
        {
            Configure(1, 1);
            Enable(true);
            Enable(true);
            Enable(false);
            Enable(false);

            Assert.Equal(new object[] { true, false }, Values(Output));
        }

        [Fact]
        public void Times_HaveAMinimum()
        {
            Configure(0, -5);
            Enable(true);
            Assert.Equal(PulseGeneratorRule.MinimumSeconds * 1000, SutTimer.Interval);
        }

        [Fact]
        public void BeforeStart_EnableIsIgnored()
        {
            Enable(true);
            Assert.Empty(Values(Output));
        }
    }
}
