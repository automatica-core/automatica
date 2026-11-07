using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.EF.Models;
using P3.Logic.Time.AdvancedTimer;
using P3.Logic.Time.Tests.TestSupport;
using Xunit;

namespace P3.Logic.Time.Tests.AdvancedTimer
{
    /// <summary>
    /// AdvancedTimer with a fixed clock in UTC. 2026-11-04 is a wednesday.
    /// </summary>
    public class AdvancedTimerScheduleTests : AdvancedTimerTestBase
    {
        public AdvancedTimerScheduleTests() : base(TimeZoneInfo.Utc)
        {
        }

        // daily 08:00 - 10:00, created some days ago
        private static CalendarPropertyDataEntry Morning => Entry(Utc(2026, 10, 1, 8, 0), Utc(2026, 10, 1, 10, 0));

        // daily 22:00 - 06:00 (crosses midnight)
        private static CalendarPropertyDataEntry Night => Entry(Utc(2026, 10, 1, 22, 0), Utc(2026, 10, 2, 6, 0));

        [Theory]
        [InlineData(0, 0, 0, false)]
        [InlineData(7, 59, 59, false)]
        [InlineData(8, 0, 0, true)]   // start is inclusive
        [InlineData(8, 0, 1, true)]
        [InlineData(9, 0, 0, true)]
        [InlineData(9, 59, 59, true)]
        [InlineData(10, 0, 0, false)] // end is exclusive
        [InlineData(10, 0, 1, false)]
        [InlineData(15, 0, 0, false)]
        [InlineData(23, 59, 59, false)]
        public void DailyWindow_StateAtStart(int hour, int minute, int second, bool expected)
        {
            StartWith(Utc(2026, 11, 4, hour, minute, second), Morning);

            AssertState(expected);
        }

        [Fact]
        public void DailyWindow_Start_DispatchesExactlyOneState()
        {
            StartWith(Utc(2026, 11, 4, 9, 0), Morning);

            Assert.Equal(new object[] { true }, Values(Output));
        }

        [Fact]
        public void DailyWindow_BeforeStart_NextTickIsTheStart()
        {
            StartWith(Utc(2026, 11, 4, 7, 59, 59, 500), Morning);

            AssertState(false);
            AssertNextTickIn(TimeSpan.FromMilliseconds(500));
        }

        [Fact]
        public void DailyWindow_Active_NextTickIsTheEnd()
        {
            StartWith(Utc(2026, 11, 4, 9, 30), Morning);

            AssertState(true);
            AssertNextTickIn(TimeSpan.FromMinutes(30));
        }

        [Fact]
        public void DailyWindow_TicksSwitchOnAndOff()
        {
            StartWith(Utc(2026, 11, 4, 7, 59, 59, 500), Morning);
            AssertState(false);

            TickAt(Utc(2026, 11, 4, 8, 0, 0, 1));
            AssertState(true);
            AssertNextTickIn(TimeSpan.FromHours(2) - TimeSpan.FromMilliseconds(1));

            TickAt(Utc(2026, 11, 4, 10, 0, 0, 1));
            AssertState(false);

            Assert.Equal(new object[] { false, true, false }, Values(Output));
        }

        [Fact]
        public void DailyWindow_RepeatsTheNextDay()
        {
            StartWith(Utc(2026, 11, 4, 9, 0), Morning);
            AssertState(true);

            TickAt(Utc(2026, 11, 4, 10, 0, 1));
            AssertState(false);

            TickAt(Utc(2026, 11, 5, 8, 30));
            AssertState(true);
        }

        [Fact]
        public void DailyWindow_TickTimeIsAlwaysPositive()
        {
            foreach (var hour in Enumerable.Range(0, 24))
            {
                TickAt(Utc(2026, 11, 4, hour, 17));
                Assert.True(IntervalMs > 0, $"interval at {hour}:17 is {IntervalMs}");
            }
        }

