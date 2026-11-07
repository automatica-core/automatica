using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Time.Tests.TestSupport;
using P3.Logic.Time.Timer;
using Xunit;

namespace P3.Logic.Time.Tests.Timer
{
    /// <summary>
    /// These tests used to take "now" from <see cref="DateTime.Now"/> and waited for the real timer (up to 5 s).
    /// They now run at fixed instants in a pinned Europe/Vienna zone and fire the timer tick themselves.
    /// </summary>
    public class TimerTests : LogicTest<TimerLogicFactory>, IDisposable
    {
        private readonly TimeZoneScope _zone = TimeZoneScope.Vienna();

        public void Dispose()
        {
            Logic.Stop().GetAwaiter().GetResult();
            _zone.Dispose();
        }

        private static DateTime Local(string wallTime)
        {
            return DateTime.SpecifyKind(DateTime.ParseExact(wallTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), DateTimeKind.Local);
        }

        private async Task Restart(DateTime now)
        {
            FakeTimeProvider.SetDateTime(now);
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();
        }

        private object Current()
        {
            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);
            Assert.Single(values);
            return values.First().Value.Value;
        }

        private System.Timers.Timer LogicTimer =>
            (System.Timers.Timer)Logic.GetType().GetField("_timer", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Logic);

        private void FireTick()
        {
            var method = Logic.GetType().GetMethod("_timer_Elapsed", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            method.Invoke(Logic, new object[] { null, null });
        }

        /// <summary>
        /// Wall times in Europe/Vienna that replace DateTime.Now (at least 3 seconds before midnight, see TestTimerRule_WindowAcrossMidnight)
        /// </summary>
        public static IEnumerable<object[]> Nows => new[]
        {
            "2026-11-04 10:00:00", "2026-11-04 23:05:00", "2026-11-04 23:59:56", "2026-11-05 00:00:00", "2026-11-05 00:00:01",
            "2026-11-30 23:30:00", "2026-03-29 03:00:00", "2026-10-25 03:30:00", "2026-12-31 23:59:00"
        }.Select(a => new object[] { a });

        [Theory]
        [MemberData(nameof(Nows))]
        public async Task TestTimerRule(string wallTime)
        {
            var now = Local(wallTime);
            await Restart(now);

            GetLogicInterfaceByTemplate(TimerLogicFactory.RuleTimerParameter).Value = new TimerPropertyData
            {
                StartTime = now.AddSeconds(-1),
                StopTime = now.AddSeconds(3),
                EnabledDays = AllDays.ToList()
            };

            await Logic.Start();

            Assert.Equal(true, Current());
            Assert.InRange(LogicTimer.Interval, 2999, 3001);

            // the tick at the stop time switches off (it used to wait 5 s for the real timer)
            FakeTimeProvider.SetDateTime(now.AddSeconds(3));
            FireTick();

            Assert.Equal(false, Current());
            await Logic.Stop();
        }

        [Fact]
        public async Task TestTimerRule_WindowAcrossMidnight()
        {
            // 23:59:58 - 00:00:02 crosses midnight, it is on and switches off after midnight
            var now = Local("2026-11-04 23:59:59");
            await Restart(now);

            GetLogicInterfaceByTemplate(TimerLogicFactory.RuleTimerParameter).Value = new TimerPropertyData
            {
                StartTime = now.AddSeconds(-1),
                StopTime = now.AddSeconds(3),
                EnabledDays = AllDays.ToList()
            };
            await Logic.Start();
            Assert.Equal(true, Current());

            FakeTimeProvider.SetDateTime(now.AddSeconds(3));
            FireTick();
            Assert.Equal(false, Current());
            await Logic.Stop();
        }

        public static IEnumerable<object[]> NowsNotNearMidnight => new[]
        {
            "2026-11-04 10:00:00", "2026-11-04 23:05:00", "2026-11-05 00:00:00", "2026-11-30 23:30:00", "2026-03-29 03:00:00", "2026-10-25 03:30:00"
        }.Select(a => new object[] { a });

        [Theory]
        [MemberData(nameof(NowsNotNearMidnight))]
        public async Task TestTimerRule2(string wallTime)
        {
            var now = Local(wallTime);
            await Restart(now);

            var timerData = new TimerPropertyData
            {
                StartTime = now.AddSeconds(1),
                StopTime = now.AddSeconds(2),
                EnabledDays = AllDays.ToList()
            };
            timerData.EnabledDays.Remove(now.DayOfWeek);
            GetLogicInterfaceByTemplate(TimerLogicFactory.RuleTimerParameter).Value = timerData;

            await Logic.Start();

            // the start emits the current state (off), a disabled weekday must never switch on
            Assert.Equal(false, Current());

            // the next change is tomorrow's start, not today's
            var expected = TimeSpan.FromDays(1) + TimeSpan.FromSeconds(1);
            Assert.InRange(LogicTimer.Interval, expected.TotalMilliseconds - 1, expected.TotalMilliseconds + 1);
            await Logic.Stop();
        }

        private static readonly List<DayOfWeek> AllDays = new()
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday,
            DayOfWeek.Saturday, DayOfWeek.Sunday
        };

