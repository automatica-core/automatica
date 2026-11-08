using Xunit;
using F = P3.Logic.Math.BasicOperations.WindGauge.WindGaugeLogicFactory;

namespace P3.Logic.Math.BasicOperations.Tests.WindGauge
{
    public class WindGaugeTests : BlockTest<F>
    {
        private void Tick(double seconds)
        {
            Clock.Advance(seconds);
            Fire();
        }

        private object Gust => Last(F.OutputGust);
        private object Average => Last(F.OutputAverage);
        private object Maximum => Last(F.OutputMaximum);
        private object Alarm => Last(F.OutputAlarm);

        [Fact]
        public void Factory_Defaults()
        {
            Assert.Equal(10.0, Interface(F.ParamAveragingTime).Value);
            Assert.Equal(1.0, Interface(F.ParamFactor).Value);
            Assert.Equal(50.0, Interface(F.ParamAlarmSpeed).Value);
            Assert.Equal(1 + 4 + 3, RuleInstance.RuleInterfaceInstance.Count);
        }

        [Fact]
        public void ConstantFrequency_TimesFactor()
        {
            Param(F.ParamFactor, 2.4);
            Start();
            Assert.Equal(false, Alarm);
            Assert.True(Timer().Enabled);
            Assert.Equal(1000, Timer().Interval);

            Input(F.InputFrequency, 10);
            Assert.Equal(24.0, Gust);
            Assert.Equal(24.0, Average);
            Assert.Equal(24.0, Maximum);

            Tick(30);
            Assert.Equal(24.0, Average);
        }

        [Fact]
        public void Average_WeighsEachValueWithItsTime()
        {
            Start();
            Input(F.InputFrequency, 0);
            Clock.Advance(300);
            Input(F.InputFrequency, 10);
            Tick(300);

            Assert.Equal(5.0, Average);
            Assert.Equal(10.0, Gust);
            Assert.Equal(10.0, Maximum);
        }

        [Fact]
        public void Average_SlidesWithTheAveragingTime()
        {
            Param(F.ParamAveragingTime, 1.0);
            Start();
            Input(F.InputFrequency, 20);
            Clock.Advance(60);
            Input(F.InputFrequency, 0);
            Tick(30);
            Assert.Equal(10.0, Average);

            Tick(30);
            Assert.Equal(0.0, Average);
        }

        [Fact]
        public void Gust_IsTheLastThreeSeconds()
        {
            Start();
            Input(F.InputFrequency, 10);
            Clock.Advance(100);
            Input(F.InputFrequency, 40);
            Tick(1.5);
            Assert.Equal(25.0, Gust);
            Tick(1.5);
            Assert.Equal(40.0, Gust);
        }

        [Fact]
        public void Maximum_IsTheHighestGustInTheAveragingTime()
        {
            Param(F.ParamAveragingTime, 2.0);
            Start();
            Input(F.InputFrequency, 30);
            Tick(3);
            Input(F.InputFrequency, 10);
            Tick(3);
            Assert.Equal(30.0, Maximum);

            Tick(121);
            Assert.Equal(10.0, Maximum);
        }

        [Fact]
        public void Alarm_OnAtTheLimit_OffWhenGustAndAverageBelowNinetyPercent()
        {
            Param(F.ParamAlarmSpeed, 50.0);
            Start();
            Input(F.InputFrequency, 60);
            Tick(3);
            Assert.Equal(true, Alarm);

            Input(F.InputFrequency, 46);
            Tick(3);
            Assert.Equal(true, Alarm);

            Input(F.InputFrequency, 40);
            Tick(3);
            // the gust is below 45, the average not yet
            Assert.Equal(true, Alarm);

            Tick(60);
            Assert.Equal(false, Alarm);
        }

        [Fact]
        public void InvalidValues_AreIgnored()
        {
            Start();
            Input(F.InputFrequency, "abc");
            Input(F.InputFrequency, null);
            Assert.Null(Gust);
            Input(F.InputFrequency, -5);
            Assert.Equal(0.0, Gust);
        }

        [Fact]
        public void Stop_StopsTheTimer()
        {
            Start();
            Sut.Stop().GetAwaiter().GetResult();
            Assert.False(Timer().Enabled);
        }
    }
}
