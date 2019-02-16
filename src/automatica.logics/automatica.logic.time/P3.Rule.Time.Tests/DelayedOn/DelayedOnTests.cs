using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.UnitTests.Rules;
using P3.Rule.Time.DelayedOn;
using Xunit;

namespace P3.Rule.Time.Tests.DelayedOn
{
    public class DelayedOnTests : RuleTest<DelayedOnRuleFactory>
    {
        [Fact]
        public async void TestDelayedOnRule()
        {
            Context.Dispatcher.Clear();

            Rule.ValueChanged(GetRuleInterfaceByTemplate(DelayedOnRuleFactory.RuleTrigger), Dispatchable, true);

            await Task.Delay(8000);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            Assert.Equal(true, values.First().Value);
        }

        [Fact]
        public async void TestDelayedOnRule2()
        {
            Context.Dispatcher.Clear();

            var paramDelay = GetRuleInterfaceByTemplate(DelayedOnRuleFactory.RuleParamDelay);
            paramDelay.Value = 1;

            await Rule.Start();

            Rule.ValueChanged(GetRuleInterfaceByTemplate(DelayedOnRuleFactory.RuleTrigger), Dispatchable, true);

            await Task.Delay(3000);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            Assert.Equal(true, values.First().Value);
        }
    }
}
