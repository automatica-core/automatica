using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Compare.BaseOperations.Equal;
using Xunit;

namespace P3.Logic.Compare.BaseOperations.Tests.Equal
{
    
    public class EqualTests: LogicTest<EqualLogicFactory>
    {
        [Fact]
        public void TestEqualRule()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(EqualLogicFactory.RuleInput1), Dispatchable, 1).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(EqualLogicFactory.RuleInput1), Dispatchable, 100).Count == 0);
            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(EqualLogicFactory.RuleInput2), Dispatchable, 2)[0].ValueBoolean);

            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(EqualLogicFactory.RuleInput2), Dispatchable, 200)[0].ValueBoolean);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(EqualLogicFactory.RuleInput1), Dispatchable, 200)[0].ValueBoolean);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(EqualLogicFactory.RuleInput1), Dispatchable, null)[0].ValueBoolean);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(EqualLogicFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == EqualLogicFactory.RuleOutput);
        }
    }
}