        [Theory]
        [InlineData(4, 12, 0, false)]
        [InlineData(4, 21, 59, false)]
        [InlineData(4, 22, 0, true)]   // start inclusive
        [InlineData(4, 23, 0, true)]
        [InlineData(4, 23, 59, true)]
        [InlineData(5, 0, 0, true)]    // midnight
        [InlineData(5, 0, 1, true)]
        [InlineData(5, 3, 0, true)]
        [InlineData(5, 5, 59, true)]
        [InlineData(5, 6, 0, false)]   // end exclusive
        [InlineData(5, 7, 0, false)]
        public void OvernightWindow_StateAtStart(int day, int hour, int minute, bool expected)
        {
            StartWith(Utc(2026, 11, day, hour, minute), Night);

            AssertState(expected);
        }

        [Fact]
        public void OvernightWindow_BeforeMidnight_NextTickIsTomorrowsEnd()
        {
            StartWith(Utc(2026, 11, 4, 23, 30), Night);

            AssertState(true);
            AssertNextTickIn(TimeSpan.FromHours(6.5));
        }

        [Fact]
        public void OvernightWindow_AfterMidnight_NextTickIsTodaysEnd()
        {
            StartWith(Utc(2026, 11, 5, 3, 0), Night);

            AssertState(true);
            AssertNextTickIn(TimeSpan.FromHours(3));
        }

        [Fact]
        public void OvernightWindow_ShortlyBeforeStart_NextTickIsTheStartNotTheEnd()
        {
            StartWith(Utc(2026, 11, 4, 21, 59, 59, 500), Night);

            AssertState(false);
            AssertNextTickIn(TimeSpan.FromMilliseconds(500));
        }

        [Fact]
        public void OvernightWindow_TicksAcrossMidnight()
        {
            StartWith(Utc(2026, 11, 4, 21, 59, 59, 500), Night);
            AssertState(false);

            TickAt(Utc(2026, 11, 4, 22, 0, 0, 1));
            AssertState(true);

            // a recheck at midnight keeps it on
            TickAt(Utc(2026, 11, 5, 0, 0, 0));
            AssertState(true);
            AssertNextTickIn(TimeSpan.FromHours(6));

            TickAt(Utc(2026, 11, 5, 6, 0, 0, 1));
            AssertState(false);

            Assert.Equal(new object[] { false, true, true, false }, Values(Output));
        }

        [Fact]
        public void TwoEntries_ActiveOneWins_EvenIfTheOtherStartsCloser()
        {
            var a = Entry(Utc(2026, 10, 1, 8, 0), Utc(2026, 10, 1, 10, 0));
            var b = Entry(Utc(2026, 10, 1, 10, 30), Utc(2026, 10, 1, 11, 0));

            StartWith(Utc(2026, 11, 4, 9, 50), a, b);

            AssertState(true);
            AssertNextTickIn(TimeSpan.FromMinutes(10));
        }

        [Fact]
        public void TwoEntries_Overlapping_NextTickIsTheLaterEnd()
        {
            var a = Entry(Utc(2026, 10, 1, 8, 0), Utc(2026, 10, 1, 10, 0));
            var b = Entry(Utc(2026, 10, 1, 9, 0), Utc(2026, 10, 1, 12, 0));

            StartWith(Utc(2026, 11, 4, 9, 30), a, b);

            AssertState(true);
            AssertNextTickIn(TimeSpan.FromHours(2.5));
        }

        [Fact]
        public void TwoEntries_InTheGap_OffAndNextTickIsTheNextStart()
        {
            var a = Entry(Utc(2026, 10, 1, 8, 0), Utc(2026, 10, 1, 10, 0));
            var b = Entry(Utc(2026, 10, 1, 10, 30), Utc(2026, 10, 1, 11, 0));

            StartWith(Utc(2026, 11, 4, 10, 15), a, b);

            AssertState(false);
            AssertNextTickIn(TimeSpan.FromMinutes(15));
        }

