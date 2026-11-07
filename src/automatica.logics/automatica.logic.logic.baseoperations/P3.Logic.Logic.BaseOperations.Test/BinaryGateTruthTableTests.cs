using System;
using System.Collections.Generic;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Logic.BaseOperations.And;
using P3.Logic.Logic.BaseOperations.ExclusiveOr;
using P3.Logic.Logic.BaseOperations.Or;
using Xunit;

namespace P3.Logic.Logic.BaseOperations.Tests
{
    /// <summary>
    /// Shared behaviour of the two input gates (And, Or, Xor): truth table, unknown inputs, null handling and re-emits
    /// </summary>
    public abstract class BinaryGateTestBase<T> : LogicTest<T> where T : LogicFactory
    {
        protected abstract Guid Input1 { get; }
        protected abstract Guid Input2 { get; }
        protected abstract Guid Output { get; }

        /// <summary>the gate's result for (I1, I2)</summary>
        protected abstract bool Gate(bool i1, bool i2);

        protected IList<ILogicOutputChanged> Set1(object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(Input1), Dispatchable, value);
        protected IList<ILogicOutputChanged> Set2(object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(Input2), Dispatchable, value);

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void InputValueChanged_BothInputsKnown_EmitsTruthTableValueOnOutput(bool i1, bool i2)
        {
            Set1(i1);
            var result = Set2(i2);

            var single = Assert.Single(result);
            Assert.Equal(Gate(i1, i2), single.Value);
            Assert.Equal(Output, single.Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void InputValueChanged_InputOrderReversed_SameResult(bool i1, bool i2)
        {
            Set2(i2);
            var result = Set1(i1);

            Assert.Equal(Gate(i1, i2), Assert.Single(result).Value);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void InputValueChanged_OnlyInput1Known_UnknownInput2CountsAsFalse(bool i1)
        {
            Assert.Equal(Gate(i1, false), Assert.Single(Set1(i1)).Value);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void InputValueChanged_OnlyInput2Known_UnknownInput1CountsAsFalse(bool i2)
        {
            Assert.Equal(Gate(false, i2), Assert.Single(Set2(i2)).Value);
        }

        [Fact]
        public void InputValueChanged_NullBeforeAnyValue_EmitsNothing()
        {
            Assert.Empty(Set1(null));
            Assert.Empty(Set2(null));
        }

        [Fact]
        public void InputValueChanged_NullAfterValue_KeepsLastValueAndReEmits()
        {
            Set1(true);
            Set2(true);

            var result = Set1(null);

            Assert.Equal(Gate(true, true), Assert.Single(result).Value);
        }

        [Fact]
        public void InputValueChanged_SameValueTwice_IsEmittedAgain()
        {
            Set1(true);
            var first = Set2(false);
            var second = Set2(false);

            Assert.Equal(Assert.Single(first).Value, Assert.Single(second).Value);
        }

        [Fact]
        public void InputValueChanged_SequenceOfChanges_FollowsLatestInputs()
        {
            Assert.Equal(Gate(true, false), Set1(true)[0].Value);
            Assert.Equal(Gate(true, true), Set2(true)[0].Value);
            Assert.Equal(Gate(false, true), Set1(false)[0].Value);
            Assert.Equal(Gate(false, false), Set2(false)[0].Value);
        }

        [Fact]
        public void InputValueChanged_UnconvertibleValue_CountsAsFalseAndDoesNotThrow()
        {
            Set2(true);
            var result = Set1(new object());

            Assert.Equal(Gate(false, true), Assert.Single(result).Value);
        }

        [Fact]
        public void InputValueChanged_OutputValueIsBoxedBool()
        {
            Set1(1);
            var result = Set2(1);

            Assert.IsType<bool>(result[0].Value);
        }
    }

    public class AndTruthTableTests : BinaryGateTestBase<AndLogicFactory>
    {
        protected override Guid Input1 => AndLogicFactory.RuleInput1;
        protected override Guid Input2 => AndLogicFactory.RuleInput2;
        protected override Guid Output => AndLogicFactory.RuleOutput;

        protected override bool Gate(bool i1, bool i2) => (i1, i2) switch
        {
            (true, true) => true,
            _ => false
        };

        /// <summary>
        /// Helper.ConvertValueToBool, checked with I2 = true so the output equals the interpretation of I1
        /// </summary>
        public static IEnumerable<object[]> TruthinessData => new List<object[]>
        {
            new object[] { true, true },
            new object[] { false, false },
            new object[] { 1, true },
            new object[] { 0, false },
            new object[] { -1, true },
            new object[] { 2, true },          // logical, not bitwise
            new object[] { 0.5, true },
            new object[] { 0.0, false },
            new object[] { -0.0, false },
            new object[] { double.NaN, false },
            new object[] { double.PositiveInfinity, true },
            new object[] { double.NegativeInfinity, true },
            new object[] { 0.0001f, true },
            new object[] { 0m, false },
            new object[] { 2.5m, true },
            new object[] { 0L, false },
            new object[] { long.MaxValue, true },
            new object[] { (byte)0, false },
            new object[] { (byte)255, true },
            new object[] { (short)-1, true },
            new object[] { 7u, true },
            new object[] { "true", true },
            new object[] { "True", true },
            new object[] { "TRUE", true },
            new object[] { "false", false },
            new object[] { " true ", true },
            new object[] { "1", true },
            new object[] { "0", false },
            new object[] { "0.0", false },
            new object[] { "0.5", true },
            new object[] { "-3", true },
            new object[] { "1e3", true },
            new object[] { " 2 ", true },
            new object[] { "1,5", false },     // invariant culture: no thousands separator allowed, not a number
            new object[] { "abc", false },
            new object[] { "", false },
            new object[] { "yes", false },
            new object[] { "NaN", true },      // "NaN" parses (invariant) and NaN != 0
            new object[] { 'a', false },       // char.ToDouble throws, swallowed
            new object[] { new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), false },
            new object[] { new Guid("11111111-2222-3333-4444-555555555555"), false },  // not IConvertible
        };

        [Theory]
        [MemberData(nameof(TruthinessData))]
        public void InputValueChanged_Input1Coercion_FollowsHelperBooleanSemantics(object i1, bool expected)
        {
            Set2(true);
            var result = Set1(i1);

            Assert.Equal(expected, Assert.Single(result).Value);
        }

        [Fact]
        public void InputValueChanged_OneAndTwo_IsTrueNotBitwiseZero()
        {
            Set1(1);
            Assert.Equal(true, Set2(2)[0].Value);
        }

        [Fact]
        public void InputValueChanged_ElevenAndOne_IsTrue()
        {
            Set1(11);
            Assert.Equal(true, Set2(1)[0].Value);
        }
    }

    public class OrTruthTableTests : BinaryGateTestBase<OrLogicFactory>
    {
        protected override Guid Input1 => OrLogicFactory.RuleInput1;
        protected override Guid Input2 => OrLogicFactory.RuleInput2;
        protected override Guid Output => OrLogicFactory.RuleOutput;

        protected override bool Gate(bool i1, bool i2) => (i1, i2) switch
        {
            (false, false) => false,
            _ => true
        };

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData("1", true)]
        [InlineData("0", false)]
        [InlineData("abc", false)]
        [InlineData(3, true)]
        [InlineData(0, false)]
        [InlineData(-0.25, true)]
        [InlineData(double.NaN, false)]
        public void InputValueChanged_Input2Coercion_WithInput1False_FollowsHelperSemantics(object i2, bool expected)
        {
            Set1(false);
            Assert.Equal(expected, Assert.Single(Set2(i2)).Value);
        }

        [Fact]
        public void InputValueChanged_OneInputTrueAfterReset_OutputDropsWhenBothFalse()
        {
            Set1(true);
            Set2(false);
            Assert.Equal(false, Set1(false)[0].Value);
        }
    }

    public class ExclusiveOrTruthTableTests : BinaryGateTestBase<ExclusiveOrLogicFactory>
    {
        protected override Guid Input1 => ExclusiveOrLogicFactory.RuleInput1;
        protected override Guid Input2 => ExclusiveOrLogicFactory.RuleInput2;
        protected override Guid Output => ExclusiveOrLogicFactory.RuleOutput;

        protected override bool Gate(bool i1, bool i2) => (i1, i2) switch
        {
            (true, false) => true,
            (false, true) => true,
            _ => false
        };

        [Theory]
        [InlineData(1, 2, false)]       // bitwise 1 ^ 2 would be 3 (true)
        [InlineData(3, 3, false)]
        [InlineData(5, 0, true)]
        [InlineData("1", "0", true)]
        [InlineData("true", 1, false)]
        [InlineData(0.1, "0", true)]
        [InlineData(double.NaN, 1, true)]
        public void InputValueChanged_MixedTypes_UseLogicalNotBitwiseXor(object i1, object i2, bool expected)
        {
            Set1(i1);
            Assert.Equal(expected, Set2(i2)[0].Value);
        }
    }
}
