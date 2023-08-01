using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Math.BasicOperations.Division;
using Xunit;

namespace P3.Logic.Math.BasicOperations.Tests.Division
{
    
    public class DivisionTests: LogicTest<DivisionLogicFactory>
    {
        [Fact]
        public void TestDivisionTestsRule()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(DivisionLogicFactory.RuleInput1), Dispatchable, 1)[0].ValueInteger == 1);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(DivisionLogicFactory.RuleInput1), Dispatchable, 100)[0].ValueInteger == 100);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(DivisionLogicFactory.RuleInput2), Dispatchable, 2)[0].ValueInteger == 50);


            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(DivisionLogicFactory.RuleInput1), Dispatchable, null)[0].ValueInteger == 50);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(DivisionLogicFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == DivisionLogicFactory.RuleOutput);
        }
    }
}
