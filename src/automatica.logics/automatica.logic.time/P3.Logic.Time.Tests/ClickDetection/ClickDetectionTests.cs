using System;
using P3.Logic.Time.ClickDetection;
using P3.Logic.Time.Tests.TestSupport;
using Xunit;

namespace P3.Logic.Time.Tests.ClickDetection
{
    /// <summary>
    /// Click detection: press = 1, release = 0. The test fires the timer instead of waiting for the click window / long press time.
    /// </summary>
    public class ClickDetectionTests : TimeLogicTest<ClickDetectionLogicFactory>
    {
        private static readonly Guid Single = ClickDetectionLogicFactory.RuleSingle;
        private static readonly Guid Double = ClickDetectionLogicFactory.RuleDouble;
        private static readonly Guid Triple = ClickDetectionLogicFactory.RuleTriple;
        private static readonly Guid Long = ClickDetectionLogicFactory.RuleLong;
        private static readonly object[] Pulse = { true, false };

        private void Configure(int clickWindow = 400, int longPress = 800)
        {
            Interface(ClickDetectionLogicFactory.RuleParamClickWindow).Value = clickWindow;
            Interface(ClickDetectionLogicFactory.RuleParamLongPress).Value = longPress;
            Start();
        }

        private void Press() => Assert.Empty(Input(ClickDetectionLogicFactory.RuleInput, true));
        private void Release() => Assert.Empty(Input(ClickDetectionLogicFactory.RuleInput, false));

        private void Click()
        {
            Press();
            Release();
        }

        private void AssertNothingSent()
        {
            Assert.Empty(Values(Single));
            Assert.Empty(Values(Double));
            Assert.Empty(Values(Triple));
            Assert.Empty(Values(Long));
        }

        [Fact]
        public void SingleClick_ReportedAfterTheWindow()
        {
            Configure();
            Press();
            Assert.Equal(800, SutTimer.Interval);
            Release();
            Assert.Equal(400, SutTimer.Interval);
            AssertNothingSent();

            FireTick();
            Assert.Equal(Pulse, Values(Single));
            Assert.Empty(Values(Double));
        }

        [Fact]
        public void DoubleClick()
        {
            Configure();
            Click();
            Click();
            AssertNothingSent();

            FireTick();
            Assert.Equal(Pulse, Values(Double));
            Assert.Empty(Values(Single));
        }

        [Fact]
        public void TripleClick_ReportedImmediately()
        {
            Configure();
            Click();
            Click();
            Click();

            Assert.Equal(Pulse, Values(Triple));
            Assert.Empty(Values(Single));
            Assert.Empty(Values(Double));
            Assert.False(SutTimer.Enabled);
        }

        [Fact]
        public void LongPress_OnUntilRelease_NoClick()
        {
            Configure();
            Press();
            FireTick();
            Assert.Equal(new object[] { true }, Values(Long));

            Release();
            Assert.Equal(new object[] { true, false }, Values(Long));
            Assert.Empty(Values(Single));
        }

        [Fact]
        public void ClickThenLongPress_DropsTheClick()
        {
            Configure();
            Click();
            Press();
            FireTick();
            Release();

            Assert.Equal(new object[] { true, false }, Values(Long));
            Assert.Empty(Values(Single));
            Assert.Empty(Values(Double));
        }

        [Fact]
        public void RepeatedTelegrams_AreIgnored()
        {
            Configure();
            Press();
            Press();
            Release();
            Release();
            FireTick();

            Assert.Equal(Pulse, Values(Single));
        }

        [Fact]
        public void LongPressOff_EveryOneIsAClick()
        {
            // buttons that only send 1
            Configure(longPress: 0);
            Press();
            Press();
            FireTick();

            Assert.Equal(Pulse, Values(Double));
            Assert.Empty(Values(Long));
        }
    }
}
