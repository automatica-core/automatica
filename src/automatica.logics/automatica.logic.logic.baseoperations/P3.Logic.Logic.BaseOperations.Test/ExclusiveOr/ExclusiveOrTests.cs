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
            // like Or: an unknown input counts as false
            Assert.Equal(true, Logic.ValueChanged(GetLogicInterfaceByTemplate(ExclusiveOrLogicFactory.RuleInput1), Dispatchable, 1)[0].Value);
            Assert.Equal(false, Logic.ValueChanged(GetLogicInterfaceByTemplate(ExclusiveOrLogicFactory.RuleInput2), Dispatchable, 11)[0].Value);
            Assert.Equal(false, Logic.ValueChanged(GetLogicInterfaceByTemplate(ExclusiveOrLogicFactory.RuleInput1), Dispatchable, (object)null)[0].Value);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(ExclusiveOrLogicFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == ExclusiveOrLogicFactory.RuleOutput);
        }

        [Fact]
        public void TestRule_NoInputKnown_NoOutput()
        {
            Assert.Empty(Logic.ValueChanged(GetLogicInterfaceByTemplate(ExclusiveOrLogicFactory.RuleInput1), Dispatchable, (object)null));
        }

        [Theory]
        [InlineData(1, 2, false)]  // bitwise 1 ^ 2 would be 3
        [InlineData(1, 0, true)]
        [InlineData(0, 0, false)]
        [InlineData("true", "false", true)]
        [InlineData("false", "false", false)]
        [InlineData(0.5, 0, true)]
        public void TestRule_BooleanSemantics(object i1, object i2, bool expected)
        {
            Logic.ValueChanged(GetLogicInterfaceByTemplate(ExclusiveOrLogicFactory.RuleInput1), Dispatchable, i1);
            var result = Logic.ValueChanged(GetLogicInterfaceByTemplate(ExclusiveOrLogicFactory.RuleInput2), Dispatchable, i2);

            Assert.Equal(expected, result[0].Value);
        }
    }
}
