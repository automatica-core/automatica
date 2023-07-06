using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Compare.BaseOperations.Smaller;
using Xunit;

namespace P3.Logic.Compare.BaseOperations.Tests.Smaller
{
    
    public class SmallerTests: LogicTest<SmallerLogicFactory>
    {
        [Fact]
        public void TestBiggerRule()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput1), Dispatchable, 1).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput1), Dispatchable, 100).Count == 0);
            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput2), Dispatchable, 2)[0].ValueBoolean);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput2), Dispatchable, 200)[0].ValueBoolean);


            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput1), Dispatchable, null)[0].ValueBoolean);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SmallerLogicFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == SmallerLogicFactory.RuleOutput);
        }
    }
}
