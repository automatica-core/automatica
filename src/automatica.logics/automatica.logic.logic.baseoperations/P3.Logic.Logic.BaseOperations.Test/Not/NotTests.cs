using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Logic.BaseOperations.Not;
using Xunit;

namespace P3.Logic.Logic.BaseOperations.Tests.Not
{
    
    public class NotTests: LogicTest<NotLogicFactory>
    {
        [Fact]
        public void TestRule()
        {
            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(NotLogicFactory.RuleInput1), Dispatchable, true)[0].ValueBoolean);
            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(NotLogicFactory.RuleInput1), Dispatchable, null)[0].ValueBoolean);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(NotLogicFactory.RuleInput1), Dispatchable, false)[0].ValueBoolean);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(NotLogicFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == NotLogicFactory.RuleOutput);
        }
    }
}
