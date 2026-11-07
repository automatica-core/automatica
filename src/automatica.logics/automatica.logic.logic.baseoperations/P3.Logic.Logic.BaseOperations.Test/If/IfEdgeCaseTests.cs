using System;
using System.Collections.Generic;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Logic.BaseOperations.If;
using Xunit;

namespace P3.Logic.Logic.BaseOperations.Tests.If
{
    public class IfEdgeCaseTests : LogicTest<IfLogicFactory>
    {
        private IList<ILogicOutputChanged> Set1(object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(IfLogicFactory.RuleInput1), Dispatchable, value);
        private IList<ILogicOutputChanged> Set2(object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(IfLogicFactory.RuleInput2), Dispatchable, value);

        private void SetParams(int trueValue, int falseValue)
        {
            GetLogicInterfaceByTemplate(IfLogicFactory.RuleParamTrue).Value = trueValue;
            GetLogicInterfaceByTemplate(IfLogicFactory.RuleParamFalse).Value = falseValue;
        }

        [Fact]
        public void InputValueChanged_DefaultParams_TrueIsOneFalseIsZero()
        {
            Assert.Equal(0, Assert.Single(Set1(3)).ValueInteger);
            Assert.Equal(1, Assert.Single(Set2(3)).ValueInteger);
        }

        [Fact]
        public void InputValueChanged_OnlyOneInputKnown_EmitsFalseValue()
        {
            SetParams(10, 20);

            var single = Assert.Single(Set2(10));

            Assert.Equal(20, single.ValueInteger);
            Assert.Equal(IfLogicFactory.RuleOutput, single.Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate);
        }

        [Fact]
        public void InputValueChanged_NullOnlyInput_EmitsFalseValue()
        {
            SetParams(10, 20);

            Assert.Equal(20, Assert.Single(Set1(null)).ValueInteger);
        }

        [Theory]
        [InlineData(1, 1, true)]
        [InlineData(1, 2, false)]
        [InlineData(-5, -5, true)]
        [InlineData(0, 0, true)]
        [InlineData(int.MaxValue, int.MaxValue, true)]
        [InlineData(int.MinValue, int.MaxValue, false)]
        [InlineData(true, 1, true)]
        [InlineData(false, 0, true)]
        [InlineData(true, false, false)]
        [InlineData(true, 2, false)]
        [InlineData("5", 5, true)]
        [InlineData(" 7", 7, true)]
        [InlineData("5", "6", false)]
        [InlineData(2.0, 2, true)]         // "2" parses as int
        [InlineData(5L, 5, true)]
        [InlineData((short)3, (byte)3, true)]
        public void InputValueChanged_BothKnown_ComparesAsInteger(object i1, object i2, bool equal)
        {
            SetParams(100, 200);

            Set1(i1);
            var single = Assert.Single(Set2(i2));

            Assert.Equal(equal ? 100 : 200, single.ValueInteger);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("1.5")]
        [InlineData(2.5)]
        [InlineData("")]
        public void InputValueChanged_NotAnInteger_EmitsNothingAndDoesNotThrow(object value)
        {
            Set1(1);

            Assert.Empty(Set2(value));
        }

        [Fact]
        public void InputValueChanged_NullAfterBothKnown_KeepsLastValuesAndReEmits()
        {
            SetParams(100, 200);
            Set1(4);
            Set2(4);

            Assert.Equal(100, Assert.Single(Set1(null)).ValueInteger);
            Assert.Equal(100, Assert.Single(Set2(null)).ValueInteger);
        }

        [Fact]
        public void InputValueChanged_SameValueTwice_IsEmittedAgain()
        {
            Set1(4);
            Assert.Equal(1, Assert.Single(Set2(4)).ValueInteger);
            Assert.Equal(1, Assert.Single(Set2(4)).ValueInteger);
        }

        [Fact]
        public void InputValueChanged_ParamChangedAtRuntime_NextOutputUsesNewValue()
        {
            SetParams(100, 200);
            Set1(4);
            Assert.Equal(100, Set2(4)[0].ValueInteger);

            SetParams(300, 400);

            Assert.Equal(300, Set2(4)[0].ValueInteger);
            Assert.Equal(400, Set2(5)[0].ValueInteger);
        }

        [Fact]
        public void InputValueChanged_NegativeParams_AreEmitted()
        {
            SetParams(-1, -2);
            Set1(1);

            Assert.Equal(-1, Set2(1)[0].ValueInteger);
            Assert.Equal(-2, Set2(0)[0].ValueInteger);
        }

        [Fact]
        public void InputValueChanged_TrueFalseToggle_FollowsInputs()
        {
            Set1(1);
            Assert.Equal(1, Set2(1)[0].ValueInteger);
            Assert.Equal(0, Set1(2)[0].ValueInteger);
            Assert.Equal(1, Set2(2)[0].ValueInteger);
        }
    }
}
