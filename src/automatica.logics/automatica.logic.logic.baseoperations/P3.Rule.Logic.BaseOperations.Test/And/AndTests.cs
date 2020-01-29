using Automatica.Core.UnitTests.Base.Rules;
using Automatica.Core.UnitTests.Rules;
using P3.Rule.Logic.BaseOperations.And;
using Xunit;

namespace P3.Rule.Logic.BaseOperations.Test.And
{
    public class AndTests : RuleTest<AndRuleFactory>
    {
        [Fact]
        public void TestRule()
        {
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(AndRuleFactory.RuleInput1), Dispatchable, 1)[0].Value == null);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(AndRuleFactory.RuleInput2), Dispatchable, 11)[0].ValueInteger == 1);


            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(AndRuleFactory.RuleInput1), Dispatchable, null)[0].ValueInteger == 1);

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(AndRuleFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == AndRuleFactory.RuleOutput);
        }
    }
}
