using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Compare.BaseOperations.BiggerOrEqual;
using System;
using Xunit;

namespace P3.Logic.Compare.BaseOperations.Tests.BiggerOrEqual
{
    
    public class BiggerOrEqualTests: LogicTest<BiggerOrEqualLogicFactory>
    {
        [Fact]
        public void TestBiggerOrEqualRule()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerOrEqualLogicFactory.RuleInput1), Dispatchable, 1).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerOrEqualLogicFactory.RuleInput1), Dispatchable, 100).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerOrEqualLogicFactory.RuleInput2), Dispatchable, 2)[0].ValueBoolean);

            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerOrEqualLogicFactory.RuleInput2), Dispatchable, 200)[0].ValueBoolean);
            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerOrEqualLogicFactory.RuleInput1), Dispatchable, null)[0].ValueBoolean);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerOrEqualLogicFactory.RuleInput1), Dispatchable, 200)[0].ValueBoolean); 

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerOrEqualLogicFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == BiggerOrEqualLogicFactory.RuleOutput);
        }

        [Fact]
        public void TestBiggerOrEqualRuleDateTime()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerOrEqualLogicFactory.RuleInput1), Dispatchable, new DateTime(2023, 03, 31)).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerOrEqualLogicFactory.RuleInput1), Dispatchable, new DateTime(2023, 03, 31)).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerOrEqualLogicFactory.RuleInput2), Dispatchable, new DateTime(2022, 03, 31))[0].ValueBoolean);

            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerOrEqualLogicFactory.RuleInput2), Dispatchable, new DateTime(2024, 03, 31))[0].ValueBoolean);


            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerOrEqualLogicFactory.RuleInput1), Dispatchable, new DateTime(2023, 03, 31))[0].ValueBoolean);
            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerOrEqualLogicFactory.RuleInput1), Dispatchable, null)[0].ValueBoolean);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerOrEqualLogicFactory.RuleInput2), Dispatchable, new DateTime(2023, 03, 31))[0].ValueBoolean);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerOrEqualLogicFactory.RuleInput1), Dispatchable, new DateTime(2023, 03, 31))[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == BiggerOrEqualLogicFactory.RuleOutput);
        }
    }
}
