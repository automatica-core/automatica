using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Logic.BaseOperations.If;
using Xunit;

namespace P3.Logic.Logic.BaseOperations.Tests.If
{
    public class IfTests: LogicTest<IfLogicFactory>
    {
        [Fact]
        public void TestRuleLogic()
        {
            var paramTrue = GetLogicInterfaceByTemplate(IfLogicFactory.RuleParamTrue);
            paramTrue.Value = 1;

            var paramFalse = GetLogicInterfaceByTemplate(IfLogicFactory.RuleParamFalse);
            paramFalse.Value = 0;

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(IfLogicFactory.RuleInput1), Dispatchable, 1)[0].ValueInteger == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(IfLogicFactory.RuleInput2), Dispatchable, 11)[0].ValueInteger == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(IfLogicFactory.RuleInput2), Dispatchable, 1)[0].ValueInteger == 1);
        }

        [Fact]
        public void TestRuleLogic2()
        {
            var paramTrue = GetLogicInterfaceByTemplate(IfLogicFactory.RuleParamTrue);
            paramTrue.Value = 10;

            var paramFalse = GetLogicInterfaceByTemplate(IfLogicFactory.RuleParamFalse);
            paramFalse.Value = 5;

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(IfLogicFactory.RuleInput1), Dispatchable, 1)[0].ValueInteger == 5);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(IfLogicFactory.RuleInput2), Dispatchable, 11)[0].ValueInteger == 5);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(IfLogicFactory.RuleInput2), Dispatchable, 1)[0].ValueInteger == 10);
        }

        [Fact]
        public void TestRuleLogic3()
        {
            var paramTrue = GetLogicInterfaceByTemplate(IfLogicFactory.RuleParamTrue);
            paramTrue.Value = 1;

            var paramFalse = GetLogicInterfaceByTemplate(IfLogicFactory.RuleParamFalse);
            paramFalse.Value = 0;

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(IfLogicFactory.RuleInput1), Dispatchable, true)[0].ValueInteger == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(IfLogicFactory.RuleInput2), Dispatchable, false)[0].ValueInteger == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(IfLogicFactory.RuleInput2), Dispatchable, true)[0].ValueInteger == 1);
        }

        [Fact]
        public void TestRuleLogic4()
        {
            var paramTrue = GetLogicInterfaceByTemplate(IfLogicFactory.RuleParamTrue);
            paramTrue.Value = 10;

            var paramFalse = GetLogicInterfaceByTemplate(IfLogicFactory.RuleParamFalse);
            paramFalse.Value = 5;

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(IfLogicFactory.RuleInput1), Dispatchable, true)[0].ValueInteger == 5);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(IfLogicFactory.RuleInput2), Dispatchable, false)[0].ValueInteger == 5);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(IfLogicFactory.RuleInput2), Dispatchable, true)[0].ValueInteger == 10);
        }

        [Fact]
        public void TestRuleLogic5()
        {
            var paramTrue = GetLogicInterfaceByTemplate(IfLogicFactory.RuleParamTrue);
            paramTrue.Value = 1;

            var paramFalse = GetLogicInterfaceByTemplate(IfLogicFactory.RuleParamFalse);
            paramFalse.Value = 0;

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(IfLogicFactory.RuleInput1), Dispatchable, true)[0].ValueInteger == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(IfLogicFactory.RuleInput2), Dispatchable, 1)[0].ValueInteger == 1);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(IfLogicFactory.RuleInput2), Dispatchable, 1)[0].ValueInteger == 1);
        }
    }
}
