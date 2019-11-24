using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Rules;
using Automatica.Core.UnitTests.Rules;
using P3.Rule.Time.Timer;
using Xunit;

namespace P3.Rule.Time.Tests.Timer
{
    public class TimerTests : RuleTest<TimerRuleFactory>
    {
        [Fact]
        public async void TestTimerRule()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Rule.Stop();

            var paramDelay = GetRuleInterfaceByTemplate(TimerRuleFactory.RuleTimerParameter);
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
            await Rule.Start();
            await Task.Delay(2500);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            Assert.Equal(true, values.First().Value);

            await Task.Delay(2500);

            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            Assert.Equal(false, values.First().Value);
            await Rule.Stop();
        }

        [Fact]
        public async void TestTimerRule2()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Rule.Stop();

            var paramDelay = GetRuleInterfaceByTemplate(TimerRuleFactory.RuleTimerParameter);
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

            await Rule.Start();
            await Task.Delay(1500);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(0, values.Count);
            await Rule.Stop();
        }
    }
}
