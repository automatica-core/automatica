using System;
using Automatica.Logic.Metering.EnergyMonitor;
using Xunit;

namespace Automatica.Logic.Metering.Tests.EnergyMonitor
{
    public class EnergyDayCounterTests
    {
        private const double Precision = 9;

        internal static readonly TimeZoneInfo Vienna = TimeZoneInfo.FindSystemTimeZoneById("Europe/Vienna");

        /// <summary>local time in Vienna</summary>
        internal static DateTimeOffset At(int day, int hour, int minute = 0, int month = 1)
        {
            var local = new DateTime(2026, month, day, hour, minute, 0, DateTimeKind.Unspecified);
            return new DateTimeOffset(local, Vienna.GetUtcOffset(local));
        }

        private static EnergyFlowSnapshot Flow(double pv = 0, double grid = 0, double battery = 0)
        {
            return EnergyFlowCalculator.Calculate(new EnergyFlowInputs { Pv = pv, Grid = grid, Battery = battery });
        }

        private static EnergyDayCounter Started(DateTimeOffset at, EnergyFlowSnapshot flow)
        {
            var counter = new EnergyDayCounter(Vienna);
            counter.Advance(at);
            counter.SetPower(flow);
            return counter;
        }

        [Fact]
        public void BeforeTheFirstUpdate_EverythingIsZero()
        {
            var counter = new EnergyDayCounter(Vienna);

            Assert.Null(counter.Day);
            Assert.Equal(0, counter.Today(EnergyFlowKind.Pv));
            Assert.Equal(0, counter.ConsumptionToday);
            Assert.Equal(0, counter.SelfConsumptionToday);
            Assert.Equal(0, counter.AutarkyToday);
        }

        [Fact]
        public void HeldPower_IsIntegratedOverTime()
        {
            var counter = Started(At(5, 10), Flow(pv: 4, grid: -1));

            counter.Advance(At(5, 10, 30));

            Assert.Equal(2, counter.Today(EnergyFlowKind.Pv), Precision);
            Assert.Equal(0.5, counter.Today(EnergyFlowKind.GridExport), Precision);
            Assert.Equal(0, counter.Today(EnergyFlowKind.GridImport));
            Assert.Equal(1.5, counter.ConsumptionToday, Precision);
            Assert.Equal(75, counter.SelfConsumptionToday, Precision);
            Assert.Equal(100, counter.AutarkyToday, Precision);
        }

        [Fact]
        public void NewPower_AppliesOnlyFromItsUpdateOn()
        {
            var counter = Started(At(5, 8), Flow(grid: 2));

            counter.Advance(At(5, 9));
            counter.SetPower(Flow(grid: 0.5));
            counter.Advance(At(5, 11));

            Assert.Equal(3, counter.Today(EnergyFlowKind.GridImport), Precision);
        }

        [Fact]
        public void Battery_ChargeAndDischarge_AreCountedApart()
        {
            var counter = Started(At(5, 12), Flow(pv: 5, battery: 3));
            counter.Advance(At(5, 13));
            counter.SetPower(Flow(battery: -1));
            counter.Advance(At(5, 15));

            Assert.Equal(3, counter.Today(EnergyFlowKind.BatteryCharge), Precision);
            Assert.Equal(2, counter.Today(EnergyFlowKind.BatteryDischarge), Precision);
            // 5 kWh PV - 3 charged + 2 discharged
            Assert.Equal(4, counter.ConsumptionToday, Precision);
            Assert.Equal(100, counter.AutarkyToday, Precision);
        }

        [Fact]
        public void Midnight_StartsANewDay_AndBooksOnlyThePartAfterMidnight()
        {
            var counter = Started(At(5, 23), Flow(grid: 1));
            counter.Advance(At(5, 23, 30));
            Assert.Equal(0.5, counter.Today(EnergyFlowKind.GridImport), Precision);

            counter.Advance(At(6, 0, 15));

            Assert.Equal(new DateTime(2026, 1, 6), counter.Day);
            Assert.Equal(0.25, counter.Today(EnergyFlowKind.GridImport), Precision);
        }

        [Fact]
        public void Midnight_IsLocalTime_NotUtc()
        {
            // 23:30 UTC is already 00:30 in Vienna (winter, UTC+1)
            var counter = Started(At(5, 22), Flow(grid: 2));

            counter.Advance(new DateTimeOffset(2026, 1, 5, 23, 30, 0, TimeSpan.Zero));

            Assert.Equal(new DateTime(2026, 1, 6), counter.Day);
            Assert.Equal(1, counter.Today(EnergyFlowKind.GridImport), Precision);
        }

