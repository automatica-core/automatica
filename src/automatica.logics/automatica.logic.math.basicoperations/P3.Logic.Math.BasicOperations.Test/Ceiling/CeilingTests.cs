using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Math.BasicOperations.Ceiling;
using Xunit;

namespace P3.Logic.Math.BasicOperations.Tests.Ceiling
{
    
    public class CeilingTests: LogicTest<CeilingLogicFactory>
    {
        [Fact]
        public void TestFloorRule()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(CeilingLogicFactory.RuleInput1), Dispatchable, 1.1000)[0].ValueInteger == 2);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(CeilingLogicFactory.RuleInput1), Dispatchable, null)[0].ValueInteger == 2);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(CeilingLogicFactory.RuleInput1), Dispatchable, 1.6)[0].ValueInteger == 2);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(CeilingLogicFactory.RuleInput1), Dispatchable, 9.923112)[0].ValueInteger == 10);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(CeilingLogicFactory.RuleInput1), Dispatchable, -5.923112)[0].ValueInteger == -5);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(CeilingLogicFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == CeilingLogicFactory.RuleOutput);
        }
    }
}
