using System;
using Automatica.Core.EF.Models;
using P3.Logic.Time.AdvancedTimer;
using P3.Logic.Time.Tests.TestSupport;
using Xunit;

namespace P3.Logic.Time.Tests.AdvancedTimer
{
    /// <summary>
    /// AdvancedTimer in Europe/Vienna (CET +1 / CEST +2). The UI sends the wall times the user picked as UTC instants on the day the entry was created.
    /// DST 2026: 2026-03-29 02:00 -> 03:00, 2026-10-25 03:00 -> 02:00.
    /// </summary>
    public class AdvancedTimerViennaTests : AdvancedTimerTestBase
    {
        public AdvancedTimerViennaTests() : base(TimeZones.Vienna)
        {
        }

        private static DateTime V(int year, int month, int day, int hour, int minute = 0, int second = 0) =>
            TimeZones.ViennaToUtc(year, month, day, hour, minute, second);

        // 06:00 - 08:00 picked in winter (created 2023-12-14)
        private static CalendarPropertyDataEntry MorningWinter => Entry(V(2023, 12, 14, 6), V(2023, 12, 14, 8));

        // 06:00 - 08:00 picked in summer (created 2026-07-01)
        private static CalendarPropertyDataEntry MorningSummer => Entry(V(2026, 7, 1, 6), V(2026, 7, 1, 8));

        // 18:00 - 22:00 picked in winter
        private static CalendarPropertyDataEntry EveningWinter => Entry(V(2023, 12, 14, 18), V(2023, 12, 14, 22));

        // 22:00 - 06:00 picked in winter
        private static CalendarPropertyDataEntry NightWinter => Entry(V(2023, 12, 14, 22), V(2023, 12, 15, 6));

        [Fact]
        public void Zone_IsPinned()
        {
            Assert.Same(TimeZones.Vienna, TimeZoneInfo.Local);
            Assert.Equal(TimeSpan.FromHours(1), FakeTimeProvider.LocalTimeZone.GetUtcOffset(Utc(2026, 1, 14, 12)));
            Assert.Equal(TimeSpan.FromHours(2), FakeTimeProvider.LocalTimeZone.GetUtcOffset(Utc(2026, 7, 1, 12)));
        }

        [Theory]
        [InlineData(5, 59, false)]
        [InlineData(6, 0, true)]
        [InlineData(7, 0, true)]
        [InlineData(7, 59, true)]
        [InlineData(8, 0, false)]
        [InlineData(12, 0, false)]
        public void WinterEntry_WinterDay(int hour, int minute, bool expected)
        {
            StartWith(V(2026, 1, 14, hour, minute), MorningWinter);

            AssertState(expected);
        }

        [Theory]
        [InlineData(5, 59, false)]
        [InlineData(6, 0, true)]
        [InlineData(7, 59, true)]
        [InlineData(8, 0, false)]
        public void SummerEntry_SummerDay(int hour, int minute, bool expected)
        {
            StartWith(V(2026, 7, 1, hour, minute), MorningSummer);

            AssertState(expected);
        }

        [Theory]
        [InlineData(17, 59, false)]
        [InlineData(18, 0, true)]
        [InlineData(21, 59, true)]
        [InlineData(22, 0, false)]
        [InlineData(23, 30, false)]
        public void WinterEvening_WinterDay(int hour, int minute, bool expected)
        {
            StartWith(V(2026, 11, 4, hour, minute), EveningWinter);

            AssertState(expected);
        }

        [Fact]
        public void WinterEvening_Active_NextTickIsTheEnd()
        {
            StartWith(V(2026, 11, 4, 19, 30), EveningWinter);

            AssertState(true);
            AssertNextTickIn(TimeSpan.FromHours(2.5));
        }

        [Theory(Skip = Bugs.DstShiftsDailyEntries)]
        [InlineData(2026, 7, 1, 6, 30)]    // summer
        [InlineData(2026, 3, 29, 6, 30)]   // the day DST starts
        [InlineData(2026, 3, 30, 6, 15)]
        public void WinterEntry_SummerDay_KeepsTheWallClockTime(int year, int month, int day, int hour, int minute)
        {
            StartWith(V(year, month, day, hour, minute), MorningWinter);

            AssertState(hour < 8);
        }

        [Theory]
        [InlineData(25, 6, 30, true)]  // the day DST ends: CET again, same offset as when the entry was created
        [InlineData(25, 8, 30, false)]
        [InlineData(26, 7, 0, true)]
        public void WinterEntry_AfterDstEnds(int day, int hour, int minute, bool expected)
        {
            StartWith(V(2026, 10, day, hour, minute), MorningWinter);

            AssertState(expected);
        }

        [Fact]
        public void NightWindow_DuringTheFallBackHour_IsOn_AndTicksAtTheRealEnd()
        {
            // 2026-10-25 00:30Z is 02:30 CEST (the first 02:30), the window ends 06:00 CET = 05:00Z -> 4.5 real hours
            StartWith(Utc(2026, 10, 25, 0, 30), NightWinter);

            AssertState(true);
            AssertNextTickIn(TimeSpan.FromHours(4.5));
        }

