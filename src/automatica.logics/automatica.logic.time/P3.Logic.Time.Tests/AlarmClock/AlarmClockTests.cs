using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Time.AlarmClock;
using P3.Logic.Time.OperatingModes;
using P3.Logic.Time.Tests.TestSupport;
using Xunit;
using F = P3.Logic.Time.AlarmClock.AlarmClockLogicFactory;

namespace P3.Logic.Time.Tests.AlarmClock
{
    /// <summary>
    /// Alarm clock in Europe/Vienna. 2026-11-08 is a Sunday, 2026-11-09 a Monday (CET, UTC+1).
    /// </summary>
    public class AlarmClockTests : TimeLogicTest<AlarmClockLogicFactory>
    {
        private static readonly object[] Pulse = { true, false };

        public AlarmClockTests() : base(TimeZones.Vienna)
        {
        }

        private static DateTime V(int month, int day, int hour, int minute = 0, int second = 0) => TimeZones.ViennaToUtc(2026, month, day, hour, minute, second);

        private AlarmClockRule Rule => (AlarmClockRule)Sut;

        private void Configure(string alarms = null, double maxDuration = 120, double preAlarm = 180, double snooze = 300, bool confirmRequired = false, string suppress = null)
        {
            Interface(F.ParamAlarms).Value = alarms ?? AlarmEntries.DefaultJson;
            Interface(F.ParamMaxDuration).Value = maxDuration;
            Interface(F.ParamPreAlarm).Value = preAlarm;
            Interface(F.ParamSnooze).Value = snooze;
            Interface(F.ParamConfirmRequired).Value = confirmRequired;
            Interface(F.ParamSuppressModes).Value = suppress ?? F.DefaultSuppressModes;
            Start();
        }

        private static string Alarms(params object[] entries) => Newtonsoft.Json.JsonConvert.SerializeObject(entries);

        private DateTime? NextAlarmLocal => (DateTime?)Last(F.OutputNextAlarm);

        [Fact]
        public void DefaultAlarm_IsNextWorkday0600()
        {
            SetUtcNow(V(11, 8, 12));
            Configure();

            Assert.Equal(new DateTime(2026, 11, 9, 6, 0, 0), NextAlarmLocal);
            Assert.Equal(new object[] { true }, Values(F.OutputDefaultEnabled));
            Assert.True(SutTimer.Enabled);
        }

        [Fact]
        public void Alarm_RingsAndEndsAfterMaxDuration()
        {
            SetUtcNow(V(11, 9, 5, 50));
            Configure(preAlarm: 0);
            ClearRecorded();

            SetUtcNow(V(11, 9, 6));
            FireTick();

            Assert.Equal(Pulse, Values(F.OutputAlarmStart));
            Assert.Equal(new object[] { true }, Values(F.OutputAlarm));
            Assert.Equal(new object[] { true }, Values(F.OutputBuzzer));
            // the next alarm is Tuesday
            Assert.Equal(new DateTime(2026, 11, 10, 6, 0, 0), NextAlarmLocal);
            // the timer wakes at least once a minute for the operating modes
            Assert.Equal(OperatingModeWatcher.PollMilliseconds, SutTimer.Interval);

            SetUtcNow(V(11, 9, 6, 2));
            FireTick();

            Assert.Equal(new object[] { true, false }, Values(F.OutputBuzzer));
            Assert.Equal(new object[] { true, false }, Values(F.OutputAlarm));
            Assert.Equal(Pulse, Values(F.OutputAlarmEnd));
            Assert.False(Rule.AlarmActive);
        }

        [Fact]
        public void PreAlarm_PulsesBeforeTheAlarm()
        {
            SetUtcNow(V(11, 9, 5, 50));
            Configure(preAlarm: 180);

            SetUtcNow(V(11, 9, 5, 56, 59));
            FireTick();
            Assert.Empty(Values(F.OutputPreAlarm));
            // armed for the pre-alarm at 05:57
            Assert.Equal(1000, SutTimer.Interval);

            SetUtcNow(V(11, 9, 5, 57));
            FireTick();
            Assert.Equal(Pulse, Values(F.OutputPreAlarm));
            Assert.Empty(Values(F.OutputAlarmStart));

            SetUtcNow(V(11, 9, 6));
            FireTick();
            Assert.Equal(Pulse, Values(F.OutputPreAlarm)); // only once
            Assert.Equal(Pulse, Values(F.OutputAlarmStart));
        }

