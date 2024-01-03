using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.Calendar;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Time.AdvancedTimer;
using Xunit;

namespace P3.Logic.Time.Tests.AdvancedTimer
{
    public class AdvancedTimerTests : LogicTest<AdvancedTimerRuleFactory>
    {
        [Fact]
        public async void TestTimerRule()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            var paramDelay = GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter);
            paramDelay.Value = new CalendarPropertyData()
            {
                Value = new List<CalendarPropertyDataEntry>
                {
                    new CalendarPropertyDataEntry
                    {
                        AllDay = true
                    }
                }
            };
            await Logic.Start();
            await Task.Delay(200);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            //Assert.Equal(true, values.First().Value.Value);

            await Task.Delay(200);

            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            Assert.Equal(true, values.First().Value.Value);
            await Logic.Stop();
        }

        [Fact]
        public async void TestTimerRule_AllDaysButNotToday()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            var paramDelay = GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter);

            var recurrenceRule = new RFC2445Recur.Rule();
            recurrenceRule.ByDay = new[]
            {
                new Tuple<DayOfWeek, int>(DayOfWeek.Sunday, DateTime.Now.DayOfWeek == DayOfWeek.Sunday ? -1 : 0),
                new Tuple<DayOfWeek, int>(DayOfWeek.Monday,  DateTime.Now.DayOfWeek == DayOfWeek.Monday ? -1 : 0),
                new Tuple<DayOfWeek, int>(DayOfWeek.Tuesday,  DateTime.Now.DayOfWeek == DayOfWeek.Tuesday ?  -1 : 0),
                new Tuple<DayOfWeek, int>(DayOfWeek.Wednesday, DateTime.Now.DayOfWeek == DayOfWeek.Wednesday ?  -1 : 0),
                new Tuple<DayOfWeek, int>(DayOfWeek.Thursday,  DateTime.Now.DayOfWeek == DayOfWeek.Thursday ?  -1 : 0),
                new Tuple<DayOfWeek, int>(DayOfWeek.Friday,  DateTime.Now.DayOfWeek == DayOfWeek.Friday ? -1 : 0),
                new Tuple<DayOfWeek, int>(DayOfWeek.Saturday, DateTime.Now.DayOfWeek == DayOfWeek.Saturday ?  -1 : 0)
            };

            
            recurrenceRule.Frequency = Frequency.DAILY;

            var timerData = new CalendarPropertyData()
            {
                Value = new List<CalendarPropertyDataEntry>
                {
                    new CalendarPropertyDataEntry
                    {
                        StartDate = DateTime.Now.AddSeconds(1),
                        EndDate= DateTime.Now.AddSeconds(2),
                        RecurrenceRule = recurrenceRule.ToString()  
                    }
                }
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
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            var paramDelay = GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter);

            var recurrenceRule = new RFC2445Recur.Rule();
            recurrenceRule.ByDay = new[]
            {
                new Tuple<DayOfWeek, int>(DayOfWeek.Sunday, DateTime.Now.DayOfWeek == DayOfWeek.Sunday ? 0 : -1),
                new Tuple<DayOfWeek, int>(DayOfWeek.Monday,  DateTime.Now.DayOfWeek == DayOfWeek.Monday ? 0 : -1),
                new Tuple<DayOfWeek, int>(DayOfWeek.Tuesday,  DateTime.Now.DayOfWeek == DayOfWeek.Tuesday ?  0 : -1),
                new Tuple<DayOfWeek, int>(DayOfWeek.Wednesday, DateTime.Now.DayOfWeek == DayOfWeek.Wednesday ?  0 : -1),
                new Tuple<DayOfWeek, int>(DayOfWeek.Thursday,  DateTime.Now.DayOfWeek == DayOfWeek.Thursday ?  0 : -1),
                new Tuple<DayOfWeek, int>(DayOfWeek.Friday,  DateTime.Now.DayOfWeek == DayOfWeek.Friday ? 0 : -1),
                new Tuple<DayOfWeek, int>(DayOfWeek.Saturday, DateTime.Now.DayOfWeek == DayOfWeek.Saturday ?  0 : -1)
            };

            recurrenceRule.Frequency = Frequency.DAILY;

            var timerData = new CalendarPropertyData()
            {
                Value = new List<CalendarPropertyDataEntry>
                {
                    new CalendarPropertyDataEntry
                    {
                        StartDate = DateTime.Now.AddHours(-1),
                        EndDate= DateTime.Now.AddHours(2),
                        RecurrenceRule = recurrenceRule.ToString()
                    }
                }
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
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            var paramDelay = GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter);
            paramDelay.Value = new CalendarPropertyData()
            {
                Value = new List<CalendarPropertyDataEntry>
                {
                    new CalendarPropertyDataEntry
                    {
                        StartDate =DateTime.Now.AddHours(-2),
                        EndDate = DateTime.Now.AddHours(2),
                        RecurrenceRule = "FREQ=DAILY"
                    }
                }
            };
            await Logic.Start();
            await Task.Delay(200);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            //Assert.Equal(true, values.First().Value.Value);

            await Task.Delay(200);

            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            Assert.Equal(true, values.First().Value.Value);
            await Logic.Stop();
        }

        [Fact]
        public async void TestTimerRule3()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            var paramDelay = GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter);
            paramDelay.Value = new CalendarPropertyData()
            {
                Value = new List<CalendarPropertyDataEntry>
                {
                    new CalendarPropertyDataEntry
                    {
                        StartDate =DateTime.Now.AddHours(-2),
                        EndDate = DateTime.Now.AddHours(2),
                        RecurrenceRule = "FREQ=DAILY"
                    },
                    new CalendarPropertyDataEntry
                    {
                        StartDate =DateTime.Now.AddHours(-8),
                        EndDate = DateTime.Now.AddHours(-6),
                        RecurrenceRule = "FREQ=DAILY"
                    }
                }
            };
            await Logic.Start();

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            Assert.Equal(true, values.First().Value.Value);


            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            Assert.Equal(true, values.First().Value.Value);
            await Logic.Stop();
        }

        [Fact]
        public async void TestTimerRule4()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            var paramDelay = GetLogicInterfaceByTemplate(AdvancedTimerRuleFactory.RuleTimerParameter);
            paramDelay.ValueString =
                "{\"Value\":[{\"Text\":\"Morgens\",\"StartDate\":\"2023-12-14T05:00:00.000Z\",\"EndDate\":\"2023-12-14T07:00:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false,\"TrackingState\":1},{\"Text\":\"Abends\",\"StartDate\":\"2023-12-14T15:00:00.000Z\",\"EndDate\":\"2023-12-14T22:45:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false,\"TrackingState\":1},{\"Text\":\"Mittags\",\"StartDate\":\"2023-12-13T23:02:00.000Z\",\"EndDate\":\"2023-12-13T23:02:00.000Z\",\"RecurrenceRule\":\"FREQ=DAILY\",\"AllDay\":false,\"TrackingState\":1}],\"TrackingState\":1}";

            await Logic.Start();
            await Task.Delay(200);
            
            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.Equal(true, values.First().Value.Value);


            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.Equal(true, values.First().Value.Value);
            await Logic.Stop();
        }

    }
}

