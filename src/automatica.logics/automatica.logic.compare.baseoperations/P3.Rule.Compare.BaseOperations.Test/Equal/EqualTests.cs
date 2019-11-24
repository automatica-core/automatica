using Automatica.Core.UnitTests.Base.Rules;
using Automatica.Core.UnitTests.Rules;
using P3.Rule.Compare.BaseOperations.Equal;
using Xunit;

namespace P3.Rule.Compare.BaseOperations.Test.Equal
{
    
    public class EqualTests : RuleTest<EqualRuleFactory>
    {
        [Fact]
        public void TestEqualRule()
        {
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(EqualRuleFactory.RuleInput1), Dispatchable, 1).Count == 0);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(EqualRuleFactory.RuleInput1), Dispatchable, 100).Count == 0);
            Assert.False(Rule.ValueChanged(GetRuleInterfaceByTemplate(EqualRuleFactory.RuleInput2), Dispatchable, 2)[0].ValueBoolean);

            Assert.False(Rule.ValueChanged(GetRuleInterfaceByTemplate(EqualRuleFactory.RuleInput2), Dispatchable, 200)[0].ValueBoolean);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(EqualRuleFactory.RuleInput1), Dispatchable, 200)[0].ValueBoolean);

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(EqualRuleFactory.RuleInput1), Dispatchable, null)[0].ValueBoolean);

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(EqualRuleFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == EqualRuleFactory.RuleOutput);
        }
    }
}