        [Theory]
        [InlineData(6, 30, true)]
        [InlineData(7, 30, false)]
        [InlineData(11, 30, true)]
        [InlineData(13, 0, false)]
        [InlineData(16, 0, true)]
        [InlineData(23, 30, false)]
        public void ThreeEntries_TruthTable(int hour, int minute, bool expected)
        {
            // the data of the existing tests: 05-07, 15-23, 11-12
            StartWith(Utc(2026, 11, 4, hour, minute),
                Entry(Utc(2023, 12, 14, 5, 0), Utc(2023, 12, 14, 7, 0)),
                Entry(Utc(2023, 12, 3, 15, 0), Utc(2023, 12, 3, 23, 0)),
                Entry(Utc(2023, 12, 1, 11, 0), Utc(2023, 12, 1, 12, 0)));

            AssertState(expected);
        }

        [Fact]
        public void AllDayEntry_IsOnAllDay_AndTicksAtTheEndOfTheDay()
        {
            SetUtcNow(Utc(2026, 11, 4, 0, 0, 0));
            Interface(AdvancedTimerRuleFactory.RuleTimerParameter).Value = new CalendarPropertyData
            {
                Value = [new CalendarPropertyDataEntry { AllDay = true }]
            };
            Start();
            AssertState(true);

            TickAt(Utc(2026, 11, 4, 23, 59, 59));
            AssertState(true);
            Assert.InRange(IntervalMs, 999, 1000);
        }

        [Theory]
        [InlineData(4, 9, 0, true)]
        [InlineData(4, 7, 0, false)]
        [InlineData(4, 11, 0, false)]
        [InlineData(3, 9, 0, false)]  // not today
        [InlineData(5, 9, 0, false)]  // not today
        public void SingleEntryWithoutRecurrence_OnlyOnItsDay(int day, int hour, int minute, bool expected)
        {
            StartWith(Utc(2026, 11, day, hour, minute), Entry(Utc(2026, 11, 4, 8, 0), Utc(2026, 11, 4, 10, 0), recurrence: null));

            AssertState(expected);
        }

        [Fact]
        public void SingleEntryWithoutRecurrence_NotToday_RechecksEveryMinute()
        {
            StartWith(Utc(2026, 11, 2, 9, 0), Entry(Utc(2026, 11, 4, 8, 0), Utc(2026, 11, 4, 10, 0), recurrence: ""));

            AssertState(false);
            AssertNextTickIn(TimeSpan.FromMinutes(1));
        }

        [Theory]
        [InlineData(4, 23, 0, true)]
        [InlineData(5, 1, 0, true)]
        [InlineData(5, 2, 0, false)]
        [InlineData(4, 21, 0, false)]
        public void SingleEntryWithoutRecurrence_AcrossMidnight(int day, int hour, int minute, bool expected)
        {
            StartWith(Utc(2026, 11, day, hour, minute), Entry(Utc(2026, 11, 4, 22, 0), Utc(2026, 11, 5, 2, 0), recurrence: null));

            AssertState(expected);
        }

        [Fact]
        public void EmptyCalendar_IsOff_AndRechecksIn15Minutes()
        {
            StartWith(Utc(2026, 11, 4, 9, 0));

            AssertState(false);
            AssertNextTickIn(TimeSpan.FromMinutes(15));
        }

        [Fact]
        public void CalendarWithoutEntryList_DispatchesNothing()
        {
            SetUtcNow(Utc(2026, 11, 4, 9, 0));
            Interface(AdvancedTimerRuleFactory.RuleTimerParameter).Value = new CalendarPropertyData { Value = null };
            Start();

            Assert.Empty(Values(Output));

            FireTick();
            Assert.Empty(Values(Output));
        }

        [Theory(Skip = Bugs.InvalidRecurrenceThrows)]
        [InlineData("INTERVAL=2")]
        [InlineData("FREQ=FOO")]
        [InlineData("garbage")]
        public void InvalidRecurrence_EntryIsSkipped(string rule)
        {
            StartWith(Utc(2026, 11, 4, 9, 0), Entry(Utc(2026, 10, 1, 8, 0), Utc(2026, 10, 1, 10, 0), rule));

            AssertState(false);
            AssertNextTickIn(TimeSpan.FromMinutes(1));
        }

