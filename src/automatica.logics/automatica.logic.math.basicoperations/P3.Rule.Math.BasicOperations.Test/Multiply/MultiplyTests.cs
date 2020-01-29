using Automatica.Core.UnitTests.Base.Rules;
using Automatica.Core.UnitTests.Rules;
using Xunit;
using P3.Rule.Math.BasicOperations.Multiply;

namespace P3.Rule.Math.BasicOperations.Test.Multiply
{
    
    public class MultiplyTests : RuleTest<MultiplyRuleFactory>
    {
        [Fact]
        public void TestMultplyTestsRule()
        {
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(MultiplyRuleFactory.RuleInput1), Dispatchable, 1)[0].ValueInteger == 1);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(MultiplyRuleFactory.RuleInput1), Dispatchable, 100)[0].ValueInteger == 100);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(MultiplyRuleFactory.RuleInput2), Dispatchable, 2)[0].ValueInteger == 200);


            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(MultiplyRuleFactory.RuleInput1), Dispatchable, null)[0].ValueInteger == 200);

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(MultiplyRuleFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == MultiplyRuleFactory.RuleOutput);
        }
    }
}
