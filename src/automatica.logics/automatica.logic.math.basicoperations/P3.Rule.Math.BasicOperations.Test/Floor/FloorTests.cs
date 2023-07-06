using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Math.BasicOperations.Floor;
using Xunit;

namespace P3.Logic.Math.BasicOperations.Tests.Floor
{
    
    public class FloorTests: LogicTest<FloorLogicFactory>
    {
        [Fact]
        public void TestFloorRule()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(FloorLogicFactory.RuleInput1), Dispatchable, 1.1000)[0].ValueInteger == 1);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(FloorLogicFactory.RuleInput1), Dispatchable, null)[0].ValueInteger == 1);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(FloorLogicFactory.RuleInput1), Dispatchable, 9.923112)[0].ValueInteger == 9);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(FloorLogicFactory.RuleInput1), Dispatchable, -5.923112)[0].ValueInteger == -6);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(FloorLogicFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == FloorLogicFactory.RuleOutput);
        }
    }
}
