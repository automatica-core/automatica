using Automatica.Core.UnitTests.Base.Rules;
using Automatica.Core.UnitTests.Rules;
using Xunit;
using P3.Rule.Logic.BaseOperations.Not;


namespace P3.Rule.Logic.BaseOperations.Test.Not
{
    
    public class NotTests : RuleTest<NotRuleFactory>
    {
        [Fact]
        public void TestRule()
        {
            Assert.False(Rule.ValueChanged(GetRuleInterfaceByTemplate(NotRuleFactory.RuleInput1), Dispatchable, true)[0].ValueBoolean);
            Assert.False(Rule.ValueChanged(GetRuleInterfaceByTemplate(NotRuleFactory.RuleInput1), Dispatchable, null)[0].ValueBoolean);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(NotRuleFactory.RuleInput1), Dispatchable, false)[0].ValueBoolean);

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(NotRuleFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == NotRuleFactory.RuleOutput);
        }
    }
}
