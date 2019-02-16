using Automatica.Core.UnitTests.Rules;
using Xunit;
using P3.Rule.Math.BasicOperations.Substract;

namespace P3.Rule.Math.BasicOperations.Test.Substract
{
    
    public class SubstractTests : RuleTest<SubstractRuleFactory>
    {
        [Fact]
        public void TestSubstractRule()
        {
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(SubstractRuleFactory.RuleInput1), Dispatchable, 1)[0].ValueInteger == 1);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(SubstractRuleFactory.RuleInput2), Dispatchable, 10)[0].ValueInteger == -9);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(SubstractRuleFactory.RuleInput1), Dispatchable, 0)[0].ValueInteger == -10);


            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(SubstractRuleFactory.RuleInput1), Dispatchable, null)[0].ValueInteger == -10);

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(SubstractRuleFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == SubstractRuleFactory.RuleOutput);
        }
    }
}
