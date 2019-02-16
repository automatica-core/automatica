using Automatica.Core.UnitTests.Rules;
using P3.Rule.Logic.BaseOperations.If;
using Xunit;

namespace P3.Rule.Logic.BaseOperations.Test.If
{
    public class IfTests : RuleTest<IfRuleFactory>
    {
        [Fact]
        public void TestRuleLogic()
        {
            var paramTrue = GetRuleInterfaceByTemplate(IfRuleFactory.RuleParamTrue);
            paramTrue.Value = 1;

            var paramFalse = GetRuleInterfaceByTemplate(IfRuleFactory.RuleParamFalse);
            paramFalse.Value = 0;

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(IfRuleFactory.RuleInput1), Dispatchable, 1)[0].ValueInteger == 0);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(IfRuleFactory.RuleInput2), Dispatchable, 11)[0].ValueInteger == 0);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(IfRuleFactory.RuleInput2), Dispatchable, 1)[0].ValueInteger == 1);
        }

        [Fact]
        public void TestRuleLogic2()
        {
            var paramTrue = GetRuleInterfaceByTemplate(IfRuleFactory.RuleParamTrue);
            paramTrue.Value = 10;

            var paramFalse = GetRuleInterfaceByTemplate(IfRuleFactory.RuleParamFalse);
            paramFalse.Value = 5;

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(IfRuleFactory.RuleInput1), Dispatchable, 1)[0].ValueInteger == 5);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(IfRuleFactory.RuleInput2), Dispatchable, 11)[0].ValueInteger == 5);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(IfRuleFactory.RuleInput2), Dispatchable, 1)[0].ValueInteger == 10);
        }

        [Fact]
        public void TestRuleLogic3()
        {
            var paramTrue = GetRuleInterfaceByTemplate(IfRuleFactory.RuleParamTrue);
            paramTrue.Value = 1;

            var paramFalse = GetRuleInterfaceByTemplate(IfRuleFactory.RuleParamFalse);
            paramFalse.Value = 0;

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(IfRuleFactory.RuleInput1), Dispatchable, true)[0].ValueInteger == 0);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(IfRuleFactory.RuleInput2), Dispatchable, false)[0].ValueInteger == 0);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(IfRuleFactory.RuleInput2), Dispatchable, true)[0].ValueInteger == 1);
        }

        [Fact]
        public void TestRuleLogic4()
        {
            var paramTrue = GetRuleInterfaceByTemplate(IfRuleFactory.RuleParamTrue);
            paramTrue.Value = 10;

            var paramFalse = GetRuleInterfaceByTemplate(IfRuleFactory.RuleParamFalse);
            paramFalse.Value = 5;

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(IfRuleFactory.RuleInput1), Dispatchable, true)[0].ValueInteger == 5);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(IfRuleFactory.RuleInput2), Dispatchable, false)[0].ValueInteger == 5);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(IfRuleFactory.RuleInput2), Dispatchable, true)[0].ValueInteger == 10);
        }
    }
}