        [Theory]
        [InlineData("INTERVAL=2")]
        [InlineData("FREQ=FOO")]
        [InlineData("garbage")]
        public void InvalidRecurrence_CurrentlyThrowsOnStart(string rule)
        {
            // pins today's behaviour (see InvalidRecurrence_EntryIsSkipped): an unparsable rule fails the start
            SetUtcNow(Utc(2026, 11, 4, 9, 0));
            Interface(AdvancedTimerRuleFactory.RuleTimerParameter).Value = new CalendarPropertyData
            {
                Value = [Entry(Utc(2026, 10, 1, 8, 0), Utc(2026, 10, 1, 10, 0), rule)]
            };

            Assert.Throws<FormatException>(() => Sut.Start().GetAwaiter().GetResult());
            Assert.Empty(Values(Output));
        }

        [Fact(Skip = Bugs.InvalidRecurrenceThrows)]
        public void InvalidRecurrence_DoesNotHideAValidEntry()
        {
            StartWith(Utc(2026, 11, 4, 9, 0),
                Entry(Utc(2026, 10, 1, 8, 0), Utc(2026, 10, 1, 10, 0), "INTERVAL=2"),
                Morning);

            AssertState(true);
        }

        [Fact]
        public void EntryStartingInTheFuture_IsOff()
        {
            // daily from next week on
            StartWith(Utc(2026, 11, 4, 9, 0), Entry(Utc(2026, 11, 11, 8, 0), Utc(2026, 11, 11, 10, 0)));

            AssertState(false);
        }

        [Fact]
        public void EntryStartingToday_IsOnInItsWindow()
        {
            StartWith(Utc(2026, 11, 4, 9, 0), Entry(Utc(2026, 11, 4, 8, 0), Utc(2026, 11, 4, 10, 0)));

            AssertState(true);
        }

        // ---- weekday masks ----

        // 2026-11-04 wednesday, 2026-11-07 saturday
        [Theory]
        [InlineData(4, true)]  // wednesday, in the mask
        [InlineData(5, true)]  // thursday
        [InlineData(6, true)]  // friday
        public void WeekdayMask_EnabledDay_IsOn(int day, bool expected)
        {
            StartWith(Utc(2026, 11, day, 9, 0), Entry(Utc(2026, 11, 2, 8, 0), Utc(2026, 11, 2, 10, 0), "FREQ=DAILY;BYDAY=MO,TU,WE,TH,FR"));

            AssertState(expected);
        }

        [Theory(Skip = Bugs.WeekdayMaskIgnored)]
        [InlineData(7)]  // saturday
        [InlineData(8)]  // sunday
        public void WeekdayMask_DisabledDay_IsOff(int day)
        {
            StartWith(Utc(2026, 11, day, 9, 0), Entry(Utc(2026, 11, 2, 8, 0), Utc(2026, 11, 2, 10, 0), "FREQ=DAILY;BYDAY=MO,TU,WE,TH,FR"));

            AssertState(false);
        }

        [Fact]
        public void WeekdayMask_FirstOccurrenceStillAhead_IsOff()
        {
            // created on monday for saturdays only -> the first saturday has not come yet on wednesday
            StartWith(Utc(2026, 11, 4, 9, 0), Entry(Utc(2026, 11, 2, 8, 0), Utc(2026, 11, 2, 10, 0), "FREQ=DAILY;BYDAY=SA"));

            AssertState(false);
        }

        [Theory(Skip = Bugs.WeeklyWithoutWkstThrows)]
        [InlineData(4, true)]   // wednesday
        [InlineData(5, false)]  // thursday
        public void WeeklyRecurrence_WithoutWkst_Starts(int day, bool expected)
        {
            // the scheduler UI sends weekly rules without WKST
            StartWith(Utc(2026, 11, day, 9, 0), Entry(Utc(2026, 11, 2, 8, 0), Utc(2026, 11, 2, 10, 0), "FREQ=WEEKLY;BYDAY=MO,WE,FR"));

            AssertState(expected);
        }

