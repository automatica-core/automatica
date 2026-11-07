using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Automatica.Core.Base.Calendar;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Time.AdvancedTimer;
using P3.Logic.Time.Tests.TestSupport;
using Xunit;

namespace P3.Logic.Time.Tests.AdvancedTimer
{
    /// <summary>
    /// These tests used to take "now" from <see cref="DateTime.Now"/> and the machine's time zone, so they failed when they ran
    /// shortly before midnight (a window "now - 2h .. now + 2h" crossed midnight) or in the first hour of a day / on the last day of a month
    /// (date arithmetic). They now run at fixed instants in a pinned Europe/Vienna zone; the near-midnight and DST instants are kept on purpose.
    /// </summary>
    public class AdvancedTimerTests : LogicTest<AdvancedTimerRuleFactory>, IDisposable
    {
        private readonly TimeZoneScope _zone = TimeZoneScope.Vienna();

        public void Dispose()
        {
            // the fake time provider is static, don't leak a time zone into the next test
            Logic.Stop().GetAwaiter().GetResult();
            _zone.Dispose();
        }

        /// <summary>
        /// Wall times in Europe/Vienna that replace DateTime.Now: a normal morning, the hours around midnight, month and year end, both DST days
        /// </summary>
        public static IEnumerable<object[]> Nows => new[]
        {
            "2026-11-04 10:00:00", "2026-11-04 21:30:00", "2026-11-04 22:30:00", "2026-11-04 23:05:00", "2026-11-04 23:59:59",
            "2026-11-05 00:00:00", "2026-11-05 00:30:00", "2026-11-05 01:30:00",
            "2026-11-30 23:30:00", "2026-12-31 23:30:00", "2027-01-01 00:30:00",
            "2026-03-29 01:30:00", "2026-03-29 03:30:00", "2026-03-28 23:30:00",
            "2026-10-25 01:30:00", "2026-10-25 03:30:00", "2026-10-24 23:30:00"
        }.Select(a => new object[] { a });

        /// <summary>
        /// A local wall time (Kind Local, the pinned zone is Vienna)
        /// </summary>
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

        /// <summary>
        /// Runs the scheduled timer tick of the logic right away (instead of waiting for the real timer)
        /// </summary>
        private void FireTick()
        {
            var method = Logic.GetType().GetMethod("_timer_Elapsed", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            method.Invoke(Logic, new object[] { null, null });
        }

        private static RFC2445Recur.Rule DailyByDay(DayOfWeek today, bool onlyToday)
        {
            // -1 drops the day from the BYDAY list
            return new RFC2445Recur.Rule
            {
                Frequency = Frequency.DAILY,
                ByDay = Enum.GetValues<DayOfWeek>()
                    .Select(d => new Tuple<DayOfWeek, int>(d, (d == today) == onlyToday ? 0 : -1))
                    .ToArray()
            };
        }

        [Theory]
        [MemberData(nameof(Nows))]
        public async Task TestTimerRule(string wallTime)
        {
            await Restart(Local(wallTime));

            GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter).Value = new CalendarPropertyData
            {
                Value = [new() { AllDay = true }]
            };
            await Logic.Start();

            Assert.Equal(true, Current());
            await Logic.Stop();
        }

        [Theory]
        [MemberData(nameof(Nows))]
        public async Task TestTimerRule_AllDaysButNotToday(string wallTime)
        {
            var now = Local(wallTime);
            await Restart(now);

            GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter).Value = new CalendarPropertyData
            {
                Value =
                [
                    new()
                    {
                        StartDate = now.AddSeconds(1),
                        EndDate = now.AddSeconds(2),
                        RecurrenceRule = DailyByDay(now.DayOfWeek, onlyToday: false).ToString()
                    }
                ]
            };
            await Logic.Start();

            Assert.Equal(false, Current());
            await Logic.Stop();
        }