        [Fact]
        public void Snooze_SilencesAndRingsAgain()
        {
            SetUtcNow(V(11, 9, 5, 59, 59));
            Configure(preAlarm: 0, snooze: 300);
            SetUtcNow(V(11, 9, 6));
            FireTick();
            ClearRecorded();

            Assert.Empty(Input(F.InputSnooze, true));
            Assert.Equal(new object[] { false }, Values(F.OutputBuzzer));
            Assert.Equal(new object[] { 300 }, Values(F.OutputSnoozeRemaining));
            Assert.True(Rule.AlarmActive);
            Assert.Equal(1000, SutTimer.Interval);

            SetUtcNow(V(11, 9, 6, 2, 30));
            FireTick();
            Assert.Equal(150, Last(F.OutputSnoozeRemaining));

            SetUtcNow(V(11, 9, 6, 5));
            FireTick();
            Assert.Equal(new object[] { false, true }, Values(F.OutputBuzzer));
            Assert.Equal(0, Last(F.OutputSnoozeRemaining));
            Assert.Empty(Values(F.OutputAlarmEnd));
        }

        [Fact]
        public void ConfirmRequired_SnoozesAfterMaxDurationUntilConfirmed()
        {
            SetUtcNow(V(11, 9, 5, 59, 59));
            Configure(preAlarm: 0, maxDuration: 60, snooze: 120, confirmRequired: true);
            SetUtcNow(V(11, 9, 6));
            FireTick();

            SetUtcNow(V(11, 9, 6, 1));
            FireTick();
            Assert.False(Rule.Buzzer);
            Assert.True(Rule.AlarmActive);
            Assert.Equal(120, Last(F.OutputSnoozeRemaining));

            SetUtcNow(V(11, 9, 6, 3));
            FireTick();
            Assert.True(Rule.Buzzer);

            Assert.Empty(Input(F.InputConfirm, true));
            Assert.False(Rule.AlarmActive);
            Assert.False(Rule.Buzzer);
            Assert.Equal(Pulse, Values(F.OutputAlarmEnd));
        }

        [Fact]
        public void Toggle_DuringAlarm_EndsIt_OtherwiseSwitchesTheDefaultAlarm()
        {
            SetUtcNow(V(11, 8, 12));
            Configure();

            Input(F.InputToggle, true);
            Assert.Equal(new object[] { true, false }, Values(F.OutputDefaultEnabled));
            Assert.Null(NextAlarmLocal);

            Input(F.InputToggle, true);
            Assert.Equal(new object[] { true, false, true }, Values(F.OutputDefaultEnabled));
            Assert.Equal(new DateTime(2026, 11, 9, 6, 0, 0), NextAlarmLocal);

            SetUtcNow(V(11, 9, 6));
            FireTick();
            Assert.True(Rule.AlarmActive);
            Input(F.InputToggle, true);
            Assert.False(Rule.AlarmActive);
            Assert.True(Rule.Entries.Single(a => a.IsDefault).Enabled);
        }

        [Fact]
        public void Set_ChangesTheDefaultAlarmTime()
        {
            SetUtcNow(V(11, 8, 12));
            Configure();

            Input(F.InputSet, 390);
            Assert.Equal("06:30", Rule.Entries.Single(a => a.IsDefault).Time);
            Assert.Equal(new DateTime(2026, 11, 9, 6, 30, 0), NextAlarmLocal);

            Input(F.InputSet, 5000); // clamped to 23:59
            Assert.Equal("23:59", Rule.Entries.Single(a => a.IsDefault).Time);
        }

