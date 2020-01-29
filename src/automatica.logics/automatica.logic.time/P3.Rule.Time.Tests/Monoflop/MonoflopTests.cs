using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.UnitTests.Base.Rules;
using Automatica.Core.UnitTests.Rules;
using P3.Rule.Time.Monoflop;
using Xunit;

namespace P3.Rule.Time.Tests.Monoflop
{
    public class MonoflopTests : RuleTest<MonoflopRuleFactory>
    {
        [Fact]
        public async void TestDelayedOnRule()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();

            Rule.ValueChanged(GetRuleInterfaceByTemplate(MonoflopRuleFactory.RuleTrigger), Dispatchable, true);

            await Task.Delay(2000);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            Assert.Equal(true, values.First().Value);

            await Task.Delay(3500);

            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            Assert.Equal(false, values.First().Value);
        }

        [Fact]
        public async void TestDelayedOnRule2()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();

            var paramDelay = GetRuleInterfaceByTemplate(MonoflopRuleFactory.RuleParamDelay);
            paramDelay.Value = 1;

            Rule.ValueChanged(GetRuleInterfaceByTemplate(MonoflopRuleFactory.RuleTrigger), Dispatchable, true);

            
            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            Assert.Equal(true, values.First().Value);

            await Task.Delay(1500);

            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            Assert.Equal(false, values.First().Value);
        }
    }
}