        [Fact]
        public void WeeklyRecurrence_WithWkst_Starts()
        {
            StartWith(Utc(2026, 11, 4, 9, 0), Entry(Utc(2026, 11, 2, 8, 0), Utc(2026, 11, 2, 10, 0), "FREQ=WEEKLY;WKST=MO;BYDAY=MO,WE,FR"));

            AssertState(true);
        }

        [Fact]
        public void WeekdayMask_OnlyToday_IsOnInTheWindow()
        {
            StartWith(Utc(2026, 11, 4, 9, 0), Entry(Utc(2026, 11, 4, 8, 0), Utc(2026, 11, 4, 10, 0), "FREQ=DAILY;BYDAY=WE"));

            AssertState(true);
        }

        [Fact(Skip = Bugs.WeekdayMaskIgnored)]
        public void WeekdayMask_AllButToday_IsOff()
        {
            StartWith(Utc(2026, 11, 4, 9, 0), Entry(Utc(2026, 11, 4, 8, 0), Utc(2026, 11, 4, 10, 0), "FREQ=DAILY;BYDAY=MO,TU,TH,FR,SA,SU"));

            AssertState(false);
        }

        // ---- hourly ----

        // every hour hh:00 - hh:10
        private static CalendarPropertyDataEntry Every10Minutes => Entry(Utc(2024, 2, 9, 23, 0), Utc(2024, 2, 9, 23, 10), "FREQ=HOURLY");

        [Theory]
        [InlineData(14, 0, true)]
        [InlineData(14, 4, true)]
        [InlineData(14, 20, false)]
        [InlineData(14, 59, false)]
        [InlineData(0, 2, true)]
        [InlineData(23, 1, true)]
        public void Hourly_StateAtStart(int hour, int minute, bool expected)
        {
            StartWith(Utc(2026, 11, 4, hour, minute), Every10Minutes);

            AssertState(expected);
        }

        [Theory(Skip = Bugs.HourlyGraceCutsWindow)]
        [InlineData(14, 6)]
        [InlineData(14, 9)]
        public void Hourly_LateInTheWindow_IsStillOn(int hour, int minute)
        {
            StartWith(Utc(2026, 11, 4, hour, minute), Every10Minutes);

            AssertState(true);
        }

        [Fact]
        public void Hourly_AfterTheWindow_NextTickIsTheNextHour()
        {
            StartWith(Utc(2026, 11, 4, 14, 20), Every10Minutes);

            AssertState(false);
            AssertNextTickIn(TimeSpan.FromMinutes(40));

            TickAt(Utc(2026, 11, 4, 15, 0, 1));
            AssertState(true);
        }

        // ---- reset ----

        public static IEnumerable<object[]> Truthy => new[]
        {
            new object[] { true }, new object[] { 1 }, new object[] { 1L }, new object[] { 2.5d }, new object[] { -1 },
            new object[] { "true" }, new object[] { "True" }, new object[] { "1" }, new object[] { "0.5" }
        };

        public static IEnumerable<object[]> Falsy => new[]
        {
            new object[] { false }, new object[] { 0 }, new object[] { 0d }, new object[] { "false" }, new object[] { "0" },
            new object[] { "" }, new object[] { "abc" }, new object[] { null }, new object[] { new object() }
        };

        [Theory]
        [MemberData(nameof(Truthy))]
        public void Reset_Truthy_RecalculatesForNow(object value)
        {
            StartWith(Utc(2026, 11, 4, 7, 0), Morning);
            AssertState(false);

            SetUtcNow(Utc(2026, 11, 4, 9, 0));
            var result = Input(AdvancedTimerRuleFactory.RuleReset, value);

            Assert.Empty(result);
            AssertState(true);
            AssertNextTickIn(TimeSpan.FromHours(1));
        }