        [Fact]
        public void Disable_SuppressesAllAlarms()
        {
            SetUtcNow(V(11, 8, 12));
            Configure();

            Input(F.InputDisable, true);
            Assert.Null(NextAlarmLocal);

            SetUtcNow(V(11, 9, 6));
            FireTick();
            Assert.Empty(Values(F.OutputAlarmStart));

            Input(F.InputDisable, false);
            Assert.Equal(new DateTime(2026, 11, 10, 6, 0, 0), NextAlarmLocal);
        }

        [Fact]
        public void Vacation_SuppressesWeekdayAlarms()
        {
            SetUtcNow(V(11, 8, 12));
            var today = new DateOnly(2026, 11, 8);
            OperatingModeState.Publish(Recorder, OperatingModeState.Json(today, new Dictionary<DateOnly, string[]>
            {
                [new DateOnly(2026, 11, 9)] = new[] { OperatingModeContract.Vacation }
            }));
            Configure();

            Assert.Equal(new DateTime(2026, 11, 10, 6, 0, 0), NextAlarmLocal);
        }

        [Fact]
        public void ModeEntry_ReplacesTheWeekdayEntries()
        {
            SetUtcNow(V(11, 8, 12));
            var today = new DateOnly(2026, 11, 8);
            OperatingModeState.Publish(Recorder, OperatingModeState.Json(today, new Dictionary<DateOnly, string[]>
            {
                [new DateOnly(2026, 11, 9)] = new[] { OperatingModeContract.Vacation },
                [new DateOnly(2026, 11, 10)] = new[] { OperatingModeContract.SchoolHolidays }
            }));
            Configure(Alarms(
                new { id = "default", time = "06:00", enabled = true, days = new[] { 1, 2, 3, 4, 5 } },
                new { id = "late", time = "09:15", enabled = true, modes = new[] { "vacation" } }));

            Assert.Equal(new DateTime(2026, 11, 9, 9, 15, 0), NextAlarmLocal);

            // school holidays are not suppressed by default: Tuesday rings at 06:00
            SetUtcNow(V(11, 9, 9, 15));
            FireTick();
            Assert.Equal(Pulse, Values(F.OutputAlarmStart));
            Assert.Equal(new DateTime(2026, 11, 10, 6, 0, 0), NextAlarmLocal);
        }

        [Fact]
        public void ModeChange_IsPickedUpOnTheNextTick()
        {
            SetUtcNow(V(11, 8, 12));
            Configure();
            Assert.Equal(new DateTime(2026, 11, 9, 6, 0, 0), NextAlarmLocal);
            Assert.True(SutTimer.Interval <= OperatingModeWatcher.PollMilliseconds);

            OperatingModeState.Publish(Recorder, OperatingModeState.Json(new DateOnly(2026, 11, 8), new Dictionary<DateOnly, string[]>
            {
                [new DateOnly(2026, 11, 9)] = new[] { OperatingModeContract.PublicHoliday }
            }));
            SetUtcNow(V(11, 8, 12, 1));
            FireTick();

            Assert.Equal(new DateTime(2026, 11, 10, 6, 0, 0), NextAlarmLocal);
        }

        [Fact]
        public void VisuEdit_IsKeptAcrossRestarts_UntilTheConfiguredValueChanges()
        {
            SetUtcNow(V(11, 8, 12));
            Configure();

            var edited = Alarms(new { id = "default", time = "07:45", enabled = true, days = new[] { 0, 1 } });
            Sut.ValueChanged(Interface(F.ParamAlarms), new VisuDispatchable(Interface(F.ParamAlarms).ObjId), edited);

            Assert.Equal("07:45", Rule.Entries.Single().Time);
            Assert.Equal(new DateTime(2026, 11, 9, 7, 45, 0), NextAlarmLocal);
            Assert.Contains("07:45", (string)Last(F.OutputEntries));

            // restart: a new instance on the same dispatcher (the remanent values are restored before the logics start)
            Stop();
            var restarted = (AlarmClockRule)Instance.CreateLogicInstance(new LogicContextMock(RuleInstance, Factory, Recorder));
            restarted.Start().GetAwaiter().GetResult();
            try
            {
                Assert.Equal("07:45", restarted.Entries.Single().Time);
                Assert.Equal(new object[] { 0, 1 }, restarted.Entries.Single().Days.Cast<object>().ToArray());

                // the admin changes the entries in the logic editor: the configured value wins again
                Interface(F.ParamAlarms).Value = Alarms(new { id = "default", time = "05:30", enabled = true, days = new[] { 1 } });
                restarted.Restart(RuleInstance).GetAwaiter().GetResult();
                Assert.Equal("05:30", restarted.Entries.Single().Time);
            }
            finally
            {
                restarted.Stop().GetAwaiter().GetResult();
            }
        }

