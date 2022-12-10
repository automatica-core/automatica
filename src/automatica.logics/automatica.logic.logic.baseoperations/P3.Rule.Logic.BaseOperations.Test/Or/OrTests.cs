using Automatica.Core.UnitTests.Base.Rules;
using P3.Rule.Logic.BaseOperations.Or;
using Xunit;

namespace P3.Rule.Logic.BaseOperations.Tests.Or
{
    
    public class OrTests : RuleTest<OrRuleFactory>
    {
        [Fact]
        public void TestRule()
        {
            Assert.Equal(true, Rule.ValueChanged(GetRuleInterfaceByTemplate(OrRuleFactory.RuleInput1), Dispatchable, 1)[0].Value);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(OrRuleFactory.RuleInput2), Dispatchable, 11)[0].ValueBoolean);


            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(OrRuleFactory.RuleInput1), Dispatchable, null)[0].ValueBoolean);

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(OrRuleFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == OrRuleFactory.RuleOutput);
        }
    }
}
