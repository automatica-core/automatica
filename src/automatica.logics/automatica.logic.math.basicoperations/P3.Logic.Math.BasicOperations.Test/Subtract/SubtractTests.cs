using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Math.BasicOperations.Subtract;
using Xunit;

namespace P3.Logic.Math.BasicOperations.Tests.Subtract
{
    
    public class SubtractTest: LogicTest<SubtractLogicFactory>
    {
        [Fact]
        public void TestSubtractRule()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SubtractLogicFactory.RuleInput1), Dispatchable, 1)[0].ValueInteger == 1);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SubtractLogicFactory.RuleInput2), Dispatchable, 10)[0].ValueInteger == -9);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SubtractLogicFactory.RuleInput1), Dispatchable, 0)[0].ValueInteger == -10);


            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SubtractLogicFactory.RuleInput1), Dispatchable, null)[0].ValueInteger == -10);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(SubtractLogicFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == SubtractLogicFactory.RuleOutput);
        }
    }
}
