using System;
using Automatica.Logic.Metering.Meters;
using Automatica.Logic.Metering.Tests.EnergyMonitor;
using Xunit;

namespace Automatica.Logic.Metering.Tests.Meters
{
    public class MeterRegisterTests
    {
        private static DateTimeOffset At(int day, int hour, int minute = 0, int month = 3) => EnergyDayCounterTests.At(day, hour, minute, month);

        private static MeterRegister Create(DateTimeOffset start)
        {
            var register = new MeterRegister(EnergyDayCounterTests.Vienna);
            register.Advance(start);
            return register;
        }

        [Fact]
        public void Power_IsIntegratedOverTime()
        {
            var r = Create(At(10, 8));
            r.SetPower(2, At(10, 8));

            r.Advance(At(10, 9, 30));

            Assert.Equal(3, r.Today, 6);
            Assert.Equal(3, r.Month, 6);
            Assert.Equal(3, r.Total, 6);
        }

        [Fact]
        public void NegativePower_CountsNothing()
        {
            var r = Create(At(10, 8));
            r.SetPower(-5, At(10, 8));

            r.Advance(At(10, 10));

            Assert.Equal(0, r.Today);
        }

        [Fact]
        public void Midnight_SplitsThePower_AndStartsTheDay()
        {
            var r = Create(At(10, 23));
            r.SetPower(1, At(10, 23));

            r.Advance(At(11, 2));

            Assert.Equal(2, r.Today, 6);   // 00:00 - 02:00
            Assert.Equal(3, r.Month, 6);   // 23:00 - 02:00
            Assert.Equal(new DateTime(2026, 3, 11), r.Day);
        }

        [Fact]
        public void FirstOfTheMonth_StartsTheMonth_TotalKeepsCounting()
        {
            var r = Create(At(31, 22));
            r.SetPower(1, At(31, 22));

            r.Advance(At(1, 1, month: 4));

            Assert.Equal(1, r.Today, 6);
            Assert.Equal(1, r.Month, 6);
            Assert.Equal(3, r.Total, 6);
        }

        [Fact]
        public void SeveralDaysWithoutUpdate_EachDayGetsItsShare()
        {
            var r = Create(At(10, 12));
            r.SetPower(1, At(10, 12));

            r.Advance(At(13, 6));

            Assert.Equal(6, r.Today, 6);
            Assert.Equal(12 + 24 + 24 + 6, r.Total, 6);
        }

        [Fact]
        public void DstSpringForward_DayHas23Hours()
        {
            // 2026-03-29 has 23 hours in Vienna
            var r = Create(At(29, 0));
            r.SetPower(1, At(29, 0));

            r.Advance(At(30, 0));

            Assert.Equal(0, r.Today, 6);
            Assert.Equal(23, r.Total, 6);
        }

        [Fact]
        public void FirstReading_IsTheBaseline_AndTheTotalStartsThere()
        {
            var r = Create(At(10, 8));

            Assert.Null(r.SetReading(1500, At(10, 8)));
            Assert.Equal(0, r.Today);
            Assert.Equal(1500, r.Total);

            Assert.Equal(2.5, r.SetReading(1502.5, At(10, 9)));
            Assert.Equal(2.5, r.Today, 6);
            Assert.Equal(1502.5, r.Total, 6);
        }

        [Fact]
        public void ReadingGoingBack_IsANewMeter_NothingLost()
        {
            var r = Create(At(10, 8));
            r.SetReading(1500, At(10, 8));
            r.SetReading(1504, At(10, 9));

            Assert.Null(r.SetReading(3, At(10, 10)));
            r.SetReading(5, At(10, 11));

            Assert.Equal(6, r.Today, 6);
            Assert.Equal(1506, r.Total, 6);
        }

        [Fact]
        public void ReadingMode_IgnoresPowerForCounting()
        {
            var r = Create(At(10, 8));
            r.SetReading(100, At(10, 8));
            r.SetPower(5, At(10, 8));

            r.Advance(At(10, 10));

            Assert.Equal(0, r.Today);
            Assert.True(r.ReadingMode);
        }

        [Fact]
        public void ReadingsAcrossMidnight_DeltaGoesToTheNewDay()
        {
            var r = Create(At(10, 22));
            r.SetReading(100, At(10, 22));
            r.SetReading(101, At(10, 23));

            r.SetReading(104, At(11, 1));

            Assert.Equal(3, r.Today, 6);
            Assert.Equal(4, r.Month, 6);
        }

        [Fact]
        public void SaveAndLoad_KeepsTotals_AndTheNextReadingCountsFromTheLastOne()
        {
            var r = Create(At(10, 8));
            r.SetReading(100, At(10, 8));
            r.SetReading(110, At(10, 12));
            var state = r.Save();

            var loaded = new MeterRegister(EnergyDayCounterTests.Vienna);
            loaded.Load(state, At(10, 18));
            loaded.SetReading(115, At(10, 18));

            Assert.Equal(15, loaded.Today, 6);
            Assert.Equal(115, loaded.Total, 6);
        }

