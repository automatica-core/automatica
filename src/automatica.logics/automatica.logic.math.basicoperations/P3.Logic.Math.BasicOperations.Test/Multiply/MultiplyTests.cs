using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Math.BasicOperations.Multiply;
using Xunit;

namespace P3.Logic.Math.BasicOperations.Tests.Multiply
{
    
    public class MultiplyTests: LogicTest<MultiplyLogicFactory>
    {
        [Fact]
        public void TestMultiplyTestsRule()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(MultiplyLogicFactory.RuleInput1), Dispatchable, 1)[0].ValueInteger == 1);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(MultiplyLogicFactory.RuleInput1), Dispatchable, 100)[0].ValueInteger == 100);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(MultiplyLogicFactory.RuleInput2), Dispatchable, 2)[0].ValueInteger == 200);


            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(MultiplyLogicFactory.RuleInput1), Dispatchable, null)[0].ValueInteger == 200);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(MultiplyLogicFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == MultiplyLogicFactory.RuleOutput);
        }
    }
}
