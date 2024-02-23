using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Time.DelayedOn;
using Xunit;

namespace P3.Logic.Time.Tests.DelayedOn
{
    public class DelayedOnTests : LogicTest<DelayedOnLogicFactory>
    {
        [Fact]
        public async void TestDelayedOnRule()
        {
            FakeTimeProvider.SetDateTime(DateTime.UtcNow);
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            await Logic.Start();

            var logic = (DelayedOnRule)Logic;
            logic.Delay = 0;
            Logic.ValueChanged(GetLogicInterfaceByTemplate(DelayedOnLogicFactory.RuleTrigger), Dispatchable, true);

            await Task.Delay(100);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.Equal(true, values.First().Value.Value);
        }

        [Fact]
        public async void TestDelayedOnRule2()
        {
            FakeTimeProvider.SetDateTime(DateTime.UtcNow);
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();

            await Logic.Start();

            var logic = (DelayedOnRule)Logic;
            logic.Delay = 0;

            var paramDelay = GetLogicInterfaceByTemplate(DelayedOnLogicFactory.RuleParamDelay);
            paramDelay.Value = 1;


            Logic.ValueChanged(GetLogicInterfaceByTemplate(DelayedOnLogicFactory.RuleTrigger), Dispatchable, true);

            await Task.Delay(100);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.Equal(true, values.First().Value.Value);
        }

        [Fact]
        public async void TestDelayedOnRule3()
        {
            FakeTimeProvider.SetDateTime(DateTime.UtcNow);
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();

            await Logic.Start();

            var logic = (DelayedOnRule)Logic;
            logic.Delay = 0;
            logic.TriggerOnlyIfTrue = true;

            Logic.ValueChanged(GetLogicInterfaceByTemplate(DelayedOnLogicFactory.RuleTrigger), Dispatchable, false);

            await Task.Delay(100);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Empty(values);
        }

        [Fact]
        public async void TestDelayedOnRule4()
        {
            FakeTimeProvider.SetDateTime(DateTime.UtcNow);
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();

            await Logic.Start();

            var logic = (DelayedOnRule)Logic;
            logic.TriggerOnlyIfTrue = true;
            logic.Delay = 0;


            Logic.ValueChanged(GetLogicInterfaceByTemplate(DelayedOnLogicFactory.RuleTrigger), Dispatchable, true);

            await Task.Delay(100);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.Equal(true, values.First().Value.Value);
        }
    }
}
