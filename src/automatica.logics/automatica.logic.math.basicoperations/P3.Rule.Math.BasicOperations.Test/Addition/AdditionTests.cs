using Automatica.Core.UnitTests.Base.Rules;
using Automatica.Core.UnitTests.Rules;
using P3.Rule.Math.BasicOperations.Addition;
using Xunit;

namespace P3.Rule.Math.BasicOperations.Test.Addition
{
    public class AdditionTests : RuleTest<AdditionRuleFactory>
    {
        [Fact]
        public void TestAdditionRule()
        {
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(AdditionRuleFactory.RuleInput1), Dispatchable, 1)[0].ValueInteger == 1);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(AdditionRuleFactory.RuleInput2), Dispatchable, 10)[0].ValueInteger == 11);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(AdditionRuleFactory.RuleInput3), Dispatchable, 1)[0].ValueInteger == 12);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(AdditionRuleFactory.RuleInput4), Dispatchable, 10)[0].ValueInteger == 22);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(AdditionRuleFactory.RuleInput1), Dispatchable, 0)[0].ValueInteger == 21);


            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(AdditionRuleFactory.RuleInput1), Dispatchable, null)[0].ValueInteger == 21);

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(AdditionRuleFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == AdditionRuleFactory.RuleOutput);
        }
    }
}
