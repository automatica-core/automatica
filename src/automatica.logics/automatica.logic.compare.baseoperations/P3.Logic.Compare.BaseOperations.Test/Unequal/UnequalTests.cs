using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Compare.BaseOperations.Unequal;
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

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput2), Dispatchable, 200)[0].ValueBoolean);
            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput1), Dispatchable, 200)[0].ValueBoolean);

            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput1), Dispatchable, null)[0].ValueBoolean);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(UnequalLogicFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == UnequalLogicFactory.RuleOutput);
        }
    }
}