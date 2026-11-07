using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.EF.Models;
using P3.Logic.Time.Tests.TestSupport;
using P3.Logic.Time.Timer;
using Xunit;

namespace P3.Logic.Time.Tests.Timer
{
    /// <summary>
    /// Timer logic in Europe/Vienna with a fixed clock. The UI sends start/stop as UTC instants of the picked wall time.
    /// 2026-11-04 is a wednesday. DST 2026: 03-29 02:00 -> 03:00, 10-25 03:00 -> 02:00.
    /// </summary>
    public class TimerBehaviourTests : TimeLogicTest<TimerLogicFactory>
    {
        private static readonly Guid Output = TimerLogicFactory.RuleOutput;

        public TimerBehaviourTests() : base(TimeZones.Vienna)
        {
        }

        private static readonly DayOfWeek[] AllDays = Enum.GetValues<DayOfWeek>();

        private static readonly DayOfWeek[] WorkDays =
            { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

        private static DateTime V(int year, int month, int day, int hour, int minute = 0, int second = 0) =>
            TimeZones.ViennaToUtc(year, month, day, hour, minute, second);

        /// <summary>
        /// A window picked in winter (the stored date carries the winter offset)
        /// </summary>
        private static TimerPropertyData Window(int startHour, int startMinute, int stopHour, int stopMinute, params DayOfWeek[] days)
        {
            return new TimerPropertyData
            {
                StartTime = V(2020, 1, 1, startHour, startMinute),
                StopTime = V(2020, 1, 1, stopHour, stopMinute),
                EnabledDays = (days.Length == 0 ? AllDays : days).ToList()
            };
        }

        private void StartWith(DateTime utcNow, TimerPropertyData data)
        {
            SetUtcNow(utcNow);
            Interface(TimerLogicFactory.RuleTimerParameter).Value = data;
            Start();
        }

        private bool? State => (bool?)Last(Output);

        private double IntervalMs => SutTimer.Interval;

        [Theory]
        [InlineData(2026, 1, 14, 6, 30, true)]   // winter
        [InlineData(2026, 7, 1, 6, 30, true)]    // summer: the wall clock time is kept
        [InlineData(2026, 7, 1, 8, 30, false)]
        [InlineData(2026, 3, 29, 6, 30, true)]   // the day DST starts
        [InlineData(2026, 10, 25, 6, 30, true)]  // the day DST ends
        [InlineData(2026, 10, 25, 5, 30, false)]
        public void MorningWindow_KeepsWallClockAcrossDst(int year, int month, int day, int hour, int minute, bool expected)
        {
            StartWith(V(year, month, day, hour, minute), Window(6, 0, 8, 0));

            Assert.Equal(expected, State);
        }

        [Theory]
        [InlineData(4, 21, 59, false)]
        [InlineData(4, 22, 0, true)]
        [InlineData(4, 23, 59, true)]
        [InlineData(5, 0, 0, true)]
        [InlineData(5, 5, 59, true)]
        [InlineData(5, 6, 0, false)]
        public void NightWindow_AcrossLocalMidnight(int day, int hour, int minute, bool expected)
        {
            StartWith(V(2026, 11, day, hour, minute), Window(22, 0, 6, 0));

            Assert.Equal(expected, State);
        }

        [Fact]
        public void NightWindow_ActiveBeforeMidnight_NextTickIsTomorrowsStop()
        {
            StartWith(V(2026, 11, 4, 23, 30), Window(22, 0, 6, 0));

            Assert.Equal(true, State);
            Assert.InRange(IntervalMs, TimeSpan.FromHours(6.5).TotalMilliseconds - 1, TimeSpan.FromHours(6.5).TotalMilliseconds + 1);
        }

        [Fact]
        public void NightWindow_TicksOnAndOff()
        {
            StartWith(V(2026, 11, 4, 21, 0), Window(22, 0, 6, 0));
            Assert.Equal(false, State);

            // a tick that arrives a little early still belongs to its boundary
            SetUtcNow(V(2026, 11, 4, 21, 59, 59));
            FireTick();
            Assert.Equal(true, State);

            SetUtcNow(V(2026, 11, 5, 6, 0, 0));
            FireTick();
            Assert.Equal(false, State);

            Assert.Equal(new object[] { false, true, false }, Values(Output));
        }

        [Theory]
        [InlineData(4, true)]   // wednesday
        [InlineData(6, true)]   // friday
        [InlineData(7, false)]  // saturday
        [InlineData(8, false)]  // sunday
        [InlineData(9, true)]   // monday
        public void WeekdayMask(int day, bool expected)
        {
            StartWith(V(2026, 11, day, 9, 0), Window(8, 0, 17, 0, WorkDays));

            Assert.Equal(expected, State);
        }

        [Fact]
        public void WeekdayMask_FridayEvening_NextTickIsMonday()
        {
            StartWith(V(2026, 11, 6, 18, 0), Window(8, 0, 17, 0, WorkDays));

            Assert.Equal(false, State);
            Assert.InRange(IntervalMs, TimeSpan.FromHours(62).TotalMilliseconds - 1, TimeSpan.FromHours(62).TotalMilliseconds + 1);
        }

        [Fact]
        public void WeekdayMask_OvernightOnlyOnItsStartDay()
        {
            // friday night 22:00 - saturday 06:00 is on (friday enabled), saturday night is off
            var data = Window(22, 0, 6, 0, DayOfWeek.Friday);

            StartWith(V(2026, 11, 7, 3, 0), data);
            Assert.Equal(true, State);

            SetUtcNow(V(2026, 11, 7, 23, 0));
            Input(TimerLogicFactory.RuleReset, true);
            Assert.Equal(false, State);
        }

        [Fact]
        public void NoEnabledDays_IsOffForever()
        {
            StartWith(V(2026, 11, 4, 9, 0), new TimerPropertyData
            {
                StartTime = V(2020, 1, 1, 8), StopTime = V(2020, 1, 1, 17), EnabledDays = new List<DayOfWeek>()
            });

            Assert.Equal(false, State);
            Assert.False(SutTimer.Enabled);
        }

        [Fact]
        public void EnabledDaysNull_FailsTheStart()
        {
            SetUtcNow(V(2026, 11, 4, 9, 0));
            Interface(TimerLogicFactory.RuleTimerParameter).Value = new TimerPropertyData
            {
                StartTime = V(2020, 1, 1, 8), StopTime = V(2020, 1, 1, 17), EnabledDays = null
            };
            Start();

            Assert.Empty(Values(Output));
            Input(TimerLogicFactory.RuleReset, true);
            Assert.Empty(Values(Output));
        }

        [Fact]
        public void NoParameterValue_FailsTheStart()
        {
            SetUtcNow(V(2026, 11, 4, 9, 0));
            Interface(TimerLogicFactory.RuleTimerParameter).Value = null;
            Start();

            Assert.Empty(Values(Output));
        }

        [Fact]
        public void SameStartAndStop_IsAnEmptyWindow()
        {
            StartWith(V(2026, 11, 4, 8, 0), Window(8, 0, 8, 0));

            Assert.Equal(false, State);
            Assert.False(SutTimer.Enabled);
        }

        public static IEnumerable<object[]> Truthy => new[]
        {
            new object[] { true }, new object[] { 1 }, new object[] { 3.5d }, new object[] { "true" }, new object[] { "1" }
        };

        public static IEnumerable<object[]> Falsy => new[]
        {
            new object[] { false }, new object[] { 0 }, new object[] { "false" }, new object[] { "0" }, new object[] { "x" }, new object[] { null }
        };

        [Theory]
        [MemberData(nameof(Truthy))]
        public void Reset_Truthy_RecalculatesForNow(object value)
        {
            StartWith(V(2026, 11, 4, 7, 0), Window(8, 0, 17, 0));
            Assert.Equal(false, State);

            SetUtcNow(V(2026, 11, 4, 9, 0));
            Assert.Empty(Input(TimerLogicFactory.RuleReset, value));

            Assert.Equal(true, State);
        }

        [Theory]
        [MemberData(nameof(Falsy))]
        public void Reset_Falsy_DoesNothing(object value)
        {
            StartWith(V(2026, 11, 4, 7, 0), Window(8, 0, 17, 0));
            ClearRecorded();

            SetUtcNow(V(2026, 11, 4, 9, 0));
            Assert.Empty(Input(TimerLogicFactory.RuleReset, value));

            Assert.Empty(Values(Output));
        }

        [Fact]
        public void Reset_AfterStop_DoesNothing()
        {
            StartWith(V(2026, 11, 4, 9, 0), Window(8, 0, 17, 0));
            Stop();
            ClearRecorded();

            Input(TimerLogicFactory.RuleReset, true);

            Assert.Empty(Values(Output));
        }

        [Fact]
        public void TickAfterStop_DoesNothing()
        {
            StartWith(V(2026, 11, 4, 9, 0), Window(8, 0, 17, 0));
            Stop();
            ClearRecorded();

            FireTick();

            Assert.Empty(Values(Output));
            Assert.False(SutTimer.Enabled);
        }

        [Fact]
        public void Restart_UsesTheNewParameterInstance()
        {
            StartWith(V(2026, 11, 4, 9, 0), Window(8, 0, 17, 0));
            Assert.Equal(true, State);
            Stop();

            Interface(TimerLogicFactory.RuleTimerParameter).Value = Window(18, 0, 20, 0);
            Start();

            Assert.Equal(new object[] { true, false }, Values(Output));
        }

        [Fact]
        public void Start_DispatchesOnce()
        {
            StartWith(V(2026, 11, 4, 9, 0), Window(8, 0, 17, 0));

            Assert.Single(Values(Output));
        }

        [Fact(Skip = Bugs.TimerDstInterval)]
        public void SpringForward_NextTickIsInRealTime()
        {
            // 2026-03-29 01:00 CET (00:00Z), window 00:30 - 04:00: 04:00 CEST is 02:00Z -> 2 real hours (the wall clock difference is 3 h)
            StartWith(Utc(2026, 3, 29, 0, 0), Window(0, 30, 4, 0));

            Assert.Equal(true, State);
            Assert.InRange(IntervalMs, TimeSpan.FromHours(2).TotalMilliseconds - 1, TimeSpan.FromHours(2).TotalMilliseconds + 1);
        }

        [Fact(Skip = Bugs.TimerDstInterval)]
        public void FallBack_NextTickIsInRealTime()
        {
            // 2026-10-25 01:00 CEST (2026-10-24 23:00Z), window 00:30 - 04:00: 04:00 CET is 03:00Z -> 4 real hours (the wall clock difference is 3 h)
            StartWith(Utc(2026, 10, 24, 23, 0), Window(0, 30, 4, 0));

            Assert.Equal(true, State);
            Assert.InRange(IntervalMs, TimeSpan.FromHours(4).TotalMilliseconds - 1, TimeSpan.FromHours(4).TotalMilliseconds + 1);
        }

        [Fact(Skip = Bugs.TimerDstInterval)]
        public void FallBack_EarlyTick_DoesNotSwitchOffAnHourEarly()
        {
            // the timer was scheduled with the wall clock difference (3 h) and fires at 03:00 CET, one real hour before the stop
            StartWith(Utc(2026, 10, 24, 23, 0), Window(0, 30, 4, 0));

            SetUtcNow(Utc(2026, 10, 25, 2, 0)); // 03:00 CET
            FireTick();

            Assert.Equal(true, State);
        }

        private static DateTime Utc(int year, int month, int day, int hour, int minute = 0) =>
            new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

        [Fact]
        public void Factory_Templates()
        {
            var factory = new TimerLogicFactory();

            Assert.Equal("P3.Logic.Time.Timer", factory.LogicName);
            Assert.Equal(3, RuleInstance.RuleInterfaceInstance.Count);
            Assert.NotNull(Interface(TimerLogicFactory.RuleReset));
            Assert.NotNull(Interface(TimerLogicFactory.RuleOutput));
            Assert.NotNull(Interface(TimerLogicFactory.RuleTimerParameter));
        }
    }
}