        [Theory]
        [MemberData(nameof(Falsy))]
        public void Reset_Falsy_DoesNothing(object value)
        {
            StartWith(Utc(2026, 11, 4, 7, 0), Morning);
            ClearRecorded();

            SetUtcNow(Utc(2026, 11, 4, 9, 0));
            var result = Input(AdvancedTimerRuleFactory.RuleReset, value);

            Assert.Empty(result);
            Assert.Empty(Values(Output));
        }

        [Fact]
        public void Reset_SameState_IsDispatchedAgain()
        {
            StartWith(Utc(2026, 11, 4, 9, 0), Morning);

            Input(AdvancedTimerRuleFactory.RuleReset, true);
            Input(AdvancedTimerRuleFactory.RuleReset, true);

            Assert.Equal(new object[] { true, true, true }, Values(Output));
        }

        [Fact]
        public void Reset_WithoutCalendar_DoesNothing()
        {
            SetUtcNow(Utc(2026, 11, 4, 9, 0));
            Interface(AdvancedTimerRuleFactory.RuleTimerParameter).Value = null;
            Start();

            Input(AdvancedTimerRuleFactory.RuleReset, true);

            Assert.Empty(Values(Output));
        }

        [Fact]
        public void OutputInput_IsIgnored()
        {
            StartWith(Utc(2026, 11, 4, 9, 0), Morning);
            ClearRecorded();

            Assert.Empty(Input(AdvancedTimerRuleFactory.RuleOutput, false));
            Assert.Empty(Values(Output));
        }

        // ---- start / stop ----

        [Fact]
        public void Start_WithoutCalendarValue_DispatchesNothing()
        {
            SetUtcNow(Utc(2026, 11, 4, 9, 0));
            Interface(AdvancedTimerRuleFactory.RuleTimerParameter).Value = null;

            Start();

            Assert.Empty(Values(Output));
        }

        [Fact]
        public void Stop_StopsTheTimer()
        {
            StartWith(Utc(2026, 11, 4, 9, 0), Morning);
            Assert.True(SutTimer.Enabled);

            Stop();

            Assert.False(SutTimer.Enabled);
        }

        [Fact]
        public void Restart_UsesTheNewCalendar()
        {
            StartWith(Utc(2026, 11, 4, 9, 0), Morning);
            AssertState(true);
            Stop();

            Interface(AdvancedTimerRuleFactory.RuleTimerParameter).Value = new CalendarPropertyData { Value = [Night] };
            Start();

            AssertState(false);
            Assert.Equal(new object[] { true, false }, Values(Output));
        }

        [Fact]
        public void Start_FromJsonValueString()
        {
            SetUtcNow(Utc(2026, 11, 4, 9, 0));
            Interface(AdvancedTimerRuleFactory.RuleTimerParameter).ValueString =
                "{\"Value\":[{\"Text\":\"A\",\"StartDate\":\"2023-12-14T08:00:00.000Z\",\"EndDate\":\"2023-12-14T10:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false}]}";
            Start();

            AssertState(true);
        }

        // ---- factory ----

        [Fact]
        public void Factory_Templates()
        {
            var factory = new AdvancedTimerRuleFactory();

            Assert.Equal("P3.Logic.Time.AdvancedTimer", factory.LogicName);
            Assert.Equal(new Guid("8094b053-e226-4f0c-8e2e-bbb71fda9eb9"), factory.LogicGuid);
            Assert.NotNull(Interface(AdvancedTimerRuleFactory.RuleReset));
            Assert.NotNull(Interface(AdvancedTimerRuleFactory.RuleOutput));
            Assert.NotNull(Interface(AdvancedTimerRuleFactory.RuleTimerParameter));
            Assert.Equal(3, RuleInstance.RuleInterfaceInstance.Count);
            Assert.Equal(3, new[] { AdvancedTimerRuleFactory.RuleReset, AdvancedTimerRuleFactory.RuleOutput, AdvancedTimerRuleFactory.RuleTimerParameter }.Distinct().Count());
        }
    }
}