        [Fact]
        public void NightWindow_SecondPassOfTheFallBackHour_IsOn()
        {
            // 01:30Z is 02:30 CET (the second 02:30)
            StartWith(Utc(2026, 10, 25, 1, 30), NightWinter);

            AssertState(true);
            AssertNextTickIn(TimeSpan.FromHours(3.5));
        }

        [Theory]
        [InlineData(2026, 10, 24, 23, 30, true)]
        [InlineData(2026, 10, 25, 5, 59, true)]
        [InlineData(2026, 10, 25, 6, 0, false)]
        [InlineData(2026, 10, 25, 12, 0, false)]
        [InlineData(2026, 11, 4, 23, 30, true)]
        [InlineData(2026, 11, 5, 3, 0, true)]
        [InlineData(2026, 11, 5, 6, 30, false)]
        public void NightWindow_TruthTable(int year, int month, int day, int hour, int minute, bool expected)
        {
            StartWith(V(year, month, day, hour, minute), NightWinter);

            AssertState(expected);
        }

        [Fact]
        public void NightWindow_TicksAcrossLocalMidnight()
        {
            StartWith(V(2026, 11, 4, 21, 59, 59), NightWinter);
            AssertState(false);
            AssertNextTickIn(TimeSpan.FromSeconds(1));

            TickAt(V(2026, 11, 4, 22, 0, 1));
            AssertState(true);

            TickAt(V(2026, 11, 5, 0, 0, 30));
            AssertState(true);

            TickAt(V(2026, 11, 5, 6, 0, 1));
            AssertState(false);
        }

        // 00:00 - 00:45 picked in winter: 23:00Z - 23:45Z on the day before
        private static CalendarPropertyDataEntry AfterMidnight => Entry(V(2023, 12, 14, 0), V(2023, 12, 14, 0, 45));

        // 23:30 - 00:30 picked in winter: 22:30Z - 23:30Z
        private static CalendarPropertyDataEntry AroundMidnight => Entry(V(2023, 12, 13, 23, 30), V(2023, 12, 14, 0, 30));

        [Theory]
        [InlineData(7, 23, 30, false)]
        [InlineData(8, 1, 0, false)]
        public void AfterMidnightWindow_Outside_IsOff(int day, int hour, int minute, bool expected)
        {
            StartWith(V(2026, 11, day, hour, minute), AfterMidnight);

            AssertState(expected);
        }

        [Fact(Skip = Bugs.UtcTimeOfDayOnLocalDate)]
        public void AfterMidnightWindow_Inside_IsOn()
        {
            // 2026-11-08 00:30 CET = 2026-11-07 23:30Z
            StartWith(V(2026, 11, 8, 0, 30), AfterMidnight);

            AssertState(true);
        }

        [Fact]
        public void AroundMidnightWindow_BeforeLocalMidnight_IsOn()
        {
            StartWith(V(2026, 11, 7, 23, 45), AroundMidnight);

            AssertState(true);
        }

        [Fact(Skip = Bugs.UtcTimeOfDayOnLocalDate)]
        public void AroundMidnightWindow_AfterLocalMidnight_IsOn()
        {
            StartWith(V(2026, 11, 8, 0, 15), AroundMidnight);

            AssertState(true);
        }

        [Theory]
        [InlineData(0, 0, 30)]
        [InlineData(12, 0, 0)]
        [InlineData(23, 59, 30)]
        public void AllDay_IsOnTheWholeLocalDay(int hour, int minute, int second)
        {
            SetUtcNow(V(2026, 11, 7, hour, minute, second));
            Interface(AdvancedTimerRuleFactory.RuleTimerParameter).Value = new CalendarPropertyData
            {
                Value = [new CalendarPropertyDataEntry { AllDay = true }]
            };
            Start();

            AssertState(true);
        }

        [Fact]
        public void AllDay_OnTheShortDstDay_TicksAtLocalMidnight()
        {
            // 2026-03-29 has 23 hours, 12:00 CEST = 10:00Z, the day ends 2026-03-30 00:00 CEST = 22:00Z
            SetUtcNow(Utc(2026, 3, 29, 10, 0));
            Interface(AdvancedTimerRuleFactory.RuleTimerParameter).Value = new CalendarPropertyData
            {
                Value = [new CalendarPropertyDataEntry { AllDay = true }]
            };
            Start();

            AssertState(true);
            AssertNextTickIn(TimeSpan.FromHours(12) - TimeSpan.FromTicks(1));
        }

        [Fact]
        public void Reset_RecalculatesInLocalTime()
        {
            StartWith(V(2026, 11, 4, 17, 0), EveningWinter);
            AssertState(false);

            SetUtcNow(V(2026, 11, 4, 19, 0));
            Input(AdvancedTimerRuleFactory.RuleReset, true);

            AssertState(true);
        }
    }
}
