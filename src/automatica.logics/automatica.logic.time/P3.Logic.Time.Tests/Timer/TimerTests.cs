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
            
            Assert.Empty(values);
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

