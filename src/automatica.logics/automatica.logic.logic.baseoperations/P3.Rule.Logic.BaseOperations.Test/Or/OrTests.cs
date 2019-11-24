using Automatica.Core.UnitTests.Base.Rules;
using Automatica.Core.UnitTests.Rules;
using Xunit;
using P3.Rule.Logic.BaseOperations.Or;

namespace P3.Rule.Logic.BaseOperations.Test.Or
{
    
    public class OrTests : RuleTest<OrRuleFactory>
    {
        [Fact]
        public void TestRule()
        {
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(OrRuleFactory.RuleInput1), Dispatchable, 1)[0].Value == null);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(OrRuleFactory.RuleInput2), Dispatchable, 11)[0].ValueInteger == 11);


            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(OrRuleFactory.RuleInput1), Dispatchable, null)[0].ValueInteger == 11);

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(OrRuleFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == OrRuleFactory.RuleOutput);
        }
    }
}
