using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Compare.BaseOperations.BiggerOrEqual;
using Xunit;

namespace P3.Logic.Compare.BaseOperations.Tests.BiggerOrEqual
{
    
    public class BiggerOrEqualTests: LogicTest<BiggerOrEqualLogicFactory>
    {
        [Fact]
        public void TestBiggerRule()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerOrEqualLogicFactory.RuleInput1), Dispatchable, 1).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerOrEqualLogicFactory.RuleInput1), Dispatchable, 100).Count == 0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerOrEqualLogicFactory.RuleInput2), Dispatchable, 2)[0].ValueBoolean);

            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerOrEqualLogicFactory.RuleInput2), Dispatchable, 200)[0].ValueBoolean);
            Assert.False(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerOrEqualLogicFactory.RuleInput1), Dispatchable, null)[0].ValueBoolean);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerOrEqualLogicFactory.RuleInput1), Dispatchable, 200)[0].ValueBoolean); 

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(BiggerOrEqualLogicFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == BiggerOrEqualLogicFactory.RuleOutput);
        }
    }
}