        private static TimerPropertyData Window(int startHour, int stopHour, IList<DayOfWeek> days, DateTimeKind kind = DateTimeKind.Utc)
        {
            return new TimerPropertyData
            {
                StartTime = new DateTime(2020, 1, 1, startHour, 0, 0, kind),
                StopTime = new DateTime(2020, 1, 1, stopHour, 0, 0, kind),
                EnabledDays = days
            };
        }

        // 2026-11-06 is a friday
        [Theory]
        [InlineData(23, 0, true, 7, 6)]   // inside, before midnight -> off tomorrow 06:00
        [InlineData(5, 0, true, 6, 6)]    // inside, after midnight -> off today 06:00
        [InlineData(6, 0, false, 6, 22)]  // stop is exclusive -> on today 22:00
        [InlineData(7, 0, false, 6, 22)]
        [InlineData(21, 59, false, 6, 22)]
        [InlineData(22, 0, true, 7, 6)]   // start is inclusive
        public void TestCalculateState_Overnight(int hour, int minute, bool expected, int nextDay, int nextHour)
        {
            var now = new DateTime(2026, 11, 6, hour, minute, 0);
            var (active, next) = TimerRule.CalculateState(Window(22, 6, AllDays), now, TimeZoneInfo.Utc);

            Assert.Equal(expected, active);
            Assert.Equal(new DateTime(2026, 11, nextDay, nextHour, 0, 0), next);
        }

