using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Logic.BaseOperations.Or;
using Xunit;

namespace P3.Logic.Logic.BaseOperations.Tests.Or
{
    
    public class OrTests: LogicTest<OrLogicFactory>
    {
        [Fact]
        public void TestRule()
        {
            Assert.Equal(true, Logic.ValueChanged(GetLogicInterfaceByTemplate(OrLogicFactory.RuleInput1), Dispatchable, 1)[0].Value);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(OrLogicFactory.RuleInput2), Dispatchable, 11)[0].ValueBoolean);


            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(OrLogicFactory.RuleInput1), Dispatchable, null)[0].ValueBoolean);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(OrLogicFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == OrLogicFactory.RuleOutput);
        }
    }
}
