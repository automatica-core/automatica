using Automatica.Core.UnitTests.Base.Rules;
using P3.Rule.Math.BasicOperations.Subtract;
using Xunit;

namespace P3.Rule.Math.BasicOperations.Tests.Subtract
{
    
    public class SubtractTest : RuleTest<SubtractRuleFactory>
    {
        [Fact]
        public void TestSubtractRule()
        {
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(SubtractRuleFactory.RuleInput1), Dispatchable, 1)[0].ValueInteger == 1);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(SubtractRuleFactory.RuleInput2), Dispatchable, 10)[0].ValueInteger == -9);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(SubtractRuleFactory.RuleInput1), Dispatchable, 0)[0].ValueInteger == -10);


            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(SubtractRuleFactory.RuleInput1), Dispatchable, null)[0].ValueInteger == -10);

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(SubtractRuleFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == SubtractRuleFactory.RuleOutput);
        }
    }
}
