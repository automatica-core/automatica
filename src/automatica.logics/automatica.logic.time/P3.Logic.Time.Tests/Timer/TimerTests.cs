using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Time.Timer;
using Xunit;

namespace P3.Logic.Time.Tests.Timer
{
    public class TimerTests: LogicTest<TimerLogicFactory>
    {
        [Fact]
        public async void TestTimerRule()
        {
            FakeTimeProvider.SetDateTime(DateTime.Now);
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            var paramDelay = GetLogicInterfaceByTemplate(TimerLogicFactory.RuleTimerParameter);
            paramDelay.Value = new TimerPropertyData()
            {
                StartTime = DateTime.Now.AddSeconds(-1),
                StopTime = DateTime.Now.AddSeconds(3),
                EnabledDays = new List<DayOfWeek>()
                {
                    DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday,
                    DayOfWeek.Saturday, DayOfWeek.Sunday
                }
            };

            FakeTimeProvider.SetDateTime(DateTime.Now);
            await Logic.Start();

            FakeTimeProvider.SetDateTime(DateTime.Now);
            await Task.Delay(200);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.Equal(true, values.First().Value.Value);

            await Task.Delay(5000);

            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.Equal(false, values.First().Value.Value);
            await Logic.Stop();
        }

        [Fact]
        public async void TestTimerRule2()
        {
            FakeTimeProvider.SetDateTime(DateTime.Now);
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            FakeTimeProvider.SetDateTime(DateTime.Now);
            var paramDelay = GetLogicInterfaceByTemplate(TimerLogicFactory.RuleTimerParameter);
            var timerData = new TimerPropertyData()
            {
                StartTime = DateTime.Now.AddSeconds(1),
                StopTime = DateTime.Now.AddSeconds(2),
                EnabledDays = new List<DayOfWeek>()
                {
                    DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday,
                    DayOfWeek.Saturday, DayOfWeek.Sunday
                }
            };

            timerData.EnabledDays.Remove(DateTime.Now.DayOfWeek);

            paramDelay.Value = timerData;

            await Logic.Start();
            await Task.Delay(1500);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            // the start emits the current state (off), a disabled weekday must never switch on
            Assert.All(values, v => Assert.Equal(false, v.Value.Value));
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
            FakeTimeProvider.SetTimeZone(TimeZoneInfo.Local);
            FakeTimeProvider.SetDateTime(new DateTime(2026, 11, 6, 23, 0, 0, DateTimeKind.Local));
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            GetLogicInterfaceByTemplate(TimerLogicFactory.RuleTimerParameter).Value = Window(22, 6, AllDays, DateTimeKind.Local);

            await Logic.Start();

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);
            Assert.Single(values);
            Assert.Equal(true, values.First().Value.Value);
            await Logic.Stop();
        }

        [Fact]
        public async Task TestTimerRule_OvernightSwitchesOffAtStop()
        {
            FakeTimeProvider.SetTimeZone(TimeZoneInfo.Local);
            FakeTimeProvider.SetDateTime(new DateTime(2026, 11, 7, 5, 59, 59, 500, DateTimeKind.Local));
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            GetLogicInterfaceByTemplate(TimerLogicFactory.RuleTimerParameter).Value = Window(22, 6, AllDays, DateTimeKind.Local);

            await Logic.Start();

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);
            Assert.Equal(true, values.First().Value.Value);

            // the tick at 06:00 has to switch off (the fake clock does not move, the tick evaluates its boundary)
            await Task.Delay(1500);

            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);
            Assert.Single(values);
            Assert.Equal(false, values.First().Value.Value);
            await Logic.Stop();
        }

        [Fact]
        public async Task TestTimerRule_ResetRecalculates()
        {
            FakeTimeProvider.SetTimeZone(TimeZoneInfo.Local);
            FakeTimeProvider.SetDateTime(new DateTime(2026, 11, 6, 23, 0, 0, DateTimeKind.Local));
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            GetLogicInterfaceByTemplate(TimerLogicFactory.RuleTimerParameter).Value = Window(22, 6, AllDays, DateTimeKind.Local);
            await Logic.Start();
            await Context.Dispatcher.ClearValues();

            Logic.ValueChanged(GetLogicInterfaceByTemplate(TimerLogicFactory.RuleReset), Dispatchable, true);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);
            Assert.Single(values);
            Assert.Equal(true, values.First().Value.Value);
            await Logic.Stop();
        }




        [Fact]
        public async void TestTimerRule3()
        {
            FakeTimeProvider.SetDateTime(DateTime.Now);
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            var paramDelay = GetLogicInterfaceByTemplate(TimerLogicFactory.RuleTimerParameter);
            var timerData = "{\"StartTime\":\"" + DateTime.Now.AddSeconds(1).ToString(CultureInfo.InvariantCulture) + "\",\"StopTime\":\"" + DateTime.Now.AddSeconds(2).ToString(CultureInfo.InvariantCulture) + "\",\"EnabledDays\":[1,2,3,4,5,6,0],\"TrackingState\":1}";
            
            paramDelay.Value = timerData;

            await Logic.Start();
            await Task.Delay(1500);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);
            
            Assert.Single(values);
            await Logic.Stop();
        }
    }
}

