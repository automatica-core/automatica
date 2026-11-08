using System;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Time.RuntimeCounter;
using P3.Logic.Time.Tests.TestSupport;
using Xunit;
using F = P3.Logic.Time.RuntimeCounter.RuntimeCounterLogicFactory;

namespace P3.Logic.Time.Tests.RuntimeCounter
{
    public class RuntimeCounterTests : TimeLogicTest<RuntimeCounterLogicFactory>
    {
        private static readonly DateTime T0 = new(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc);

        private RuntimeCounterRule Rule => (RuntimeCounterRule)Sut;

        private void Configure(RuntimeUnit unit = RuntimeUnit.Hours, double intervalHours = 0)
        {
            SetUtcNow(T0);
            Interface(F.ParamUnit).Value = ((int)unit).ToString();
            Interface(F.ParamInterval).Value = intervalHours;
            Start();
        }

        [Fact]
        public void CountsOnlyWhileEnabled()
        {
            Configure(RuntimeUnit.Minutes);

            Input(F.InputEnable, true);
            Assert.True(Rule.Running);
            Assert.Equal(60_000, SutTimer.Interval);

            SetUtcNow(T0.AddMinutes(30));
            FireTick();
            Assert.Equal(30d, Last(F.OutputTotal));

            Input(F.InputEnable, false);
            Assert.False(Rule.Running);
            SetUtcNow(T0.AddMinutes(90));
            Assert.Equal(1800, Rule.TotalSeconds, 3);

            Input(F.InputEnable, true);
            SetUtcNow(T0.AddMinutes(100));
            Input(F.InputEnable, false);
            Assert.Equal(40d, Last(F.OutputTotal));
            // the test zone is UTC
            Assert.Equal(new DateTime(2026, 11, 8, 11, 30, 0), (DateTime)Last(F.OutputLastStart));
        }

        [Fact]
        public void Maintenance_IsSignalledAndReset()
        {
            Configure(RuntimeUnit.Hours, intervalHours: 2);
            Assert.Equal(false, Last(F.OutputMaintenance));
            Assert.Equal(2d, Last(F.OutputRemaining));

            Input(F.InputEnable, true);
            SetUtcNow(T0.AddHours(1.5));
            FireTick();
            Assert.Equal(0.5, Last(F.OutputRemaining));
            Assert.Equal(false, Last(F.OutputMaintenance));

            SetUtcNow(T0.AddHours(2.25));
            FireTick();
            Assert.Equal(true, Last(F.OutputMaintenance));
            Assert.Equal(0d, Last(F.OutputRemaining));

            Input(F.InputResetMaintenance, true);
            Assert.Equal(false, Last(F.OutputMaintenance));
            Assert.Equal(2d, Last(F.OutputRemaining));
            Assert.Equal(2.25, Last(F.OutputTotal));
        }

        [Fact]
        public void Off_PausesTheCounting()
        {
            Configure(RuntimeUnit.Seconds);
            Input(F.InputEnable, true);
            SetUtcNow(T0.AddSeconds(10));
            Input(F.InputOff, true);
            SetUtcNow(T0.AddSeconds(100));
            Assert.Equal(10, Rule.TotalSeconds, 3);
            Input(F.InputOff, false);
            SetUtcNow(T0.AddSeconds(105));
            Assert.Equal(15, Rule.TotalSeconds, 3);
        }

        [Fact]
        public void ResetTotal_ClearsEverything()
        {
            Configure(RuntimeUnit.Seconds, intervalHours: 1);
            Input(F.InputEnable, true);
            SetUtcNow(T0.AddSeconds(50));
            Input(F.InputResetTotal, true);
            Assert.Equal(0d, Last(F.OutputTotal));
            SetUtcNow(T0.AddSeconds(60));
            Assert.Equal(10, Rule.TotalSeconds, 3);
        }

        [Fact]
        public void Counters_SurviveARestart()
        {
            Configure(RuntimeUnit.Hours, intervalHours: 10);
            Input(F.InputEnable, true);
            SetUtcNow(T0.AddHours(3));
            FireTick();
            Input(F.InputResetMaintenance, true);
            SetUtcNow(T0.AddHours(4));
            Stop(); // saves the running time

            var restarted = (RuntimeCounterRule)Instance.CreateLogicInstance(new LogicContextMock(RuleInstance, Factory, Recorder));
            restarted.Start().GetAwaiter().GetResult();
            try
            {
                Assert.Equal(4 * 3600, restarted.TotalSeconds, 3);
                Assert.False(restarted.Running); // En is unknown after a restart until it is sent again
                Assert.Equal(4d, Last(F.OutputTotal));
                Assert.Equal(9d, Last(F.OutputRemaining));
            }
            finally
            {
                restarted.Stop().GetAwaiter().GetResult();
            }
        }

        [Theory]
        [InlineData(RuntimeUnit.Seconds, 5400)]
        [InlineData(RuntimeUnit.Minutes, 90)]
        [InlineData(RuntimeUnit.Hours, 1.5)]
        [InlineData(RuntimeUnit.Days, 0.063)]
        public void Units(RuntimeUnit unit, double expected)
        {
            Configure(unit);
            Assert.Equal(expected, Rule.ToUnit(5400));
        }
    }
}