        [Fact]
        public void Midnight_InSummerTime_UsesTheSummerOffset()
        {
            var counter = Started(At(10, 23, 0, month: 7), Flow(pv: 0, grid: 1));

            counter.Advance(At(11, 1, 0, month: 7));

            Assert.Equal(new DateTime(2026, 7, 11), counter.Day);
            Assert.Equal(1, counter.Today(EnergyFlowKind.GridImport), Precision);
        }

        [Fact]
        public void SeveralDaysWithoutUpdate_CountOnlyToday()
        {
            var counter = Started(At(5, 12), Flow(grid: 1));

            counter.Advance(At(8, 6));

            Assert.Equal(new DateTime(2026, 1, 8), counter.Day);
            Assert.Equal(6, counter.Today(EnergyFlowKind.GridImport), Precision);
        }

        [Fact]
        public void ClockGoingBack_BooksNothing_AndContinuesFromTheNewTime()
        {
            var counter = Started(At(5, 12), Flow(grid: 1));
            counter.Advance(At(5, 13));

            counter.Advance(At(5, 12, 30));
            Assert.Equal(1, counter.Today(EnergyFlowKind.GridImport), Precision);

            counter.Advance(At(5, 13));
            Assert.Equal(1.5, counter.Today(EnergyFlowKind.GridImport), Precision);
        }

        [Fact]
        public void Meter_CountsFromTheFirstReading_InsteadOfThePower()
        {
            var counter = Started(At(5, 8), Flow(pv: 10));

            counter.SetMeter(EnergyFlowKind.Pv, 1000, At(5, 9));
            counter.SetMeter(EnergyFlowKind.Pv, 1003.5, At(5, 12));

            Assert.True(counter.HasMeter(EnergyFlowKind.Pv));
            Assert.Equal(3.5, counter.Today(EnergyFlowKind.Pv), Precision);
            // the other flows still integrate their power
            Assert.False(counter.HasMeter(EnergyFlowKind.GridImport));
        }

        [Fact]
        public void Meter_AtMidnight_TheLastReadingOfYesterdayIsTheBaseline()
        {
            var counter = Started(At(5, 20), Flow());
            counter.SetMeter(EnergyFlowKind.GridImport, 500, At(5, 20));
            counter.SetMeter(EnergyFlowKind.GridImport, 504, At(5, 23, 50));
            Assert.Equal(4, counter.Today(EnergyFlowKind.GridImport), Precision);

            counter.Advance(At(6, 0, 1));
            Assert.Equal(0, counter.Today(EnergyFlowKind.GridImport));

            counter.SetMeter(EnergyFlowKind.GridImport, 505.2, At(6, 7));
            Assert.Equal(1.2, counter.Today(EnergyFlowKind.GridImport), Precision);
        }

        [Fact]
        public void Meter_ReadingAfterMidnight_RollsTheDayFirst()
        {
            var counter = Started(At(5, 20), Flow());
            counter.SetMeter(EnergyFlowKind.GridExport, 10, At(5, 22));

            // first value of the new day arrives without a tick in between
            counter.SetMeter(EnergyFlowKind.GridExport, 12, At(6, 9));

            Assert.Equal(new DateTime(2026, 1, 6), counter.Day);
            Assert.Equal(2, counter.Today(EnergyFlowKind.GridExport), Precision);
        }

        [Fact]
        public void Meter_GoingBackwards_KeepsWhatWasCountedToday()
        {
            var counter = Started(At(5, 6), Flow());
            counter.SetMeter(EnergyFlowKind.BatteryCharge, 200, At(5, 7));
            counter.SetMeter(EnergyFlowKind.BatteryCharge, 203, At(5, 10));

            // meter replaced: starts at 0 again
            counter.SetMeter(EnergyFlowKind.BatteryCharge, 0, At(5, 11));
            Assert.Equal(3, counter.Today(EnergyFlowKind.BatteryCharge), Precision);

            counter.SetMeter(EnergyFlowKind.BatteryCharge, 1, At(5, 12));
            Assert.Equal(4, counter.Today(EnergyFlowKind.BatteryCharge), Precision);
        }

        [Fact]
        public void Meter_IgnoresNonFiniteReadings()
        {
            var counter = Started(At(5, 6), Flow());
            counter.SetMeter(EnergyFlowKind.Pv, double.NaN, At(5, 7));

            Assert.False(counter.HasMeter(EnergyFlowKind.Pv));
        }

        [Fact]
        public void RatesToday_WithoutProduction()
        {
            var counter = Started(At(5, 1), Flow(grid: 0.4));
            counter.Advance(At(5, 6));

            Assert.Equal(0, counter.Today(EnergyFlowKind.Pv));
            Assert.Equal(0, counter.SelfConsumptionToday);
            Assert.Equal(0, counter.AutarkyToday);
            Assert.Equal(2, counter.ConsumptionToday, Precision);
        }
    }
}
