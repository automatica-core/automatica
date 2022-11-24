using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.UnitTests.Base.Rules;
using P3.Rule.Time.DelayedOff;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace P3.Rule.Time.Tests.DelayedOff
{
    public class DelayedOffTests : RuleTest<DelayedOffRuleFactory>
    {
        [Fact]
        public async void TestDelayedOffRule()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();

            Rule.ValueChanged(GetRuleInterfaceByTemplate(DelayedOffRuleFactory.RuleTrigger), Dispatchable, true);

            await Task.Delay(8000);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            Assert.Equal(false, values.First().Value);
        }

        [Fact]
        public async void TestDelayedOffRule2()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();

            var paramDelay = GetRuleInterfaceByTemplate(DelayedOffRuleFactory.RuleParamDelay);
            paramDelay.Value = 1;
            await Rule.Start();

            Rule.ValueChanged(GetRuleInterfaceByTemplate(DelayedOffRuleFactory.RuleTrigger), Dispatchable, true);

            await Task.Delay(3000);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            Assert.Equal(false, values.First().Value);
        }


        [Fact]
        public async void TestDelayedOffRuleReset()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();

            var paramDelay = GetRuleInterfaceByTemplate(DelayedOffRuleFactory.RuleParamDelay);
            paramDelay.Value = 2;

            await Rule.Start();

            Rule.ValueChanged(GetRuleInterfaceByTemplate(DelayedOffRuleFactory.RuleTrigger), Dispatchable, true);

            await Task.Delay(1000);

            Rule.ValueChanged(GetRuleInterfaceByTemplate(DelayedOffRuleFactory.RuleReset), Dispatchable, true);

            await Task.Delay(1500);


            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(0, values.Count);
        }
    }
}