        [Fact]
        public void TestCalculateState_DisabledWeekdays()
        {
            var workDays = new List<DayOfWeek>
                { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
            var data = Window(8, 17, workDays);

            // friday 17:00 -> off, next on is monday 08:00 (not saturday)
            var (active, next) = TimerRule.CalculateState(data, new DateTime(2026, 11, 6, 17, 0, 0), TimeZoneInfo.Utc);
            Assert.False(active);
            Assert.Equal(new DateTime(2026, 11, 9, 8, 0, 0), next);

            // saturday 09:00 -> off
            (active, next) = TimerRule.CalculateState(data, new DateTime(2026, 11, 7, 9, 0, 0), TimeZoneInfo.Utc);
            Assert.False(active);
            Assert.Equal(new DateTime(2026, 11, 9, 8, 0, 0), next);

            // shortly before midnight on an enabled day -> exactly one change: on tomorrow 08:00
            (active, next) = TimerRule.CalculateState(data, new DateTime(2026, 11, 5, 23, 59, 59, 500), TimeZoneInfo.Utc);
            Assert.False(active);
            Assert.Equal(new DateTime(2026, 11, 6, 8, 0, 0), next);
        }

        [Fact]
        public void TestCalculateState_OvernightUsesStartDay()
        {
            // only friday enabled: friday 22:00 - saturday 06:00 is on, saturday 22:00 is not
            var data = Window(22, 6, new List<DayOfWeek> { DayOfWeek.Friday });

            var (active, next) = TimerRule.CalculateState(data, new DateTime(2026, 11, 7, 1, 0, 0), TimeZoneInfo.Utc);
            Assert.True(active);
            Assert.Equal(new DateTime(2026, 11, 7, 6, 0, 0), next);

            (active, next) = TimerRule.CalculateState(data, new DateTime(2026, 11, 7, 23, 0, 0), TimeZoneInfo.Utc);
            Assert.False(active);
            Assert.Equal(new DateTime(2026, 11, 13, 22, 0, 0), next);

            // thursday 23:00 - thursday is disabled
            (active, _) = TimerRule.CalculateState(data, new DateTime(2026, 11, 5, 23, 0, 0), TimeZoneInfo.Utc);
            Assert.False(active);
        }

        [Fact]
        public void TestCalculateState_EmptyWindowAndNoDays()
        {
            var (active, next) = TimerRule.CalculateState(Window(8, 8, AllDays), new DateTime(2026, 11, 6, 8, 0, 0), TimeZoneInfo.Utc);
            Assert.False(active);
            Assert.Null(next);

            (active, next) = TimerRule.CalculateState(Window(8, 17, new List<DayOfWeek>()), new DateTime(2026, 11, 6, 9, 0, 0), TimeZoneInfo.Utc);
            Assert.False(active);
            Assert.Null(next);
        }

        [Fact]
        public async Task TestTimerRule_OvernightOnAtStart()
        {
            FakeTimeProvider.SetDateTime(new DateTime(2026, 11, 6, 23, 0, 0, DateTimeKind.Local));
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            GetLogicInterfaceByTemplate(TimerLogicFactory.RuleTimerParameter).Value = Window(22, 6, AllDays, DateTimeKind.Local);

            await Logic.Start();

            Assert.Equal(true, Current());
            await Logic.Stop();
        }

        [Fact]
        public async Task TestTimerRule_OvernightSwitchesOffAtStop()
        {
            FakeTimeProvider.SetDateTime(new DateTime(2026, 11, 7, 5, 59, 59, 500, DateTimeKind.Local));
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            GetLogicInterfaceByTemplate(TimerLogicFactory.RuleTimerParameter).Value = Window(22, 6, AllDays, DateTimeKind.Local);

            await Logic.Start();
            Assert.Equal(true, Current());
            Assert.InRange(LogicTimer.Interval, 499, 501);

            // the tick at 06:00 has to switch off (the fake clock does not move, the tick evaluates its boundary)
            FireTick();

            Assert.Equal(false, Current());
            await Logic.Stop();
        }

        [Fact]
        public async Task TestTimerRule_ResetRecalculates()
        {
            FakeTimeProvider.SetDateTime(new DateTime(2026, 11, 6, 23, 0, 0, DateTimeKind.Local));
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            GetLogicInterfaceByTemplate(TimerLogicFactory.RuleTimerParameter).Value = Window(22, 6, AllDays, DateTimeKind.Local);
            await Logic.Start();
            await Context.Dispatcher.ClearValues();

            Logic.ValueChanged(GetLogicInterfaceByTemplate(TimerLogicFactory.RuleReset), Dispatchable, true);

            Assert.Equal(true, Current());
            await Logic.Stop();
        }

        [Fact]
        public async Task TestTimerRule3()
        {
            // the parameter as a JSON string (the format the configuration stores)
            var now = Local("2026-11-04 10:00:00");
            await Restart(now);

            var timerData = "{\"StartTime\":\"" + now.AddSeconds(1).ToString(CultureInfo.InvariantCulture) + "\",\"StopTime\":\"" +
                            now.AddSeconds(2).ToString(CultureInfo.InvariantCulture) + "\",\"EnabledDays\":[1,2,3,4,5,6,0],\"TrackingState\":1}";
            GetLogicInterfaceByTemplate(TimerLogicFactory.RuleTimerParameter).Value = timerData;

            await Logic.Start();

            Assert.Equal(false, Current());
            await Logic.Stop();
        }
    }
}
