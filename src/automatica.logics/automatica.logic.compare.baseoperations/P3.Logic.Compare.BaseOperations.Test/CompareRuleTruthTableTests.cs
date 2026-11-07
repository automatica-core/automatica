using System;
using System.Collections.Generic;
using System.Globalization;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Compare.BaseOperations.Bigger;
using P3.Logic.Compare.BaseOperations.BiggerOrEqual;
using P3.Logic.Compare.BaseOperations.Equal;
using P3.Logic.Compare.BaseOperations.Smaller;
using P3.Logic.Compare.BaseOperations.SmallerOrEqual;
using P3.Logic.Compare.BaseOperations.Unequal;
using Xunit;

namespace P3.Logic.Compare.BaseOperations.Tests
{
    /// <summary>
    /// How I1 relates to I2
    /// </summary>
    public enum Relation
    {
        Less,
        Equal,
        Greater,
        /// <summary>at least one side is NaN</summary>
        Unordered
    }

    internal sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;

        public CultureScope(string name)
        {
            CultureInfo.CurrentCulture = new CultureInfo(name);
            CultureInfo.CurrentUICulture = new CultureInfo(name);
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _culture;
            CultureInfo.CurrentUICulture = _uiCulture;
        }
    }

    /// <summary>
    /// Shared behaviour of all compare rules (BaseCompareRule): conversion of the inputs, the truth table and "emit only on change"
    /// </summary>
    public abstract class CompareRuleTestBase<T> : LogicTest<T> where T : LogicFactory
    {
        protected abstract Guid Input1 { get; }
        protected abstract Guid Input2 { get; }
        protected abstract Guid Output { get; }

        /// <summary>the expected output for the relation of I1 to I2</summary>
        protected abstract bool ExpectedFor(Relation relation);

        protected IList<ILogicOutputChanged> Set1(object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(Input1), Dispatchable, value);
        protected IList<ILogicOutputChanged> Set2(object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(Input2), Dispatchable, value);

        private void AssertRelation(object i1, object i2, Relation relation)
        {
            Set1(i1);
            var single = Assert.Single(Set2(i2));

            Assert.Equal(ExpectedFor(relation), single.Value);
            Assert.Equal(Output, single.Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate);
        }

        [Theory]
        [InlineData(1, 2, Relation.Less)]
        [InlineData(2, 1, Relation.Greater)]
        [InlineData(2, 2, Relation.Equal)]
        [InlineData(-1, -1, Relation.Equal)]
        [InlineData(-2, -1, Relation.Less)]
        [InlineData(0, 0, Relation.Equal)]
        [InlineData(1.5, 1.4999, Relation.Greater)]
        [InlineData(0.1, 0.1, Relation.Equal)]
        [InlineData(1e300, -1e300, Relation.Greater)]
        [InlineData(double.MaxValue, double.MaxValue, Relation.Equal)]
        [InlineData(double.PositiveInfinity, double.MaxValue, Relation.Greater)]
        [InlineData(double.NegativeInfinity, double.MinValue, Relation.Less)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Relation.Equal)]
        [InlineData(double.PositiveInfinity, double.NegativeInfinity, Relation.Greater)]
        [InlineData(double.NaN, 1.0, Relation.Unordered)]
        [InlineData(1.0, double.NaN, Relation.Unordered)]
        [InlineData(double.NaN, double.NaN, Relation.Unordered)]
        public void InputValueChanged_Numbers_EmitsTruthTableValue(double i1, double i2, Relation relation)
        {
            AssertRelation(i1, i2, relation);
        }

        [Fact]
        public void InputValueChanged_PositiveAndNegativeZero_AreEqual()
        {
            AssertRelation(0.0, -0.0, Relation.Equal);
        }

        [Theory]
        [InlineData(true, 1, Relation.Equal)]
        [InlineData(false, 0, Relation.Equal)]
        [InlineData(true, false, Relation.Greater)]
        [InlineData("2", 2, Relation.Equal)]
        [InlineData(" 3 ", 3, Relation.Equal)]
        [InlineData("-1", 0, Relation.Less)]
        [InlineData(5L, 4, Relation.Greater)]
        [InlineData((byte)1, (short)2, Relation.Less)]
        [InlineData(0.5f, 0.5, Relation.Equal)]
        [InlineData(7u, 7.0, Relation.Equal)]
        public void InputValueChanged_MixedTypes_AreConvertedToDouble(object i1, object i2, Relation relation)
        {
            AssertRelation(i1, i2, relation);
        }

        [Fact]
        public void InputValueChanged_Decimal_IsConvertedToDouble()
        {
            AssertRelation(2.5m, 2.5, Relation.Equal);
        }

        [Fact]
        public void InputValueChanged_StringWithDecimalComma_UsesCurrentCulture_German()
        {
            using (new CultureScope("de-DE"))
            {
                AssertRelation("1,5", 1.5, Relation.Equal);
            }
        }

        [Fact]
        public void InputValueChanged_StringWithComma_UsesCurrentCulture_EnglishThousandsSeparator()
        {
            using (new CultureScope("en-US"))
            {
                AssertRelation("1,5", 15, Relation.Equal);
            }
        }

        [Fact]
        public void InputValueChanged_StringWithDecimalPoint_UsesCurrentCulture_English()
        {
            using (new CultureScope("en-US"))
            {
                AssertRelation("1.5", 1.5, Relation.Equal);
            }
        }

        [Fact]
        public void InputValueChanged_UtcDateTime_IsComparedAsUnixSeconds()
        {
            AssertRelation(new DateTime(1970, 1, 1, 0, 0, 10, DateTimeKind.Utc), 10, Relation.Equal);
        }

        [Fact]
        public void InputValueChanged_UtcDateTime_SubSecondsAreTruncated()
        {
            AssertRelation(new DateTime(1970, 1, 1, 0, 0, 10, 900, DateTimeKind.Utc), 10, Relation.Equal);
        }

        [Fact]
        public void InputValueChanged_TwoUtcDateTimes_AreCompared()
        {
            AssertRelation(new DateTime(2024, 3, 31, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 3, 30, 23, 59, 59, DateTimeKind.Utc), Relation.Greater);
        }

        [Fact]
        public void InputValueChanged_DateOnly_IsComparedAsUnixSecondsOfUtcMidnight()
        {
            AssertRelation(new DateOnly(1970, 1, 2), 86400, Relation.Equal);
        }

        [Fact]
        public void InputValueChanged_DateOnlyAndUtcDateTimeAtMidnight_AreEqual()
        {
            AssertRelation(new DateOnly(2024, 2, 29), new DateTime(2024, 2, 29, 0, 0, 0, DateTimeKind.Utc), Relation.Equal);
        }

        [Fact]
        public void InputValueChanged_TimeOnly_IsComparedAsTicks()
        {
            AssertRelation(new TimeOnly(0, 0, 1), 10_000_000L, Relation.Equal);
        }

        [Fact]
        public void InputValueChanged_TwoTimeOnly_AreCompared()
        {
            AssertRelation(new TimeOnly(12, 0), new TimeOnly(11, 59, 59), Relation.Greater);
        }

        [Fact]
        public void InputValueChanged_OnlyInput1Known_EmitsNothing()
        {
            Assert.Empty(Set1(1));
        }

        [Fact]
        public void InputValueChanged_OnlyInput2Known_EmitsNothing()
        {
            Assert.Empty(Set2(1));
        }

        [Fact]
        public void InputValueChanged_NullBeforeAnyValue_EmitsNothing()
        {
            Assert.Empty(Set1(null));
            Assert.Empty(Set2(null));
        }

        [Fact]
        public void InputValueChanged_NullOnSecondInput_DoesNotCountAsKnown()
        {
            Set1(1);

            Assert.Empty(Set2(null));
        }

        [Fact]
        public void InputValueChanged_FirstComparison_IsAlwaysEmitted()
        {
            Set1(5);

            Assert.Single(Set2(5));
        }

        [Fact]
        public void InputValueChanged_SameResultAgain_IsNotEmitted()
        {
            Set1(1);
            Assert.Single(Set2(2));

            Assert.Empty(Set2(2));
            Assert.Empty(Set2(3));   // still Less
            Assert.Empty(Set1(0));   // still Less
        }

        [Fact]
        public void InputValueChanged_ResultChanges_IsEmittedOnlyOnChange()
        {
            Set1(1);
            Set2(2); // Less

            AssertEmittedOnChange(Set1(2), Relation.Less, Relation.Equal);
            AssertEmittedOnChange(Set1(3), Relation.Equal, Relation.Greater);
            AssertEmittedOnChange(Set2(double.NaN), Relation.Greater, Relation.Unordered);
            AssertEmittedOnChange(Set2(0), Relation.Unordered, Relation.Greater);
        }

        private void AssertEmittedOnChange(IList<ILogicOutputChanged> result, Relation before, Relation after)
        {
            if (ExpectedFor(before) == ExpectedFor(after))
            {
                Assert.Empty(result);
            }
            else
            {
                Assert.Equal(ExpectedFor(after), Assert.Single(result).Value);
            }
        }

        [Fact]
        public void InputValueChanged_NullAfterBothKnown_KeepsLastValues()
        {
            Set1(1);
            Set2(2); // Less

            Assert.Empty(Set1(null));
            // I1 is still 1
            AssertEmittedOnChange(Set2(1), Relation.Less, Relation.Equal);
        }

        [Fact]
        public void InputValueChanged_UnparsableString_EmitsNothingAndKeepsLastValue()
        {
            Set1(5);
            Set2(5); // Equal

            Assert.Empty(Set1("abc"));
            // I1 is still 5
            AssertEmittedOnChange(Set2(6), Relation.Equal, Relation.Less);
        }

        [Fact]
        public void InputValueChanged_UnconvertibleObject_EmitsNothing()
        {
            Set2(1);

            Assert.Empty(Set1(new object()));
        }

        [Fact]
        public void InputValueChanged_OutputValueIsBoxedBool()
        {
            Set1(1);

            Assert.IsType<bool>(Assert.Single(Set2(1)).Value);
        }
    }

    public class BiggerTruthTableTests : CompareRuleTestBase<BiggerLogicFactory>
    {
        protected override Guid Input1 => BiggerLogicFactory.RuleInput1;
        protected override Guid Input2 => BiggerLogicFactory.RuleInput2;
        protected override Guid Output => BiggerLogicFactory.RuleOutput;
        protected override bool ExpectedFor(Relation relation) => relation == Relation.Greater;
    }

    public class BiggerOrEqualTruthTableTests : CompareRuleTestBase<BiggerOrEqualLogicFactory>
    {
        protected override Guid Input1 => BiggerOrEqualLogicFactory.RuleInput1;
        protected override Guid Input2 => BiggerOrEqualLogicFactory.RuleInput2;
        protected override Guid Output => BiggerOrEqualLogicFactory.RuleOutput;
        protected override bool ExpectedFor(Relation relation) => relation is Relation.Greater or Relation.Equal;
    }

    public class EqualTruthTableTests : CompareRuleTestBase<EqualLogicFactory>
    {
        protected override Guid Input1 => EqualLogicFactory.RuleInput1;
        protected override Guid Input2 => EqualLogicFactory.RuleInput2;
        protected override Guid Output => EqualLogicFactory.RuleOutput;
        protected override bool ExpectedFor(Relation relation) => relation == Relation.Equal;
    }

    public class SmallerTruthTableTests : CompareRuleTestBase<SmallerLogicFactory>
    {
        protected override Guid Input1 => SmallerLogicFactory.RuleInput1;
        protected override Guid Input2 => SmallerLogicFactory.RuleInput2;
        protected override Guid Output => SmallerLogicFactory.RuleOutput;
        protected override bool ExpectedFor(Relation relation) => relation == Relation.Less;
    }

    public class SmallerOrEqualTruthTableTests : CompareRuleTestBase<SmallerOrEqualLogicFactory>
    {
        protected override Guid Input1 => SmallerOrEqualLogicFactory.RuleInput1;
        protected override Guid Input2 => SmallerOrEqualLogicFactory.RuleInput2;
        protected override Guid Output => SmallerOrEqualLogicFactory.RuleOutput;
        protected override bool ExpectedFor(Relation relation) => relation is Relation.Less or Relation.Equal;
    }

    public class UnequalTruthTableTests : CompareRuleTestBase<UnequalLogicFactory>
    {
        protected override Guid Input1 => UnequalLogicFactory.RuleInput1;
        protected override Guid Input2 => UnequalLogicFactory.RuleInput2;
        protected override Guid Output => UnequalLogicFactory.RuleOutput;
        // NaN != x is true
        protected override bool ExpectedFor(Relation relation) => relation != Relation.Equal;
    }
}
