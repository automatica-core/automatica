using Automatica.Core.UnitTests.Base.Rules;
using P3.Rule.Logic.BaseOperations.ExclusiveOr;
using Xunit;

namespace P3.Rule.Logic.BaseOperations.Tests.ExclusiveOr
{
    public class ExclusiveOrTests : RuleTest<ExclusiveOrRuleFactory>
    {
        [Fact]
        public void TestRule()
        {
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(ExclusiveOrRuleFactory.RuleInput1), Dispatchable, 1)[0].Value == null);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(ExclusiveOrRuleFactory.RuleInput2), Dispatchable, 11)[0].ValueInteger == 10);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(ExclusiveOrRuleFactory.RuleInput1), Dispatchable, null)[0].ValueInteger == 10);

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(ExclusiveOrRuleFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == ExclusiveOrRuleFactory.RuleOutput);
        }
    }
}
