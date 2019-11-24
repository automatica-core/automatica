using Automatica.Core.UnitTests.Base.Rules;
using Automatica.Core.UnitTests.Rules;
using Xunit;
using P3.Rule.Math.BasicOperations.Division;

namespace P3.Rule.Math.BasicOperations.Test.Division
{
    
    public class DivisionTests : RuleTest<DivisionRuleFactory>
    {
        [Fact]
        public void TestDivisionTestsRule()
        {
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(DivisionRuleFactory.RuleInput1), Dispatchable, 1)[0].ValueInteger == 1);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(DivisionRuleFactory.RuleInput1), Dispatchable, 100)[0].ValueInteger == 100);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(DivisionRuleFactory.RuleInput2), Dispatchable, 2)[0].ValueInteger == 50);


            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(DivisionRuleFactory.RuleInput1), Dispatchable, null)[0].ValueInteger == 50);

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(DivisionRuleFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == DivisionRuleFactory.RuleOutput);
        }
    }
}
