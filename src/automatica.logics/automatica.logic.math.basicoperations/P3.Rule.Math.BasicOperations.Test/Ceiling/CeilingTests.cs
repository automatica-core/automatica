using Automatica.Core.UnitTests.Rules;
using Xunit;
using P3.Rule.Math.BasicOperations.Ceiling;

namespace P3.Rule.Math.BasicOperations.Test.Ceiling
{
    
    public class CeilingTests : RuleTest<CeilingRuleFactory>
    {
        [Fact]
        public void TestFloorRule()
        {
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(CeilingRuleFactory.RuleInput1), Dispatchable, 1.1000)[0].ValueInteger == 2);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(CeilingRuleFactory.RuleInput1), Dispatchable, null)[0].ValueInteger == 2);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(CeilingRuleFactory.RuleInput1), Dispatchable, 1.6)[0].ValueInteger == 2);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(CeilingRuleFactory.RuleInput1), Dispatchable, 9.923112)[0].ValueInteger == 10);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(CeilingRuleFactory.RuleInput1), Dispatchable, -5.923112)[0].ValueInteger == -5);

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(CeilingRuleFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == CeilingRuleFactory.RuleOutput);
        }
    }
}
