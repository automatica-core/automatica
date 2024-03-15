using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.Calendar;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Logics;
using Newtonsoft.Json;
using P3.Logic.Time.AdvancedTimer;
using Xunit;

namespace P3.Logic.Time.Tests.AdvancedTimer
{
    public class AdvancedTimerTests : LogicTest<AdvancedTimerRuleFactory>
    {
        [Fact]
        public async void TestTimerRule()
        {
            FakeTimeProvider.SetDateTime(DateTime.Now);
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            var paramDelay = GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter);
            paramDelay.Value = new CalendarPropertyData()
            {
                Value =
                [
                    new()
                    {
                        AllDay = true
                    }
                ]
            };
            await Logic.Start();
            await Task.Delay(200);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            //Assert.Equal(true, values.First().Value.Value);

            await Task.Delay(200);

            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.Equal(true, values.First().Value.Value);
            await Logic.Stop();
        }

        [Fact]
        public async void TestTimerRule_AllDaysButNotToday()
        {
            FakeTimeProvider.SetDateTime(DateTime.Now);
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            var paramDelay = GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter);

            var recurrenceRule = new RFC2445Recur.Rule();
            recurrenceRule.ByDay = new[]
            {
                new Tuple<DayOfWeek, int>(DayOfWeek.Sunday, DateTime.Now.DayOfWeek == DayOfWeek.Sunday ? -1 : 0),
                new Tuple<DayOfWeek, int>(DayOfWeek.Monday, DateTime.Now.DayOfWeek == DayOfWeek.Monday ? -1 : 0),
                new Tuple<DayOfWeek, int>(DayOfWeek.Tuesday, DateTime.Now.DayOfWeek == DayOfWeek.Tuesday ? -1 : 0),
                new Tuple<DayOfWeek, int>(DayOfWeek.Wednesday, DateTime.Now.DayOfWeek == DayOfWeek.Wednesday ? -1 : 0),
                new Tuple<DayOfWeek, int>(DayOfWeek.Thursday, DateTime.Now.DayOfWeek == DayOfWeek.Thursday ? -1 : 0),
                new Tuple<DayOfWeek, int>(DayOfWeek.Friday, DateTime.Now.DayOfWeek == DayOfWeek.Friday ? -1 : 0),
                new Tuple<DayOfWeek, int>(DayOfWeek.Saturday, DateTime.Now.DayOfWeek == DayOfWeek.Saturday ? -1 : 0)
            };


            recurrenceRule.Frequency = Frequency.DAILY;

            var timerData = new CalendarPropertyData
            {
                Value =
                [
                    new()
                    {
                        StartDate = DateTime.Now.AddSeconds(1),
                        EndDate = DateTime.Now.AddSeconds(2),
                        RecurrenceRule = recurrenceRule.ToString()
                    }
                ]
            };

            paramDelay.Value = timerData;