        [Fact]
        public void Load_OnALaterDay_StartsTheDay_KeepsMonthAndTotal()
        {
            var r = Create(At(10, 8));
            r.SetPower(1, At(10, 8));
            r.Advance(At(10, 12));
            var state = r.Save();

            var loaded = new MeterRegister(EnergyDayCounterTests.Vienna);
            loaded.Load(state, At(12, 9));

            Assert.Equal(0, loaded.Today);
            Assert.Equal(4, loaded.Month, 6);
            Assert.Equal(4, loaded.Total, 6);
            Assert.Equal(new DateTime(2026, 3, 12), loaded.Day);
        }

        [Fact]
        public void Load_TheDowntimeIsNotCounted()
        {
            var r = Create(At(10, 8));
            r.SetPower(3, At(10, 8));
            var state = r.Save();

            var loaded = new MeterRegister(EnergyDayCounterTests.Vienna);
            loaded.Load(state, At(10, 10));
            loaded.Advance(At(10, 11));

            Assert.Equal(0, loaded.Today);
        }

        [Fact]
        public void Load_BrokenValues_StartAtZero()
        {
            var loaded = new MeterRegister(EnergyDayCounterTests.Vienna);
            loaded.Load(new MeterRegisterState { Today = double.NaN, Month = -3, Total = double.PositiveInfinity, Day = "garbage" }, At(10, 10));

            Assert.Equal(0, loaded.Today);
            Assert.Equal(0, loaded.Month);
            Assert.Equal(0, loaded.Total);
            Assert.Equal(new DateTime(2026, 3, 10), loaded.Day);
        }

        [Fact]
        public void ClockGoingBack_BooksNothing()
        {
            var r = Create(At(10, 12));
            r.SetPower(1, At(10, 12));

            r.Advance(At(10, 11));
            r.Advance(At(10, 12));

            Assert.Equal(1, r.Today, 6);
        }
    }

    public class RateEstimatorTests
    {
        private static readonly DateTimeOffset T0 = new(2026, 3, 10, 8, 0, 0, TimeSpan.Zero);

        [Fact]
        public void NeedsTwoSteps()
        {
            var rate = new RateEstimator();
            Assert.Null(rate.Current(T0));

            rate.Add(0.001, T0);
            Assert.Null(rate.Current(T0));

            rate.Add(0.001, T0.AddSeconds(3.6));
            Assert.Equal(1, rate.Current(T0.AddSeconds(3.6))!.Value, 6);
        }

        [Fact]
        public void BaselineReading_StartsTheClock()
        {
            var rate = new RateEstimator();
            rate.Add(null, T0);
            rate.Add(0.5, T0.AddMinutes(15));

            Assert.Equal(2, rate.Current(T0.AddMinutes(15))!.Value, 6);
        }

        [Fact]
        public void WithoutNewSteps_TheRateFalls_AndIsZeroAfterTheSilence()
        {
            var rate = new RateEstimator();
            rate.Add(0.001, T0);
            rate.Add(0.001, T0.AddSeconds(3.6)); // 1 kW

            Assert.Equal(1, rate.Current(T0.AddSeconds(5))!.Value, 6);
            Assert.Equal(0.1, rate.Current(T0.AddSeconds(3.6 + 36))!.Value, 6);
            Assert.Equal(0, rate.Current(T0.AddHours(2)));
        }

        [Fact]
        public void OneStepOnly_IsZeroAfterTheSilence()
        {
            var rate = new RateEstimator();
            rate.Add(null, T0);

            Assert.Null(rate.Current(T0.AddMinutes(10)));
            Assert.Equal(0, rate.Current(T0.AddHours(1)));
        }

        [Fact]
        public void ZeroStep_GivesZero()
        {
            var rate = new RateEstimator();
            rate.Add(null, T0);
            rate.Add(0, T0.AddMinutes(15));

            Assert.Equal(0, rate.Current(T0.AddMinutes(16)));
        }
    }

    public class PulseCounterTests
    {
        [Fact]
        public void Bool_CountsRisingEdges()
        {
            var p = new PulseCounter();

            Assert.Equal(1, p.Pulses(true));
            Assert.Equal(0, p.Pulses(true));
            Assert.Equal(0, p.Pulses(false));
            Assert.Equal(1, p.Pulses(true));
        }

        [Fact]
        public void Number_IsACounter()
        {
            var p = new PulseCounter();

            Assert.Equal(0, p.Pulses(1000d));
            Assert.Equal(5, p.Pulses(1005d));
            Assert.Equal(0, p.Pulses(3d));     // counter reset: new start
            Assert.Equal(2, p.Pulses("5"));
            Assert.Equal(0, p.Pulses(null));
            Assert.Equal(0, p.Pulses("garbage"));
        }

        [Fact]
        public void Counter_SurvivesSaveAndLoad()
        {
            var p = new PulseCounter();
            p.Pulses(1000d);
            var values = new System.Collections.Generic.Dictionary<string, double>();
            p.Save(values);

            var loaded = new PulseCounter();
            loaded.Load(values);

            Assert.Equal(7, loaded.Pulses(1007d));
        }
    }
}
