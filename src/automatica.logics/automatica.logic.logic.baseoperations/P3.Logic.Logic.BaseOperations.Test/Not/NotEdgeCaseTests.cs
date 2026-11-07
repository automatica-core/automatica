using System.Collections.Generic;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Logic.BaseOperations.Not;
using Xunit;

namespace P3.Logic.Logic.BaseOperations.Tests.Not
{
    public class NotEdgeCaseTests : LogicTest<NotLogicFactory>
    {
        private IList<ILogicOutputChanged> Set(object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(NotLogicFactory.RuleInput1), Dispatchable, value);

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(0, true)]
        [InlineData(1, false)]
        [InlineData(5, false)]
        [InlineData(-1, false)]
        [InlineData(0.0, true)]
        [InlineData(0.5, false)]
        [InlineData("true", false)]
        [InlineData("False", true)]
        public void InputValueChanged_Value_EmitsInvertedBool(object input, bool expected)
        {
            var single = Assert.Single(Set(input));

            Assert.Equal(expected, single.Value);
            Assert.Equal(NotLogicFactory.RuleOutput, single.Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate);
        }

        [Fact]
        public void InputValueChanged_NullBeforeAnyValue_EmitsNullOutput()
        {
            var single = Assert.Single(Set(null));

            Assert.Null(single.Value);
        }

        [Fact]
        public void InputValueChanged_NullAfterValue_KeepsLastInvertedValue()
        {
            Set(true);

            Assert.Equal(false, Assert.Single(Set(null)).Value);
        }

        [Fact]
        public void InputValueChanged_SameValueTwice_IsEmittedAgain()
        {
            Assert.Equal(true, Assert.Single(Set(false)).Value);
            Assert.Equal(true, Assert.Single(Set(false)).Value);
        }

        [Fact]
        public void InputValueChanged_Toggle_FollowsInput()
        {
            Assert.Equal(false, Set(true)[0].Value);
            Assert.Equal(true, Set(false)[0].Value);
            Assert.Equal(false, Set(true)[0].Value);
        }

        [Fact(Skip = "BUG: NotRule uses Convert.ToBoolean instead of Helper.ConvertValueToBool - a numeric string like \"1\" throws (swallowed) and no output is emitted, And/Or/Xor treat it as true (NotRule.cs:31)")]
        public void InputValueChanged_NumericString_IsInterpretedLikeAndOr()
        {
            Assert.Equal(false, Assert.Single(Set("1")).Value);
            Assert.Equal(true, Assert.Single(Set("0")).Value);
        }

        [Fact(Skip = "BUG: NotRule uses Convert.ToBoolean - NaN counts as true (output false), And/Or/Xor treat NaN as false (NotRule.cs:31)")]
        public void InputValueChanged_NaN_IsInterpretedLikeAndOr()
        {
            Assert.Equal(true, Assert.Single(Set(double.NaN)).Value);
        }
    }
}
