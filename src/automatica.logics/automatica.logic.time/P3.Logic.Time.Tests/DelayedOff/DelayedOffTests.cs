using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Time.DelayedOff;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace P3.Logic.Time.Tests.DelayedOff
{
    public class DelayedOffTests: LogicTest<DelayedOffLogicFactory>
    {
        [Fact]
        public async void TestDelayedOffRule()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();

            Logic.ValueChanged(GetLogicInterfaceByTemplate(DelayedOffLogicFactory.RuleTrigger), Dispatchable, true);

            await Task.Delay(8000);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            Assert.Equal(false, values.First().Value.Value);
        }

        [Fact]
        public async void TestDelayedOffRule2()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();

            var paramDelay = GetLogicInterfaceByTemplate(DelayedOffLogicFactory.RuleParamDelay);
            paramDelay.Value = 1;
            await Logic.Start();

            Logic.ValueChanged(GetLogicInterfaceByTemplate(DelayedOffLogicFactory.RuleTrigger), Dispatchable, true);

            await Task.Delay(3000);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            Assert.Equal(false, values.First().Value.Value);
        }


        [Fact]
        public async void TestDelayedOffRuleReset()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();

            var paramDelay = GetLogicInterfaceByTemplate(DelayedOffLogicFactory.RuleParamDelay);
            paramDelay.Value = 2;

            await Logic.Start();

            Logic.ValueChanged(GetLogicInterfaceByTemplate(DelayedOffLogicFactory.RuleTrigger), Dispatchable, true);

            await Task.Delay(1000);

            Logic.ValueChanged(GetLogicInterfaceByTemplate(DelayedOffLogicFactory.RuleReset), Dispatchable, true);

            await Task.Delay(1500);


            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(0, values.Count);
        }
    }
}