        [Theory]
        [MemberData(nameof(Nows))]
        public async Task TestTimerRule_OnlyTodayWeekday(string wallTime)
        {
            var now = Local(wallTime);
            await Restart(now);

            GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter).Value = new CalendarPropertyData
            {
                Value =
                [
                    new()
                    {
                        StartDate = now.AddHours(-1),
                        EndDate = now.AddHours(2),
                        RecurrenceRule = DailyByDay(now.DayOfWeek, onlyToday: true).ToString()
                    }
                ]
            };
            await Logic.Start();

            Assert.Equal(true, Current());
            await Logic.Stop();
        }

        [Theory]
        [MemberData(nameof(Nows))]
        public async Task TestTimerRule2(string wallTime)
        {
            var now = Local(wallTime);
            await Restart(now);

            GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter).Value = new CalendarPropertyData
            {
                Value =
                [
                    new CalendarPropertyDataEntry
                    {
                        StartDate = now.AddHours(-2),
                        EndDate = now.AddHours(2),
                        RecurrenceRule = "FREQ=DAILY"
                    }
                ]
            };
            await Logic.Start();

            Assert.Equal(true, Current());
            await Logic.Stop();
        }

        [Theory]
        [MemberData(nameof(Nows))]
        public async Task TestTimerRule3(string wallTime)
        {
            var now = Local(wallTime);
            await Restart(now);

            GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter).Value = new CalendarPropertyData
            {
                Value =
                [
                    new()
                    {
                        StartDate = now.AddHours(-2),
                        EndDate = now.AddHours(2),
                        RecurrenceRule = "FREQ=DAILY"
                    },

                    new()
                    {
                        StartDate = now.AddHours(-8),
                        EndDate = now.AddHours(-6),
                        RecurrenceRule = "FREQ=DAILY"
                    }
                ]
            };
            await Logic.Start();

            Assert.Equal(true, Current());
            await Logic.Stop();
        }

        private const string ThreeEntries =
            "{\"Value\":[{\"Text\":\"Morgens\",\"StartDate\":\"2023-12-14T05:00:00.000Z\",\"EndDate\":\"2023-12-14T07:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false,\"TrackingState\":1},{\"Text\":\"Abends\",\"StartDate\":\"2023-12-03T15:00:00.000Z\",\"EndDate\":\"2023-12-03T23:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false,\"TrackingState\":1},{\"Text\":\"Mittags\",\"StartDate\":\"2023-12-01T11:00:00.000Z\",\"EndDate\":\"2023-12-01T12:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false,\"TrackingState\":1}],\"TrackingState\":1}";

        [Theory]
        [MemberData(nameof(TestTimerRuleWithDifferentNowData))]
        public async Task TestTimerRule_WithDifferentNow(DateTime now, bool result)
        {
            await Restart(now);
            GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter).ValueString = ThreeEntries;

            await Logic.Start();

            Assert.Equal(result, Current());
            await Logic.Stop();
        }

        public static IEnumerable<object[]> TestTimerRuleWithDifferentNowData =>
            new List<object[]>
            {
                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(6, 0), DateTimeKind.Utc), true },
                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(7, 0), DateTimeKind.Utc), false },
                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(11, 30), DateTimeKind.Utc), true },
                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(17, 0), DateTimeKind.Utc), true },

                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(10, 0), DateTimeKind.Utc), false },
                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(13, 30), DateTimeKind.Utc), false },
                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(15, 28), DateTimeKind.Utc), true },

                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(14, 28), DateTimeKind.Utc), false },

                // was "today 20:12 UTC"
                new object[] { new DateTime(new DateOnly(2026, 11, 7), new TimeOnly(20, 12), DateTimeKind.Utc), true },

                new object[] { DateTime.MaxValue, false },
                new object[] { DateTime.MinValue, false },
            };

        private const string ThreeEntriesWithNight =
            "{\"Value\":[{\"Text\":\"Morgens\",\"StartDate\":\"2023-12-13T23:00:00.000Z\",\"EndDate\":\"2023-12-14T06:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false,\"TrackingState\":1},{\"Text\":\"Abends\",\"StartDate\":\"2023-12-03T15:00:00.000Z\",\"EndDate\":\"2023-12-03T23:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false,\"TrackingState\":1},{\"Text\":\"Mittags\",\"StartDate\":\"2023-12-01T11:00:00.000Z\",\"EndDate\":\"2023-12-01T12:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false,\"TrackingState\":1}],\"TrackingState\":1}";

        /// <param name="now">the UTC instant</param>
        /// <param name="result">the expected output</param>
        /// <param name="zone">"UTC" or "Local" (the pinned Europe/Vienna); resolved in the test because the data is built before the zone is pinned</param>
        [Theory]
        [MemberData(nameof(TestTimerRuleWithDifferentNowData2))]
        [MemberData(nameof(TestTimerRuleWithDifferentNowData2_1))]
        public async Task TestTimerRule_WithDifferentNow2(DateTime now, bool result, string zone)
        {
            await Restart(now);
            FakeTimeProvider.SetTimeZone(zone == "UTC" ? TimeZoneInfo.Utc : TimeZoneInfo.Local);
            GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter).ValueString = ThreeEntriesWithNight;

            await Logic.Start();

            Assert.Equal(result, Current());
            await Logic.Stop();
        }

        private static IEnumerable<object[]> DifferentNow2Rows(string zone) =>
            new List<object[]>
            {
                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(1, 0), DateTimeKind.Utc), true, zone },
                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(2, 0), DateTimeKind.Utc), true, zone },
                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(8, 0), DateTimeKind.Utc), false, zone },
                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(11, 30), DateTimeKind.Utc), true, zone },
                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(18, 0), DateTimeKind.Utc), true, zone },

                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(11, 0), DateTimeKind.Utc), true, zone },
                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(14, 30), DateTimeKind.Utc), false, zone },
                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(16, 28), DateTimeKind.Utc), true, zone },

                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(14, 28), DateTimeKind.Utc), false, zone },

                // was "today 19:12 UTC"
                new object[] { new DateTime(new DateOnly(2026, 11, 7), new TimeOnly(19, 12), DateTimeKind.Utc), true, zone },
            };

        public static IEnumerable<object[]> TestTimerRuleWithDifferentNowData2 => DifferentNow2Rows("UTC");

        public static IEnumerable<object[]> TestTimerRuleWithDifferentNowData2_1 => DifferentNow2Rows("Local");

        [Theory]
        [InlineData(2024, 1, 1, 2, 0)]
        [InlineData(2024, 1, 1, 5, 0)]
        [InlineData(2024, 1, 1, 8, 0)]
        [InlineData(2024, 1, 1, 12, 30)]
        [InlineData(2024, 1, 1, 18, 0)]
        [InlineData(2024, 1, 1, 11, 0)]
        [InlineData(2024, 1, 1, 14, 30)]
        [InlineData(2024, 1, 1, 16, 28)]
        [InlineData(2024, 1, 1, 15, 28)]
        [InlineData(2026, 11, 7, 21, 12)] // was "today 21:12"
        public async Task TestTimerRule_WithDifferentNow3(int year, int month, int day, int hour, int minute)
        {
            // the entries start in 2224 -> never active
            await Restart(new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Local));
            GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter).ValueString =
                "{\"Value\":[{\"Text\":\"Morgens\",\"StartDate\":\"2224-12-14T05:00:00.000Z\",\"EndDate\":\"2224-12-14T07:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false,\"TrackingState\":1},{\"Text\":\"Abends\",\"StartDate\":\"2224-12-03T15:00:00.000Z\",\"EndDate\":\"2224-12-03T23:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false,\"TrackingState\":1},{\"Text\":\"Mittags\",\"StartDate\":\"2224-12-01T11:00:00.000Z\",\"EndDate\":\"2224-12-01T12:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false,\"TrackingState\":1}],\"TrackingState\":1}";

            await Logic.Start();

            Assert.False((bool)Current());
            await Logic.Stop();
        }

        private const string Every10MinutesAt00 =
            "{\"Value\":[{\"Text\":\"Every10Minutes\",\"StartDate\":\"2024-02-09T23:00:00.000Z\",\"EndDate\":\"2024-02-09T23:10:00.000Z\",\"RecurrenceRule\":\"FREQ=HOURLY\",\"AllDay\":false,\"TrackingState\":1}],\"TrackingState\":1}";

        private const string Every20MinutesAt10 =
            "{\"Value\":[{\"Text\":\"Every10Minutes\",\"StartDate\":\"2024-02-09T23:10:00.000Z\",\"EndDate\":\"2024-02-09T23:30:00.000Z\",\"RecurrenceRule\":\"FREQ=HOURLY\",\"AllDay\":false,\"TrackingState\":1}],\"TrackingState\":1}";

        /// <summary>
        /// Full hours (local wall time) that replace "the current hour": midnight, normal hours, month end, DST days
        /// </summary>
        public static IEnumerable<object[]> Hours => new[]
        {
            "2026-11-05 00:00:00", "2026-11-05 01:00:00", "2026-11-05 13:00:00", "2026-11-05 23:00:00",
            "2026-11-30 00:00:00", "2026-12-01 00:00:00", "2026-03-29 04:00:00", "2026-10-25 04:00:00"
        }.Select(a => new object[] { a });

        [Theory]
        [MemberData(nameof(Hours))]
        public async Task TestTimerRule_Every10Minutes(string fullHour)
        {
            await Restart(Local(fullHour));
            GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter).ValueString = Every10MinutesAt00;

            await Logic.Start();

            Assert.Equal(true, Current());
            await Logic.Stop();
        }

        [Theory]
        [MemberData(nameof(Hours))]
        public async Task TestTimerRule_Every10Minutes2(string fullHour)
        {
            // one second before the full hour (used to be "now.Hour - 1", which threw in the first hour of a day)
            var hour = Local(fullHour);
            await Restart(hour.AddSeconds(-1));
            GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter).ValueString = Every10MinutesAt00;
            await Logic.Start();

            FakeTimeProvider.SetDateTime(hour);
            FireTick();

            Assert.Equal(true, Current());
            await Logic.Stop();
        }

        [Theory]
        [MemberData(nameof(Hours))]
        public async Task TestTimerRule_Every10Minutes3(string fullHour)
        {
            var hour = Local(fullHour);
            await Restart(hour);
            GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter).ValueString = Every10MinutesAt00;
            await Logic.Start();

            FakeTimeProvider.SetDateTime(hour.AddMinutes(1));
            FireTick();

            Assert.Equal(true, Current());
            await Logic.Stop();
        }

        [Theory]
        [InlineData("2026-11-05")]
        [InlineData("2026-11-30")]
        [InlineData("2026-12-31")]
        public async Task TestTimerRule_Every10Minutes4(string date)
        {
            var day = Local(date + " 00:00:00");

            // hh:10 - hh:30 -> on at 23:10
            await Restart(day.AddHours(23).AddMinutes(10));
            GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter).ValueString = Every20MinutesAt10;
            await Logic.Start();
            Assert.Equal(true, Current());

            // a tick at 00:01 (the clock was set back) is before that hour's window
            FakeTimeProvider.SetDateTime(day.AddMinutes(1));
            FireTick();
            Assert.Equal(false, Current());

            FakeTimeProvider.SetDateTime(day.AddMinutes(12));
            FireTick();
            Assert.Equal(true, Current());
            await Logic.Stop();
        }

        [Theory]
        [InlineData("2026-11-05")]
        [InlineData("2026-11-30")] // used to be "now.Day + 1", which threw on the last day of a month
        [InlineData("2026-12-31")]
        public async Task TestTimerRule_Every10Minutes5(string date)
        {
            var day = Local(date + " 00:00:00");

            // 23:59 is after the 23:10 - 23:30 window
            await Restart(day.AddHours(23).AddMinutes(59));
            GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter).ValueString = Every20MinutesAt10;
            await Logic.Start();
            Assert.Equal(false, Current());

            // the next day 00:10:01 is inside the 00:10 - 00:30 window
            FakeTimeProvider.SetDateTime(day.AddDays(1).AddMinutes(10).AddSeconds(1));
            FireTick();
            Assert.Equal(true, Current());

            FakeTimeProvider.SetDateTime(day.AddDays(1).AddMinutes(31));
            FireTick();
            Assert.Equal(false, Current());
            await Logic.Stop();
        }

        private async Task StartWith(DateTime utcNow, string calendarJson)
        {
            FakeTimeProvider.SetTimeZone(TimeZoneInfo.Utc);
            FakeTimeProvider.SetDateTime(utcNow);
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter).ValueString = calendarJson;
            await Logic.Start();
        }

        [Fact]
        public async Task TestTimerRule_ActiveEntryWinsOverCloserNextEntry()
        {
            // A 08:00-10:00 is active at 09:50, B 10:30-11:00 starts closer to now - A must win
            await StartWith(new DateTime(2024, 1, 1, 9, 50, 0, DateTimeKind.Utc),
                "{\"Value\":[{\"Text\":\"A\",\"StartDate\":\"2023-12-14T08:00:00.000Z\",\"EndDate\":\"2023-12-14T10:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false},{\"Text\":\"B\",\"StartDate\":\"2023-12-14T10:30:00.000Z\",\"EndDate\":\"2023-12-14T11:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false}]}");

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);
            Assert.Single(values);
            Assert.Equal(true, values.First().Value.Value);
            await Logic.Stop();
        }

        [Fact]
        public async Task TestTimerRule_OvernightEntryOnBeforeMidnight()
        {
            // 23:00-06:00 daily, 23:30 is inside the window that started today
            await StartWith(new DateTime(2024, 1, 1, 23, 30, 0, DateTimeKind.Utc),
                "{\"Value\":[{\"Text\":\"Night\",\"StartDate\":\"2023-12-13T23:00:00.000Z\",\"EndDate\":\"2023-12-14T06:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false}]}");

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);
            Assert.Single(values);
            Assert.Equal(true, values.First().Value.Value);
            await Logic.Stop();
        }

        [Fact]
        public async Task TestTimerRule_OvernightEntryTicksAtStart()
        {
            // shortly before 23:00 the output is off and the next tick has to be at 23:00 (not at 06:00)
            await StartWith(new DateTime(2024, 1, 1, 22, 59, 59, 500, DateTimeKind.Utc),
                "{\"Value\":[{\"Text\":\"Night\",\"StartDate\":\"2023-12-13T23:00:00.000Z\",\"EndDate\":\"2023-12-14T06:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false}]}");

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);
            Assert.Equal(false, values.First().Value.Value);

            FakeTimeProvider.SetDateTime(new DateTime(2024, 1, 1, 23, 0, 1, DateTimeKind.Utc));
            FireTick();

            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);
            Assert.Single(values);
            Assert.Equal(true, values.First().Value.Value);
            await Logic.Stop();
        }

        [Fact]
        public async Task TestTimerRule_ResetRecalculates()
        {
            await StartWith(new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc),
                "{\"Value\":[{\"Text\":\"A\",\"StartDate\":\"2023-12-14T08:00:00.000Z\",\"EndDate\":\"2023-12-14T10:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false}]}");
            await Context.Dispatcher.ClearValues();

            Logic.ValueChanged(GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleReset), Dispatchable, true);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);
            Assert.Single(values);
            Assert.Equal(true, values.First().Value.Value);
            await Logic.Stop();
        }
    }
}

