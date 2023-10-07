using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Compare.BaseOperations.Bigger;
using System;
using Xunit;

namespace P3.Logic.Compare.BaseOperations.Tests.Bigger
{
    
    public class BiggerTests: LogicTest<BiggerLogicFactory>
    {
        [Fact]
        public void TestBiggerRule()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerLogicFactory.RuleInput1), Dispatchable, 1).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerLogicFactory.RuleInput1), Dispatchable, 100).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerLogicFactory.RuleInput2), Dispatchable, 2)[0].ValueBoolean);

            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerLogicFactory.RuleInput2), Dispatchable, 200)[0].ValueBoolean);


            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerLogicFactory.RuleInput1), Dispatchable, 1).Count == 0);
        }


        [Fact]
        public void TestBiggerRuleDateTime()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerLogicFactory.RuleInput1), Dispatchable, new DateTime(2023, 03, 31)).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerLogicFactory.RuleInput1), Dispatchable, new DateTime(2023, 03, 31)).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerLogicFactory.RuleInput2), Dispatchable, new DateTime(2022, 03, 31))[0].ValueBoolean);

            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerLogicFactory.RuleInput2), Dispatchable, new DateTime(2024, 03, 31))[0].ValueBoolean);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerLogicFactory.RuleInput1), Dispatchable, new DateTime(2025, 03, 31))[0].ValueBoolean);

        }

        [Fact]
        public void TestBiggerRuleDateOnly()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerLogicFactory.RuleInput1), Dispatchable, new DateOnly(2023, 03, 31)).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerLogicFactory.RuleInput1), Dispatchable, new DateOnly(2023, 03, 31)).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerLogicFactory.RuleInput2), Dispatchable, new DateOnly(2022, 03, 31))[0].ValueBoolean);

            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerLogicFactory.RuleInput2), Dispatchable, new DateOnly(2024, 03, 31))[0].ValueBoolean);

        }
        [Fact]
        public void TestBiggerRuleTimeOnly()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerLogicFactory.RuleInput1), Dispatchable, new TimeOnly(23, 3, 31)).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerLogicFactory.RuleInput1), Dispatchable, new TimeOnly(23, 03, 31)).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerLogicFactory.RuleInput2), Dispatchable, new TimeOnly(22, 03, 31))[0].ValueBoolean);

            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerLogicFactory.RuleInput2), Dispatchable, new TimeOnly(23, 10, 31))[0].ValueBoolean);

        }
    }
}
