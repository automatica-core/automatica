using Automatica.Core.UnitTests.Base.Rules;
using Automatica.Core.UnitTests.Rules;
using P3.Rule.Compare.BaseOperations.Bigger;
using Xunit;

namespace P3.Rule.Compare.BaseOperations.Test.Bigger
{
    
    public class BiggerTests : RuleTest<BiggerRuleFactory>
    {
        [Fact]
        public void TestBiggerRule()
        {
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(BiggerRuleFactory.RuleInput1), Dispatchable, 1).Count == 0);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(BiggerRuleFactory.RuleInput1), Dispatchable, 100).Count == 0);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(BiggerRuleFactory.RuleInput2), Dispatchable, 2)[0].ValueBoolean);

            Assert.False(Rule.ValueChanged(GetRuleInterfaceByTemplate(BiggerRuleFactory.RuleInput2), Dispatchable, 200)[0].ValueBoolean);


            Assert.False(Rule.ValueChanged(GetRuleInterfaceByTemplate(BiggerRuleFactory.RuleInput1), Dispatchable, null)[0].ValueBoolean);

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(BiggerRuleFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == BiggerRuleFactory.RuleOutput);
        }
    }
}
