using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
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
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            var paramDelay = GetLogicInterfaceByTemplate(TimerLogicFactory.RuleTimerParameter);
            paramDelay.Value = new TimerPropertyData()
            {
                StartTime = DateTime.Now.AddSeconds(2),
                StopTime = DateTime.Now.AddSeconds(4),
                EnabledDays = new List<DayOfWeek>()
                {
                    DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday,
                    DayOfWeek.Saturday, DayOfWeek.Sunday
                }
            };
            await Logic.Start();
            await Task.Delay(2500);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            //Assert.Equal(true, values.First().Value.Value);

            await Task.Delay(2500);

            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            Assert.Equal(false, values.First().Value.Value);
            await Logic.Stop();
        }

        [Fact]
        public async void TestTimerRule2()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

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

            Assert.Equal(0, values.Count);
            await Logic.Stop();
        }




        [Fact]
        public async void TestTimerRule3()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Stop();

            var paramDelay = GetLogicInterfaceByTemplate(TimerLogicFactory.RuleTimerParameter);
            var timerData = "{\"StartTime\":\"" + DateTime.Now.AddSeconds(1).ToString(CultureInfo.InvariantCulture) + "\",\"StopTime\":\"" + DateTime.Now.AddSeconds(2).ToString(CultureInfo.InvariantCulture) + "\",\"EnabledDays\":[1,2,3,4,5,6,0],\"TrackingState\":1}";
            
            paramDelay.Value = timerData;

            await Logic.Start();
            await Task.Delay(1500);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            await Logic.Stop();
        }
    }
}

