using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Math.BasicOperations.Random;
using Xunit;

namespace P3.Logic.Math.BasicOperations.Tests.Random
{
    
    public class RandomTests: LogicTest<RandomLogicFactory>
    {
        [Fact]
        public void TestRandomRule()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputDisabled), Dispatchable, true)[0].ValueInteger == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputDisabled), Dispatchable, false)[0].ValueInteger == 0);
            var randomValue = Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputTrigger), Dispatchable, 1)[0];
            Assert.True(randomValue.ValueInteger >= 0 && randomValue.ValueInteger <= 100);

            var randomValue2 = Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputTrigger), Dispatchable, 1)[0];
            Assert.True(randomValue2.ValueInteger >= 0 && randomValue2.ValueInteger <= 100);


            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputTrigger), Dispatchable, true)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == RandomLogicFactory.RuleOutput);
        }
        [Fact]
        public void TestRandomRule2()
        {
            var min = GetLogicInterfaceByTemplate(RandomLogicFactory.RuleParamMin);
            var max = GetLogicInterfaceByTemplate(RandomLogicFactory.RuleParamMax);
            min.Value = 100;
            max.Value = 1000;

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputDisabled), Dispatchable, true)[0].ValueInteger == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputDisabled), Dispatchable, false)[0].ValueInteger == 0);
            var randomValue = Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputTrigger), Dispatchable, 1)[0];
            Assert.True(randomValue.ValueInteger >= 100 && randomValue.ValueInteger <= 1000);


            var randomValue2 = Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputTrigger), Dispatchable, 1)[0];
            Assert.True(randomValue2.ValueInteger >= 100 && randomValue2.ValueInteger <= 1000);


            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputTrigger), Dispatchable, true)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == RandomLogicFactory.RuleOutput);
        }
    }
}
