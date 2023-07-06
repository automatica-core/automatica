using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Logic.BaseOperations.ExclusiveOr;
using Xunit;

namespace P3.Logic.Logic.BaseOperations.Tests.ExclusiveOr
{
    public class ExclusiveOrTests: LogicTest<ExclusiveOrLogicFactory>
    {
        [Fact]
        public void TestRule()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(ExclusiveOrLogicFactory.RuleInput1), Dispatchable, 1)[0].Value == null);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(ExclusiveOrLogicFactory.RuleInput2), Dispatchable, 11)[0].ValueInteger == 10);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(ExclusiveOrLogicFactory.RuleInput1), Dispatchable, null)[0].ValueInteger == 10);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(ExclusiveOrLogicFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == ExclusiveOrLogicFactory.RuleOutput);
        }
    }
}