        [Fact]
        public void VisuEdit_InvalidJsonIsIgnored()
        {
            SetUtcNow(V(11, 8, 12));
            Configure();
            Sut.ValueChanged(Interface(F.ParamAlarms), new VisuDispatchable(Interface(F.ParamAlarms).ObjId), "{not json");
            Assert.Equal("06:00", Rule.Entries.Single().Time);
        }

        [Fact]
        public void Dst_AlarmAfterTheSpringChangeUsesSummerTime()
        {
            // 2026-03-29 02:00 -> 03:00; Monday 2026-03-30 06:00 CEST = 04:00 UTC
            SetUtcNow(V(3, 28, 12));
            Configure();
            Assert.Equal(new DateTime(2026, 3, 30, 6, 0, 0), NextAlarmLocal);
            Assert.Equal(new DateTimeOffset(2026, 3, 30, 4, 0, 0, TimeSpan.Zero), Rule.NextAlarm!.Value.ToUniversalTime());
        }

        [Fact]
        public void Entries_ForDay()
        {
            var entries = AlarmEntries.Parse(Alarms(
                new { id = "a", time = "06:00", enabled = true, days = new[] { 1 } },
                new { id = "b", time = "08:00", enabled = false, modes = new[] { "vacation" } },
                new { id = "c", time = "07:00", enabled = true, modes = new[] { "custom" } },
                new { id = "d", time = "25:00", enabled = true, days = new[] { 1 } }));
            var monday = new DateOnly(2026, 11, 9);
            var suppress = new[] { "vacation" };

            Assert.Equal(3, entries.Count); // invalid time dropped
            Assert.Equal(new[] { "a" }, AlarmEntries.ForDay(entries, monday, Array.Empty<string>(), suppress).Select(a => a.Id));
            // disabled vacation entry does not count, vacation suppresses
            Assert.Empty(AlarmEntries.ForDay(entries, monday, new[] { "vacation" }, suppress));
            Assert.Equal(new[] { "c" }, AlarmEntries.ForDay(entries, monday, new[] { "vacation", "custom" }, suppress).Select(a => a.Id));
            Assert.Empty(AlarmEntries.ForDay(entries, monday.AddDays(1), Array.Empty<string>(), suppress));
        }

        [Fact]
        public void Templates_AreComplete()
        {
            var keys = new[] { "confirm", "snooze", "disable", "toggle", "set", "off", "alarm_start", "alarm_end", "buzzer", "alarm", "pre_alarm",
                "snooze_remaining", "default_enabled", "next_alarm", "entries", "max_duration", "pre_alarm_time", "snooze_duration", "confirm_required",
                "suppress_modes", "alarms" };
            foreach (var key in keys)
            {
                Assert.Contains(RuleInstance.RuleInterfaceInstance, a => a.This2RuleInterfaceTemplateNavigation.Key == key);
            }
        }

        [Fact]
        public void VisuData_ContainsTheEntries()
        {
            SetUtcNow(V(11, 8, 12));
            Configure();
            var data = (Dictionary<string, object>)Sut.GetDataForVisu();
            Assert.Contains("06:00", (string)data[F.AlarmsKey]);
            Assert.Equal(new DateTime(2026, 11, 9, 6, 0, 0), data["nextAlarm"]);
        }
    }
}
