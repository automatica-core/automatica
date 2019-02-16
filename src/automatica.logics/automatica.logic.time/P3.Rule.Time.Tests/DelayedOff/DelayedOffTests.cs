using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.UnitTests.Rules;
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
            Context.Dispatcher.Clear();
            
            Rule.ValueChanged(GetRuleInterfaceByTemplate(DelayedOffRuleFactory.RuleTrigger), Dispatchable, true);

            await Task.Delay(8000);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            Assert.Equal(false, values.First().Value);
        }

        [Fact]
        public async void TestDelayedOffRule2()
        {
            Context.Dispatcher.Clear();

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
            Context.Dispatcher.Clear();

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
