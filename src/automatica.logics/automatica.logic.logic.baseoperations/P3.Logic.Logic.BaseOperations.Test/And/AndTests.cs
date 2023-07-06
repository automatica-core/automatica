using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Logic.BaseOperations.And;
using Xunit;

namespace P3.Logic.Logic.BaseOperations.Tests.And
{
    public class AndTests: LogicTest<AndLogicFactory>
    {
        [Fact]
        public void TestRule()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(AndLogicFactory.RuleInput1), Dispatchable, 1)[0].Value == null);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(AndLogicFactory.RuleInput2), Dispatchable, 11)[0].ValueInteger == 1);


            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(AndLogicFactory.RuleInput1), Dispatchable, null)[0].ValueInteger == 1);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(AndLogicFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == AndLogicFactory.RuleOutput);
        }
    }
}
