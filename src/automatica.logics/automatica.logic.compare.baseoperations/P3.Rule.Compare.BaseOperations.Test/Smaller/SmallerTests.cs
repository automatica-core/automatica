using Automatica.Core.UnitTests.Base.Rules;
using Automatica.Core.UnitTests.Rules;
using P3.Rule.Compare.BaseOperations.Smaller;
using Xunit;

namespace P3.Rule.Compare.BaseOperations.Test.Smaller
{
    
    public class SmallerTests : RuleTest<SmallerRuleFactory>
    {
        [Fact]
        public void TestBiggerRule()
        {
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(SmallerRuleFactory.RuleInput1), Dispatchable, 1).Count == 0);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(SmallerRuleFactory.RuleInput1), Dispatchable, 100).Count == 0);
            Assert.False(Rule.ValueChanged(GetRuleInterfaceByTemplate(SmallerRuleFactory.RuleInput2), Dispatchable, 2)[0].ValueBoolean);

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(SmallerRuleFactory.RuleInput2), Dispatchable, 200)[0].ValueBoolean);


            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(SmallerRuleFactory.RuleInput1), Dispatchable, null)[0].ValueBoolean);

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(SmallerRuleFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == SmallerRuleFactory.RuleOutput);
        }
    }
}
