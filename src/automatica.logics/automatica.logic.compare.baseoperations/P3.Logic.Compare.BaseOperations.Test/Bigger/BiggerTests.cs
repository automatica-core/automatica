using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Compare.BaseOperations.Bigger;
using Xunit;

namespace P3.Logic.Compare.BaseOperations.Tests.Bigger
{
    
    public class BiggerTests: LogicTest<BiggerLogicFactory>
    {
        [Fact]
        public void TestBiggerRule()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerLogicFactory.RuleInput1), Dispatchable, 1).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerLogicFactory.RuleInput1), Dispatchable, 100).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerLogicFactory.RuleInput2), Dispatchable, 2)[0].ValueBoolean);

            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerLogicFactory.RuleInput2), Dispatchable, 200)[0].ValueBoolean);


            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerLogicFactory.RuleInput1), Dispatchable, null)[0].ValueBoolean);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerLogicFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == BiggerLogicFactory.RuleOutput);
        }
    }
}
