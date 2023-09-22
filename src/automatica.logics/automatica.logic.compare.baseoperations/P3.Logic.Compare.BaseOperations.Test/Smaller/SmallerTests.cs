using System;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Compare.BaseOperations.Smaller;
using Xunit;

namespace P3.Logic.Compare.BaseOperations.Tests.Smaller
{
    
    public class SmallerTests: LogicTest<SmallerLogicFactory>
    {
        [Fact]
        public void TestSmallerRule()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput1), Dispatchable, 1).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput1), Dispatchable, 100).Count == 0);
            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput2), Dispatchable, 2)[0].ValueBoolean);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput2), Dispatchable, 200)[0].ValueBoolean);


            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput1), Dispatchable, null)[0].ValueBoolean);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == SmallerLogicFactory.RuleOutput);
        }


        [Fact]
        public void TestSmallerRuleDateTime()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput1), Dispatchable, new DateTime(2023, 03, 31)).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput1), Dispatchable, new DateTime(2023, 03, 31)).Count == 0);
            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput2), Dispatchable, new DateTime(2022, 03, 31))[0].ValueBoolean);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput2), Dispatchable, new DateTime(2024, 03, 31))[0].ValueBoolean);


            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput1), Dispatchable, new DateTime(2023, 03, 31))[0].ValueBoolean);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput1), Dispatchable, new DateTime(2023, 03, 31))[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == SmallerLogicFactory.RuleOutput);
        }



        [Fact]
        public void TestSmallerRuleDateOnly()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput1), Dispatchable, new DateOnly(2023, 03, 31)).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput1), Dispatchable, new DateOnly(2023, 03, 31)).Count == 0);
            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput2), Dispatchable, new DateOnly(2022, 03, 31))[0].ValueBoolean);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput2), Dispatchable, new DateOnly(2024, 03, 31))[0].ValueBoolean);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput1), Dispatchable, new DateOnly(2023, 03, 31))[0].ValueBoolean);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput1), Dispatchable, new DateOnly(2023, 03, 31))[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == SmallerLogicFactory.RuleOutput);
        }
        [Fact]
        public void TestSmallerRuleTimeOnly()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput1), Dispatchable, new TimeOnly(23, 3, 31)).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput1), Dispatchable, new TimeOnly(23, 03, 31)).Count == 0);
            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput2), Dispatchable, new TimeOnly(22, 03, 31))[0].ValueBoolean);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput2), Dispatchable, new TimeOnly(23, 10, 31))[0].ValueBoolean);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput1), Dispatchable, new TimeOnly(23, 03, 31))[0].ValueBoolean);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput1), Dispatchable, new TimeOnly(23, 03, 31))[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == SmallerLogicFactory.RuleOutput);
        }
    }
}
