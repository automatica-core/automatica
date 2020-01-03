using System.Linq;
using Automatica.Core.UnitTests.Base.Rules;
using P3.Rule.DigitalToAnalog.Trigger;
using Xunit;

namespace P3.Rule.DigitalToAnalog.Tests.Trigger
{
    public class TriggerTests : RuleTest<TriggerRuleFactory>
    {
        [Fact]
        public async void TestRule()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();

            Rule.ValueChanged(GetRuleInterfaceByTemplate(TriggerRuleFactory.RuleValueInput), Dispatchable, 10);
            var values = Rule.ValueChanged(GetRuleInterfaceByTemplate(TriggerRuleFactory.RuleInput), Dispatchable, true);

            Assert.Equal(1, values.Count);
            Assert.Equal(10, values.First().Value);
        }
    }
}
