using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Compare.BaseOperations.SmallerOrEqual;
using System;
using Xunit;

namespace P3.Logic.Compare.BaseOperations.Tests.SmallerOrEqual
{
    
    public class SmallerOrEqualTests: LogicTest<SmallerOrEqualLogicFactory>
    {
        [Fact]
        public void TestSmallerOrEqualRule()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput1), Dispatchable, 1).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput1), Dispatchable, 100).Count == 0);
            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput2), Dispatchable, 2)[0].ValueBoolean);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput2), Dispatchable, 200)[0].ValueBoolean);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput1), Dispatchable, null)[0].ValueBoolean);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput1), Dispatchable, 200)[0].ValueBoolean); 

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == SmallerOrEqualLogicFactory.RuleOutput);
        }
        [Fact]
        public void TestSmallerOrEqualRuleDateTime()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput1), Dispatchable, new DateTime(2023, 03, 31)).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput1), Dispatchable, new DateTime(2023, 03, 31)).Count == 0);
            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput2), Dispatchable, new DateTime(2022, 03, 31))[0].ValueBoolean);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput2), Dispatchable, new DateTime(2024, 03, 31))[0].ValueBoolean);


            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput1), Dispatchable, new DateTime(2023, 03, 31))[0].ValueBoolean);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput1), Dispatchable, null)[0].ValueBoolean);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput2), Dispatchable, new DateTime(2023, 03, 31))[0].ValueBoolean);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput1), Dispatchable, new DateTime(2023, 03, 31))[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == SmallerOrEqualLogicFactory.RuleOutput);
        }



        [Fact]
        public void TestSmallerOrEqualRuleDateOnly()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput1), Dispatchable, new DateOnly(2023, 03, 31)).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput1), Dispatchable, new DateOnly(2023, 03, 31)).Count == 0);
            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput2), Dispatchable, new DateOnly(2022, 03, 31))[0].ValueBoolean);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput2), Dispatchable, new DateOnly(2024, 03, 31))[0].ValueBoolean);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput1), Dispatchable, new DateOnly(2023, 03, 31))[0].ValueBoolean);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput1), Dispatchable, new DateOnly(2023, 03, 31))[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == SmallerOrEqualLogicFactory.RuleOutput);
        }
        [Fact]
        public void TestSmallerOrEqualsTimeOnly()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput1), Dispatchable, new TimeOnly(23, 3, 31)).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput1), Dispatchable, new TimeOnly(23, 03, 31)).Count == 0);
            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput2), Dispatchable, new TimeOnly(22, 03, 31))[0].ValueBoolean);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput2), Dispatchable, new TimeOnly(23, 10, 31))[0].ValueBoolean);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput1), Dispatchable, new TimeOnly(23, 03, 31))[0].ValueBoolean);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerOrEqualLogicFactory.RuleInput1), Dispatchable, new TimeOnly(23, 03, 31))[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == SmallerOrEqualLogicFactory.RuleOutput);
        }
    }
}
