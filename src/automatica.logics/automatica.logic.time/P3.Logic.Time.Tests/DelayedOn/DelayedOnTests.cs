using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Time.DelayedOn;
using Xunit;

namespace P3.Logic.Time.Tests.DelayedOn
{
    public class DelayedOnTests: LogicTest<DelayedOnLogicFactory>
    {
        [Fact]
        public async void TestDelayedOnRule()
        {
            FakeTimeProvider.SetDateTime(DateTime.UtcNow);
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();

            Logic.ValueChanged(GetLogicInterfaceByTemplate(DelayedOnLogicFactory.RuleTrigger), Dispatchable, true);

            await Task.Delay(8000);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            Assert.Equal(true, values.First().Value.Value);
        }

        [Fact]
        public async void TestDelayedOnRule2()
        {
            FakeTimeProvider.SetDateTime(DateTime.UtcNow);
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();

            var paramDelay = GetLogicInterfaceByTemplate(DelayedOnLogicFactory.RuleParamDelay);
            paramDelay.Value = 1;

            await Logic.Start();

            Logic.ValueChanged(GetLogicInterfaceByTemplate(DelayedOnLogicFactory.RuleTrigger), Dispatchable, true);

            await Task.Delay(3000);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            Assert.Equal(true, values.First().Value.Value);
        }
    }
}
