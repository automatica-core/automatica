using Automatica.Core.UnitTests.Base.Rules;
using P3.Logic.StringOperations.Concat;
using Xunit;

namespace P3.Logic.StringOperations.Tests.Concat
{
    public class StringConcatTests : RuleTest<StringConcatFactory>
    {
        [Fact]
        public void TestConcatLogic()
        {
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(StringConcatFactory.RuleInput1), Dispatchable, "1")[0].Value.ToString() == "1");
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(StringConcatFactory.RuleInput2), Dispatchable, "10")[0].Value.ToString() == "110");
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(StringConcatFactory.RuleInput3), Dispatchable, "1")[0].Value.ToString() == "1101");
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(StringConcatFactory.RuleInput4), Dispatchable, "10")[0].Value.ToString() == "110110");
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(StringConcatFactory.RuleInput1), Dispatchable, "0")[0].Value.ToString() == "010110");


            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(StringConcatFactory.RuleInput1), Dispatchable, null)[0].Value.ToString() == "010110");

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(StringConcatFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == StringConcatFactory.RuleOutput);

        }
    }
}
