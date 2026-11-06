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
            // like Or: an unknown input counts as false
            Assert.Equal(false, Logic.ValueChanged(GetLogicInterfaceByTemplate(AndLogicFactory.RuleInput1), Dispatchable, 1)[0].Value);
            Assert.Equal(true, Logic.ValueChanged(GetLogicInterfaceByTemplate(AndLogicFactory.RuleInput2), Dispatchable, 11)[0].Value);


            Assert.Equal(true, Logic.ValueChanged(GetLogicInterfaceByTemplate(AndLogicFactory.RuleInput1), Dispatchable, (object)null)[0].Value);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(AndLogicFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == AndLogicFactory.RuleOutput);
        }

        [Fact]
        public void TestRule_NoInputKnown_NoOutput()
        {
            Assert.Empty(Logic.ValueChanged(GetLogicInterfaceByTemplate(AndLogicFactory.RuleInput1), Dispatchable, (object)null));
        }

        [Theory]
        [InlineData(1, 2, true)]   // bitwise 1 & 2 would be 0
        [InlineData(1, 0, false)]
        [InlineData(0, 0, false)]
        [InlineData("true", "true", true)]
        [InlineData("true", "false", false)]
        [InlineData(0.5, 1, true)]
        [InlineData(true, 2, true)]
        public void TestRule_BooleanSemantics(object i1, object i2, bool expected)
        {
            Logic.ValueChanged(GetLogicInterfaceByTemplate(AndLogicFactory.RuleInput1), Dispatchable, i1);
            var result = Logic.ValueChanged(GetLogicInterfaceByTemplate(AndLogicFactory.RuleInput2), Dispatchable, i2);

            Assert.Equal(expected, result[0].Value);
        }
    }
}
