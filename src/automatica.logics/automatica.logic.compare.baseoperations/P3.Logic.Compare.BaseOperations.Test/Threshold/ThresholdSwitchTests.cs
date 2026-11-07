using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Compare.BaseOperations.Threshold;
using Xunit;

namespace P3.Logic.Compare.BaseOperations.Tests.Threshold
{
    public class ThresholdSwitchTests : LogicTest<ThresholdSwitchLogicFactory>
    {
        private void Thresholds(double on, double off)
        {
            Logic.ValueChanged(GetLogicInterfaceByTemplate(ThresholdSwitchLogicFactory.RuleParamOn), Dispatchable, on);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(ThresholdSwitchLogicFactory.RuleParamOff), Dispatchable, off);
        }

        private object Send(object value)
        {
            var result = Logic.ValueChanged(GetLogicInterfaceByTemplate(ThresholdSwitchLogicFactory.RuleInput), Dispatchable, value);
            return result.Count == 0 ? null : result[0].Value;
        }

        [Fact]
        public void OnAboveOff_SwitchesWithHysteresis()
        {
            // fan: on from 27, off from 25
            Thresholds(27, 25);

            Assert.Equal(false, Send(26));
            Assert.Equal(true, Send(27));
            Assert.Equal(true, Send(26));   // between the thresholds: unchanged
            Assert.Equal(true, Send(25.1));
            Assert.Equal(false, Send(25));
            Assert.Equal(false, Send(26.9));
        }

        [Fact]
        public void OnBelowOff_IsHeatingMode()
        {
            // heating: on from 20, off from 22
            Thresholds(20, 22);

            Assert.Equal(false, Send(21));
            Assert.Equal(true, Send(19.5));
            Assert.Equal(true, Send(21.9));
            Assert.Equal(false, Send(22));
            Assert.Equal(false, Send(20.5));
        }

        [Fact]
        public void Defaults_AreOneAndZero()
        {
            Assert.Equal(true, Send(1));
            Assert.Equal(true, Send(0.5));
            Assert.Equal(false, Send(0));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("abc")]
        public void InvalidValue_SendsNothing(object value)
        {
            Assert.Null(Send(value));
        }

        [Fact]
        public void StringNumbers_UseInvariantCulture()
        {
            Thresholds(27, 25);
            Assert.Equal(true, Send("27.5"));
        }
    }
}
