using Automatica.Core.UnitTests.Base.Rules;
using P3.Rule.Math.BasicOperations.Random;
using Xunit;

namespace P3.Rule.Math.BasicOperations.Tests.Random
{
    
    public class RandomTests : RuleTest<RandomRuleFactory>
    {
        [Fact]
        public void TestRandomRule()
        {
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(RandomRuleFactory.RuleInputDisabled), Dispatchable, true)[0].ValueInteger == 0);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(RandomRuleFactory.RuleInputDisabled), Dispatchable, false)[0].ValueInteger == 0);
            var randomValue = Rule.ValueChanged(GetRuleInterfaceByTemplate(RandomRuleFactory.RuleInputTrigger), Dispatchable, 1)[0];
            Assert.True(randomValue.ValueInteger >= 0 && randomValue.ValueInteger <= 100);

            var randomValue2 = Rule.ValueChanged(GetRuleInterfaceByTemplate(RandomRuleFactory.RuleInputTrigger), Dispatchable, 1)[0];
            Assert.True(randomValue2.ValueInteger >= 0 && randomValue2.ValueInteger <= 100);


            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(RandomRuleFactory.RuleInputTrigger), Dispatchable, true)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == RandomRuleFactory.RuleOutput);
        }
        [Fact]
        public void TestRandomRule2()
        {
            var min = GetRuleInterfaceByTemplate(RandomRuleFactory.RuleParamMin);
            var max = GetRuleInterfaceByTemplate(RandomRuleFactory.RuleParamMax);
            min.Value = 100;
            max.Value = 1000;

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(RandomRuleFactory.RuleInputDisabled), Dispatchable, true)[0].ValueInteger == 0);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(RandomRuleFactory.RuleInputDisabled), Dispatchable, false)[0].ValueInteger == 0);
            var randomValue = Rule.ValueChanged(GetRuleInterfaceByTemplate(RandomRuleFactory.RuleInputTrigger), Dispatchable, 1)[0];
            Assert.True(randomValue.ValueInteger >= 100 && randomValue.ValueInteger <= 1000);


            var randomValue2 = Rule.ValueChanged(GetRuleInterfaceByTemplate(RandomRuleFactory.RuleInputTrigger), Dispatchable, 1)[0];
            Assert.True(randomValue2.ValueInteger >= 100 && randomValue2.ValueInteger <= 1000);


            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(RandomRuleFactory.RuleInputTrigger), Dispatchable, true)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == RandomRuleFactory.RuleOutput);
        }
    }
}
