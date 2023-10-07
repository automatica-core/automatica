using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Compare.BaseOperations.Unequal;
using System;
using Xunit;

namespace P3.Logic.Compare.BaseOperations.Tests.Unequal
{
    
    public class UnequalTests: LogicTest<UnequalLogicFactory>
    {
        [Fact]
        public void TestUnequalRule()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput1), Dispatchable, 1).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput1), Dispatchable, 100).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput2), Dispatchable, 2)[0].ValueBoolean);

            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput2), Dispatchable, 100)[0].ValueBoolean);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput2), Dispatchable, 100);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput1), Dispatchable, 200)[0].ValueBoolean);


        }

        [Fact]
        public void TestUnequalRuleDateTime()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput1), Dispatchable, new DateTime(2023, 03, 31)).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput1), Dispatchable, new DateTime(2023, 03, 31)).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput2), Dispatchable, new DateTime(2022, 03, 31))[0].ValueBoolean);

            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput2), Dispatchable, new DateTime(2023, 03, 31))[0].ValueBoolean);


            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput1), Dispatchable, new DateTime(2024, 03, 31))[0].ValueBoolean);
    }


        [Fact]
        public void TestUnequalRuleDateOnly()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput1), Dispatchable, new DateOnly(2023, 03, 31)).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput1), Dispatchable, new DateOnly(2023, 03, 31)).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput2), Dispatchable, new DateOnly(2022, 03, 31))[0].ValueBoolean);

            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput2), Dispatchable, new DateOnly(2023, 03, 31))[0].ValueBoolean);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput1), Dispatchable, new DateOnly(2023, 05, 31))[0].ValueBoolean);

        }
        [Fact]
        public void TestUnequalRuleTimeOnly()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput1), Dispatchable, new TimeOnly(23, 3, 31)).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput1), Dispatchable, new TimeOnly(23, 03, 31)).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput2), Dispatchable, new TimeOnly(22, 03, 31))[0].ValueBoolean);

            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput2), Dispatchable, new TimeOnly(23, 3, 31))[0].ValueBoolean);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput2), Dispatchable, new TimeOnly(22, 03, 31))[0].ValueBoolean);
            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput2), Dispatchable, new TimeOnly(23, 03, 31))[0].ValueBoolean);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput2), Dispatchable, new TimeOnly(22, 10, 31))[0].ValueBoolean);
     }
    }
}
