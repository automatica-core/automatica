using System.Collections.Generic;
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
            // the disabled input only changes the state, it does not emit
            Assert.Empty(Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputDisabled), Dispatchable, true));
            Assert.Empty(Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputDisabled), Dispatchable, false));
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

            Assert.Empty(Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputDisabled), Dispatchable, true));
            Assert.Empty(Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputDisabled), Dispatchable, false));
            var randomValue = Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputTrigger), Dispatchable, 1)[0];
            Assert.True(randomValue.ValueInteger >= 100 && randomValue.ValueInteger <= 1000);


            var randomValue2 = Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputTrigger), Dispatchable, 1)[0];
            Assert.True(randomValue2.ValueInteger >= 100 && randomValue2.ValueInteger <= 1000);


            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputTrigger), Dispatchable, true)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == RandomLogicFactory.RuleOutput);
        }

        [Fact]
        public void TestRandomRule_MaxIsInclusive()
        {
            GetLogicInterfaceByTemplate(RandomLogicFactory.RuleParamMin).Value = 0;
            GetLogicInterfaceByTemplate(RandomLogicFactory.RuleParamMax).Value = 1;

            var seen = new HashSet<double>();
            for (var i = 0; i < 200; i++)
            {
                var value = (double)Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputTrigger), Dispatchable, true)[0].Value;
                Assert.InRange(value, 0, 1);
                seen.Add(value);
            }

            Assert.Contains(0.0, seen);
            Assert.Contains(1.0, seen);
        }

        [Fact]
        public void TestRandomRule_MaxIntDoesNotOverflow()
        {
            GetLogicInterfaceByTemplate(RandomLogicFactory.RuleParamMin).Value = int.MaxValue - 1;
            GetLogicInterfaceByTemplate(RandomLogicFactory.RuleParamMax).Value = int.MaxValue;

            var value = (double)Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputTrigger), Dispatchable, true)[0].Value;
            Assert.InRange(value, int.MaxValue - 1, int.MaxValue);
        }

        [Fact]
        public void TestRandomRule_OnlyTruthyTriggerGenerates()
        {
            Assert.Empty(Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputTrigger), Dispatchable, false));
            Assert.Empty(Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputTrigger), Dispatchable, 0));

            Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputDisabled), Dispatchable, true);
            Assert.Empty(Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputTrigger), Dispatchable, true));
        }
    }
}
