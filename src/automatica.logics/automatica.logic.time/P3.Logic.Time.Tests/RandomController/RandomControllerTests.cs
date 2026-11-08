using System;
using P3.Logic.Time.RandomController;
using P3.Logic.Time.Tests.TestSupport;
using Xunit;
using F = P3.Logic.Time.RandomController.RandomControllerLogicFactory;

namespace P3.Logic.Time.Tests.RandomController
{
    public class RandomControllerTests : TimeLogicTest<RandomControllerLogicFactory>
    {
        private RandomControllerRule Rule => (RandomControllerRule)Sut;

        private void Configure(double maxOn, double maxOff, params double[] randoms)
        {
            Interface(F.ParamMaxOnDelay).Value = maxOn;
            Interface(F.ParamMaxOffDelay).Value = maxOff;
            Start();
            var index = 0;
            Rule.NextRandom = () => randoms.Length == 0 ? 0.5 : randoms[index++ % randoms.Length];
        }

        [Fact]
        public void Enable_WaitsThenAlternatesWithRandomTimes()
        {
            Configure(100, 10, 0.5, 1.0, 0.0);

            Input(F.InputEnable, true);
            Assert.Empty(Values(F.Output)); // starts in the off phase
            Assert.Equal((1 + 0.5 * 99) * 1000, SutTimer.Interval);

            FireTick();
            Assert.Equal(new object[] { true }, Values(F.Output));
            Assert.Equal(10_000, SutTimer.Interval); // random 1.0 of the off delay

            FireTick();
            Assert.Equal(new object[] { true, false }, Values(F.Output));
            Assert.Equal(1000, SutTimer.Interval); // random 0.0 -> minimum
        }

        [Fact]
        public void Disable_SwitchesOffAndStops()
        {
            Configure(5, 5);
            Input(F.InputEnable, true);
            FireTick();
            Input(F.InputEnable, false);

            Assert.Equal(new object[] { true, false }, Values(F.Output));
            Assert.False(Rule.Running);
            Assert.False(SutTimer.Enabled);

            FireTick();
            Assert.Equal(new object[] { true, false }, Values(F.Output));
        }

        [Fact]
        public void Disable_DuringTheOffPhase_SendsNothing()
        {
            Configure(5, 5);
            Input(F.InputEnable, true);
            Input(F.InputEnable, false);
            Assert.Empty(Values(F.Output));
        }

        [Fact]
        public void RandomSeconds_StaysInRange()
        {
            Configure(0, 0);
            Rule.NextRandom = () => 2; // clamped
            Assert.Equal(RandomControllerRule.MinimumSeconds, Rule.RandomSeconds(0));
            Assert.Equal(30, Rule.RandomSeconds(30));

            var real = new RandomControllerRule(new Automatica.Core.UnitTests.Base.Logics.LogicContextMock(RuleInstance, Factory, Recorder));
            for (var i = 0; i < 100; i++)
            {
                Assert.InRange(real.RandomSeconds(20), 1, 20);
            }
        }
    }
}