            await Logic.Start();
            await Task.Delay(200);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.False((bool)values.First().Value.Value);
            await Logic.Stop();
        }

        [Fact]
        public async void TestTimerRule_OnlyTodayWeekday()
        {
            FakeTimeProvider.SetDateTime(DateTime.Now);
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            var paramDelay = GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter);

            var recurrenceRule = new RFC2445Recur.Rule();
            recurrenceRule.ByDay = new[]
            {
                new Tuple<DayOfWeek, int>(DayOfWeek.Sunday, DateTime.Now.DayOfWeek == DayOfWeek.Sunday ? 0 : -1),
                new Tuple<DayOfWeek, int>(DayOfWeek.Monday, DateTime.Now.DayOfWeek == DayOfWeek.Monday ? 0 : -1),
                new Tuple<DayOfWeek, int>(DayOfWeek.Tuesday, DateTime.Now.DayOfWeek == DayOfWeek.Tuesday ? 0 : -1),
                new Tuple<DayOfWeek, int>(DayOfWeek.Wednesday, DateTime.Now.DayOfWeek == DayOfWeek.Wednesday ? 0 : -1),
                new Tuple<DayOfWeek, int>(DayOfWeek.Thursday, DateTime.Now.DayOfWeek == DayOfWeek.Thursday ? 0 : -1),
                new Tuple<DayOfWeek, int>(DayOfWeek.Friday, DateTime.Now.DayOfWeek == DayOfWeek.Friday ? 0 : -1),
                new Tuple<DayOfWeek, int>(DayOfWeek.Saturday, DateTime.Now.DayOfWeek == DayOfWeek.Saturday ? 0 : -1)
            };

            recurrenceRule.Frequency = Frequency.DAILY;

            var timerData = new CalendarPropertyData()
            {
                Value =
                [
                    new()
                    {
                        StartDate = DateTime.Now.AddHours(-1),
                        EndDate = DateTime.Now.AddHours(2),
                        RecurrenceRule = recurrenceRule.ToString()
                    }
                ]
            };

            paramDelay.Value = timerData;

            await Logic.Start();
            await Task.Delay(200);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.True((bool)values.First().Value.Value);
            await Logic.Stop();
        }



        [Fact]
        public async void TestTimerRule2()
        {
            FakeTimeProvider.SetDateTime(DateTime.Now);
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            var paramDelay = GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter);
            paramDelay.Value = new CalendarPropertyData()
            {
                Value =
                [
                    new CalendarPropertyDataEntry
                    {
                        StartDate = DateTime.Now.AddHours(-2),
                        EndDate = DateTime.Now.AddHours(2),
                        RecurrenceRule = "FREQ=DAILY"
                    }
                ]
            };
            await Logic.Start();
            await Task.Delay(200);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            //Assert.Equal(true, values.First().Value.Value);

            await Task.Delay(200);

            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

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

            var paramDelay = GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter);
            paramDelay.Value = new CalendarPropertyData()
            {
                Value =
                [
                    new()
                    {
                        StartDate = DateTime.Now.AddHours(-2),
                        EndDate = DateTime.Now.AddHours(2),
                        RecurrenceRule = "FREQ=DAILY"
                    },

                    new()
                    {
                        StartDate = DateTime.Now.AddHours(-8),
                        EndDate = DateTime.Now.AddHours(-6),
                        RecurrenceRule = "FREQ=DAILY"
                    }
                ]
            };
            await Logic.Start();

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.Equal(true, values.First().Value.Value);


            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.Equal(true, values.First().Value.Value);
            await Logic.Stop();
        }

        [Theory]
        [MemberData(nameof(TestTimerRuleWithDifferentNowData))]

        public async void TestTimerRule_WithDifferentNow(DateTime now, bool result)
        {
            FakeTimeProvider.SetDateTime(now);

            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();
            var paramDelay = GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter);
            paramDelay.ValueString =
                "{\"Value\":[{\"Text\":\"Morgens\",\"StartDate\":\"2023-12-14T05:00:00.000Z\",\"EndDate\":\"2023-12-14T07:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false,\"TrackingState\":1},{\"Text\":\"Abends\",\"StartDate\":\"2023-12-03T15:00:00.000Z\",\"EndDate\":\"2023-12-03T23:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false,\"TrackingState\":1},{\"Text\":\"Mittags\",\"StartDate\":\"2023-12-01T11:00:00.000Z\",\"EndDate\":\"2023-12-01T12:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false,\"TrackingState\":1}],\"TrackingState\":1}";

            await Logic.Start();
            await Task.Delay(200);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(result, values.First().Value.Value);


            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(result, values.First().Value.Value);
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

                new object[]
                {
                    new DateTime(new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day),
                        new TimeOnly(20, 12), DateTimeKind.Utc),
                    true
                },

                new object[] { DateTime.MaxValue, false },
                new object[] { DateTime.MinValue, false },
            };

        [Theory]
        [MemberData(nameof(TestTimerRuleWithDifferentNowData2))]
        [MemberData(nameof(TestTimerRuleWithDifferentNowData2_1))]

        public async void TestTimerRule_WithDifferentNow2(DateTime now, bool result, TimeZoneInfo timeZone)
        {
            FakeTimeProvider.SetDateTime(now);
            FakeTimeProvider.SetTimeZone(timeZone);

            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            var paramDelay = GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter);
            paramDelay.ValueString =
                "{\"Value\":[{\"Text\":\"Morgens\",\"StartDate\":\"2023-12-13T23:00:00.000Z\",\"EndDate\":\"2023-12-14T06:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false,\"TrackingState\":1},{\"Text\":\"Abends\",\"StartDate\":\"2023-12-03T15:00:00.000Z\",\"EndDate\":\"2023-12-03T23:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false,\"TrackingState\":1},{\"Text\":\"Mittags\",\"StartDate\":\"2023-12-01T11:00:00.000Z\",\"EndDate\":\"2023-12-01T12:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false,\"TrackingState\":1}],\"TrackingState\":1}";

            await Logic.Start();
            await Task.Delay(200);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(result, values.First().Value.Value);


            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(result, values.First().Value.Value);
            await Logic.Stop();
        }

        public static IEnumerable<object[]> TestTimerRuleWithDifferentNowData2 =>

            new List<object[]>
            {
                new object[]
                {
                    new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(1, 0), DateTimeKind.Utc), true, TimeZoneInfo.Utc
                },
                new object[]
                {
                    new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(2, 0), DateTimeKind.Utc), true, TimeZoneInfo.Utc
                },
                new object[]
                {
                    new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(8, 0), DateTimeKind.Utc), false,
                    TimeZoneInfo.Utc
                },
                new object[]
                {
                    new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(11, 30), DateTimeKind.Utc), true,
                    TimeZoneInfo.Utc
                },
                new object[]
                {
                    new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(18, 0), DateTimeKind.Utc), true,
                    TimeZoneInfo.Utc
                },


                new object[]
                {
                    new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(11, 0), DateTimeKind.Utc), true,
                    TimeZoneInfo.Utc
                },
                new object[]
                {
                    new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(14, 30), DateTimeKind.Utc), false,
                    TimeZoneInfo.Utc
                },
                new object[]
                {
                    new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(16, 28), DateTimeKind.Utc), true,
                    TimeZoneInfo.Utc
                },

                new object[]
                {
                    new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(14, 28), DateTimeKind.Utc), false,
                    TimeZoneInfo.Utc
                },

                new object[]
                {
                    new DateTime(new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day),
                        new TimeOnly(19, 12), DateTimeKind.Utc),
                    true, TimeZoneInfo.Utc
                },

            };

        public static IEnumerable<object[]> TestTimerRuleWithDifferentNowData2_1 =>

            new List<object[]>
            {
                new object[]
                {
                    new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(1, 0), DateTimeKind.Utc), true,
                    TimeZoneInfo.Local
                },
                new object[]
                {
                    new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(2, 0), DateTimeKind.Utc), true,
                    TimeZoneInfo.Local
                },
                new object[]
                {
                    new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(8, 0), DateTimeKind.Utc), false,
                    TimeZoneInfo.Local
                },
                new object[]
                {
                    new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(11, 30), DateTimeKind.Utc), true,
                    TimeZoneInfo.Local
                },
                new object[]
                {
                    new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(18, 0), DateTimeKind.Utc), true,
                    TimeZoneInfo.Local
                },


                new object[]
                {
                    new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(11, 0), DateTimeKind.Utc), true,
                    TimeZoneInfo.Local
                },
                new object[]
                {
                    new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(14, 30), DateTimeKind.Utc), false,
                    TimeZoneInfo.Local
                },
                new object[]
                {
                    new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(16, 28), DateTimeKind.Utc), true,
                    TimeZoneInfo.Local
                },

                new object[]
                {
                    new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(14, 28), DateTimeKind.Utc), false,
                    TimeZoneInfo.Local
                },

                new object[]
                {
                    new DateTime(new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day),
                        new TimeOnly(19, 12), DateTimeKind.Utc),
                    true, TimeZoneInfo.Local
                },

            };



        [Theory]
        [MemberData(nameof(TestTimerRuleWithDifferentNowData3))]

        public async void TestTimerRule_WithDifferentNow3(DateTime now)
        {
            FakeTimeProvider.SetDateTime(now);

            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();
            var paramDelay = GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter);
            paramDelay.ValueString =
                "{\"Value\":[{\"Text\":\"Morgens\",\"StartDate\":\"2224-12-14T05:00:00.000Z\",\"EndDate\":\"2224-12-14T07:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false,\"TrackingState\":1},{\"Text\":\"Abends\",\"StartDate\":\"2224-12-03T15:00:00.000Z\",\"EndDate\":\"2224-12-03T23:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false,\"TrackingState\":1},{\"Text\":\"Mittags\",\"StartDate\":\"2224-12-01T11:00:00.000Z\",\"EndDate\":\"2224-12-01T12:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false,\"TrackingState\":1}],\"TrackingState\":1}";

            await Logic.Start();
            await Task.Delay(200);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.False((bool)values.First().Value.Value);


            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.False((bool)values.First().Value.Value);
            await Logic.Stop();
        }

        public static IEnumerable<object[]> TestTimerRuleWithDifferentNowData3 =>

            new List<object[]>
            {
                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(2, 0), DateTimeKind.Local) },
                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(5, 0), DateTimeKind.Local) },
                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(8, 0), DateTimeKind.Local) },
                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(12, 30), DateTimeKind.Local) },
                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(18, 0), DateTimeKind.Local) },


                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(11, 0), DateTimeKind.Local) },
                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(14, 30), DateTimeKind.Local) },
                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(16, 28), DateTimeKind.Local) },

                new object[] { new DateTime(new DateOnly(2024, 1, 1), new TimeOnly(15, 28), DateTimeKind.Local) },

                new object[]
                {
                    new DateTime(new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day),
                        new TimeOnly(21, 12), DateTimeKind.Local)
                },
            };



        [Fact]
        public async void TestTimerRule_Every10Minutes()
        {
            var now = DateTime.Now;
            FakeTimeProvider.SetDateTime(new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0));
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            var paramDelay = GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter);
            paramDelay.ValueString =
                "{\"Value\":[{\"Text\":\"Every10Minutes\",\"StartDate\":\"2024-02-09T23:00:00.000Z\",\"EndDate\":\"2024-02-09T23:10:00.000Z\",\"RecurrenceRule\":\"FREQ=HOURLY\",\"AllDay\":false,\"TrackingState\":1}],\"TrackingState\":1}";
            await Logic.Start();
            await Task.Delay(200);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            //Assert.Equal(true, values.First().Value.Value);

            await Task.Delay(200);

            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.Equal(true, values.First().Value.Value);
            await Logic.Stop();
        }


        [Fact]
        public async void TestTimerRule_Every10Minutes2()
        {
            var now = DateTime.Now;
            FakeTimeProvider.SetDateTime(new DateTime(now.Year, now.Month, now.Day, now.Hour - 1, 59, 59));
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            var paramDelay = GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter);
            paramDelay.ValueString =
                "{\"Value\":[{\"Text\":\"Every10Minutes\",\"StartDate\":\"2024-02-09T23:00:00.000Z\",\"EndDate\":\"2024-02-09T23:10:00.000Z\",\"RecurrenceRule\":\"FREQ=HOURLY\",\"AllDay\":false,\"TrackingState\":1}],\"TrackingState\":1}";
            await Logic.Start();
            FakeTimeProvider.SetDateTime(new DateTime(now.Year, now.Month, now.Day, now.Hour, 00, 00));
            await Task.Delay(1200);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            //Assert.Equal(true, values.First().Value.Value);

            await Task.Delay(200);

            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.Equal(true, values.First().Value.Value);
            await Logic.Stop();

        }

        [Fact]
        public async void TestTimerRule_Every10Minutes3()
        {
            var now = DateTime.Now;
            FakeTimeProvider.SetDateTime(new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0));
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            var paramDelay = GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter);
            paramDelay.ValueString =
                "{\"Value\":[{\"Text\":\"Every10Minutes\",\"StartDate\":\"2024-02-09T23:00:00.000Z\",\"EndDate\":\"2024-02-09T23:10:00.000Z\",\"RecurrenceRule\":\"FREQ=HOURLY\",\"AllDay\":false,\"TrackingState\":1}],\"TrackingState\":1}";
            await Logic.Start();
            FakeTimeProvider.SetDateTime(new DateTime(now.Year, now.Month, now.Day, now.Hour, 01, 00));
            await Task.Delay(1200);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            //Assert.Equal(true, values.First().Value.Value);

            await Task.Delay(200);

            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.Equal(true, values.First().Value.Value);
            await Logic.Stop();
        }

        [Fact]
        public async void TestTimerRule_Every10Minutes4()
        {
            var now = DateTime.Now;
            FakeTimeProvider.SetDateTime(new DateTime(now.Year, now.Month, now.Day, 23, 10, 0));
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            var paramDelay = GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter);
            paramDelay.ValueString =
                "{\"Value\":[{\"Text\":\"Every10Minutes\",\"StartDate\":\"2024-02-09T23:10:00.000Z\",\"EndDate\":\"2024-02-09T23:30:00.000Z\",\"RecurrenceRule\":\"FREQ=HOURLY\",\"AllDay\":false,\"TrackingState\":1}],\"TrackingState\":1}";
            await Logic.Start();
            FakeTimeProvider.SetDateTime(new DateTime(now.Year, now.Month, now.Day, 0, 1, 00));
            await Task.Delay(1200);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            //Assert.Equal(true, values.First().Value.Value);

            await Task.Delay(200);

            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.Equal(true, values.First().Value.Value);
            await Logic.Stop();
        }

        [Fact]
        public async void TestTimerRule_Every10Minutes5()
        {
            var now = DateTime.Now;
            FakeTimeProvider.SetDateTime(new DateTime(now.Year, now.Month, now.Day, 23,59, 0));
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            var paramDelay = GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter);
            paramDelay.ValueString =
                "{\"Value\":[{\"Text\":\"Every10Minutes\",\"StartDate\":\"2024-02-09T23:10:00.000Z\",\"EndDate\":\"2024-02-09T23:30:00.000Z\",\"RecurrenceRule\":\"FREQ=HOURLY\",\"AllDay\":false,\"TrackingState\":1}],\"TrackingState\":1}";
            await Logic.Start();
            FakeTimeProvider.SetDateTime(new DateTime(now.Year, now.Month, now.Day+1, 0, 10, 1));
            await Task.Delay(1200);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            //Assert.Equal(true, values.First().Value.Value);

            await Task.Delay(200);

            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.Equal(false, values.First().Value.Value);
            await Logic.Stop();
        }

    }
}

