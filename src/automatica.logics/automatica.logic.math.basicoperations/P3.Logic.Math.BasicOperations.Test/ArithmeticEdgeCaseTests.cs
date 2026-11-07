using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Math.BasicOperations.Addition;
using P3.Logic.Math.BasicOperations.Division;
using P3.Logic.Math.BasicOperations.Multiply;
using P3.Logic.Math.BasicOperations.Subtract;
using Xunit;

namespace P3.Logic.Math.BasicOperations.Tests
{
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

    public class AdditionEdgeCaseTests : LogicTest<AdditionLogicFactory>
    {
        private IList<ILogicOutputChanged> Set(Guid input, object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(input), Dispatchable, value);
        private static double Result(IList<ILogicOutputChanged> result) => (double)Assert.Single(result).Value;

        [Fact]
        public async Task Start_NoInputs_DispatchesZero()
        {
            await Context.Dispatcher.ClearValues();
            await Logic.Stop();
            await Logic.Start();

            Assert.Equal(0.0, StartValueTestHelper.OutputValue(this, AdditionLogicFactory.RuleOutput));
        }

        [Fact]
        public async Task StopStart_KeepsAllFourInputs()
        {
            Set(AdditionLogicFactory.RuleInput1, 1);
            Set(AdditionLogicFactory.RuleInput2, 2);
            Set(AdditionLogicFactory.RuleInput3, 3);
            Set(AdditionLogicFactory.RuleInput4, 4);

            await Context.Dispatcher.ClearValues();
            await Logic.Stop();
            await Logic.Start();

            Assert.Equal(10.0, StartValueTestHelper.OutputValue(this, AdditionLogicFactory.RuleOutput));
        }

        [Theory]
        [InlineData(1, 0, 0, 0, 1.0)]
        [InlineData(1, 2, 3, 4, 10.0)]
        [InlineData(-1, -2, 3, 0, 0.0)]
        [InlineData(0.1, 0.2, 0, 0, 0.30000000000000004)]
        [InlineData(1e308, 1e308, 0, 0, double.PositiveInfinity)]
        [InlineData(double.PositiveInfinity, double.NegativeInfinity, 0, 0, double.NaN)]
        [InlineData(double.NaN, 1, 2, 3, double.NaN)]
        public void InputValueChanged_FourInputs_EmitsSum(double i1, double i2, double i3, double i4, double expected)
        {
            Set(AdditionLogicFactory.RuleInput1, i1);
            Set(AdditionLogicFactory.RuleInput2, i2);
            Set(AdditionLogicFactory.RuleInput3, i3);
            var result = Set(AdditionLogicFactory.RuleInput4, i4);

            Assert.Equal(expected, Result(result));
            Assert.Equal(AdditionLogicFactory.RuleOutput, result[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate);
        }

        [Fact]
        public void InputValueChanged_OnlyOneInput_OthersCountAsZero()
        {
            Assert.Equal(7.5, Result(Set(AdditionLogicFactory.RuleInput3, 7.5)));
        }

        [Fact]
        public void InputValueChanged_Null_ResetsThatInputToZero()
        {
            Set(AdditionLogicFactory.RuleInput1, 5);
            Set(AdditionLogicFactory.RuleInput2, 3);

            Assert.Equal(3.0, Result(Set(AdditionLogicFactory.RuleInput1, null)));
        }

        [Fact]
        public void InputValueChanged_MixedTypes_AreConvertedToDouble()
        {
            Set(AdditionLogicFactory.RuleInput1, true);
            Set(AdditionLogicFactory.RuleInput2, 2L);
            Set(AdditionLogicFactory.RuleInput3, 0.5m);

            // true = 1, 2L, 0.5m, (byte)0
            Assert.Equal(3.5, Result(Set(AdditionLogicFactory.RuleInput4, (byte)0)));
        }

        [Fact]
        public void InputValueChanged_String_UsesCurrentCulture()
        {
            using (new CultureScope("de-DE"))
            {
                Assert.Equal(2.5, Result(Set(AdditionLogicFactory.RuleInput1, "2,5")));
            }

            using (new CultureScope("en-US"))
            {
                Assert.Equal(2.5, Result(Set(AdditionLogicFactory.RuleInput1, "2.5")));
            }
        }

        [Fact]
        public void InputValueChanged_UnparsableString_EmitsNothingAndKeepsInputs()
        {
            Set(AdditionLogicFactory.RuleInput1, 1);
            Set(AdditionLogicFactory.RuleInput2, 2);

            Assert.Empty(Set(AdditionLogicFactory.RuleInput1, "abc"));
            Assert.Equal(3.0, Result(Set(AdditionLogicFactory.RuleInput2, 2)));
        }

        [Fact]
        public void InputValueChanged_SameValueTwice_IsEmittedAgain()
        {
            Assert.Equal(1.0, Result(Set(AdditionLogicFactory.RuleInput1, 1)));
            Assert.Equal(1.0, Result(Set(AdditionLogicFactory.RuleInput1, 1)));
        }
    }

    public class SubtractEdgeCaseTests : LogicTest<SubtractLogicFactory>
    {
        private IList<ILogicOutputChanged> Set(Guid input, object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(input), Dispatchable, value);
        private static double Result(IList<ILogicOutputChanged> result) => (double)Assert.Single(result).Value;

        [Fact]
        public async Task Start_NoInputs_DispatchesZero()
        {
            await Context.Dispatcher.ClearValues();
            await Logic.Stop();
            await Logic.Start();

            Assert.Equal(0.0, StartValueTestHelper.OutputValue(this, SubtractLogicFactory.RuleOutput));
        }

        [Theory]
        [InlineData(10, 4, 6.0)]
        [InlineData(4, 10, -6.0)]
        [InlineData(-1, -1, 0.0)]
        [InlineData(0.3, 0.1, 0.19999999999999998)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, double.NaN)]
        [InlineData(double.NegativeInfinity, 1e308, double.NegativeInfinity)]
        [InlineData(-1e308, 1e308, double.NegativeInfinity)]
        [InlineData(1, double.NaN, double.NaN)]
        public void InputValueChanged_BothInputs_EmitsI1MinusI2(double i1, double i2, double expected)
        {
            Set(SubtractLogicFactory.RuleInput1, i1);
            var result = Set(SubtractLogicFactory.RuleInput2, i2);

            Assert.Equal(expected, Result(result));
            Assert.Equal(SubtractLogicFactory.RuleOutput, result[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate);
        }

        [Fact]
        public void InputValueChanged_OnlyInput2_IsSubtractedFromZero()
        {
            Assert.Equal(-3.0, Result(Set(SubtractLogicFactory.RuleInput2, 3)));
        }

        [Fact]
        public void InputValueChanged_Null_ResetsThatInputToZero()
        {
            Set(SubtractLogicFactory.RuleInput1, 10);
            Set(SubtractLogicFactory.RuleInput2, 4);

            Assert.Equal(10.0, Result(Set(SubtractLogicFactory.RuleInput2, null)));
        }

        [Fact]
        public void InputValueChanged_BoolAndString_AreConverted()
        {
            using (new CultureScope("de-DE"))
            {
                Set(SubtractLogicFactory.RuleInput1, "5,5");
                Assert.Equal(4.5, Result(Set(SubtractLogicFactory.RuleInput2, true)));
            }
        }

        [Fact]
        public void InputValueChanged_UnparsableString_EmitsNothingAndKeepsInputs()
        {
            Set(SubtractLogicFactory.RuleInput1, 10);
            Set(SubtractLogicFactory.RuleInput2, 4);

            Assert.Empty(Set(SubtractLogicFactory.RuleInput2, "x"));
            Assert.Equal(7.0, Result(Set(SubtractLogicFactory.RuleInput1, 11)));
        }
    }

    public class MultiplyEdgeCaseTests : LogicTest<MultiplyLogicFactory>
    {
        private IList<ILogicOutputChanged> Set(Guid input, object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(input), Dispatchable, value);
        private static double Result(IList<ILogicOutputChanged> result) => (double)Assert.Single(result).Value;

        [Theory]
        [InlineData(3, 4, 12.0)]
        [InlineData(-3, 4, -12.0)]
        [InlineData(-3, -4, 12.0)]
        [InlineData(0, 1e308, 0.0)]
        [InlineData(1e308, 10, double.PositiveInfinity)]
        [InlineData(-1e308, 10, double.NegativeInfinity)]
        [InlineData(0, double.PositiveInfinity, double.NaN)]
        [InlineData(double.NaN, 2, double.NaN)]
        [InlineData(0.5, 0.5, 0.25)]
        public void InputValueChanged_BothInputs_EmitsProduct(double i1, double i2, double expected)
        {
            Set(MultiplyLogicFactory.RuleInput1, i1);
            var result = Set(MultiplyLogicFactory.RuleInput2, i2);

            Assert.Equal(expected, Result(result));
            Assert.Equal(MultiplyLogicFactory.RuleOutput, result[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate);
        }

        [Fact]
        public void InputValueChanged_OnlyOneInput_OtherDefaultsToOne()
        {
            Assert.Equal(5.0, Result(Set(MultiplyLogicFactory.RuleInput1, 5)));
        }

        [Fact]
        public void InputValueChanged_Null_IsIgnoredAndKeepsLastValue()
        {
            Set(MultiplyLogicFactory.RuleInput1, 5);
            Set(MultiplyLogicFactory.RuleInput2, 3);

            Assert.Equal(15.0, Result(Set(MultiplyLogicFactory.RuleInput1, null)));
        }

        [Fact]
        public void InputValueChanged_BoolFalse_MultipliesWithZero()
        {
            Set(MultiplyLogicFactory.RuleInput1, 5);

            Assert.Equal(0.0, Result(Set(MultiplyLogicFactory.RuleInput2, false)));
        }

        [Fact]
        public void InputValueChanged_UnparsableString_EmitsNothingAndKeepsInputs()
        {
            Set(MultiplyLogicFactory.RuleInput1, 5);
            Set(MultiplyLogicFactory.RuleInput2, 3);

            Assert.Empty(Set(MultiplyLogicFactory.RuleInput2, "three"));
            Assert.Equal(6.0, Result(Set(MultiplyLogicFactory.RuleInput1, 2)));
        }
    }

    public class DivisionEdgeCaseTests : LogicTest<DivisionLogicFactory>
    {
        private IList<ILogicOutputChanged> Set(Guid input, object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(input), Dispatchable, value);
        private static double Result(IList<ILogicOutputChanged> result) => (double)Assert.Single(result).Value;

        [Theory]
        [InlineData(10, 4, 2.5)]
        [InlineData(-10, 4, -2.5)]
        [InlineData(1, 3, 1.0 / 3.0)]
        [InlineData(1, 0, double.PositiveInfinity)]
        [InlineData(-1, 0, double.NegativeInfinity)]
        [InlineData(0, 0, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, double.NaN)]
        [InlineData(1, double.PositiveInfinity, 0.0)]
        [InlineData(double.NaN, 1, double.NaN)]
        public void InputValueChanged_BothInputs_EmitsQuotient(double i1, double i2, double expected)
        {
            Set(DivisionLogicFactory.RuleInput1, i1);
            var result = Set(DivisionLogicFactory.RuleInput2, i2);

            Assert.Equal(expected, Result(result));
            Assert.Equal(DivisionLogicFactory.RuleOutput, result[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate);
        }

        [Fact]
        public void InputValueChanged_DivideByIntegerZero_DoesNotThrow()
        {
            Set(DivisionLogicFactory.RuleInput1, 5);

            Assert.Equal(double.PositiveInfinity, Result(Set(DivisionLogicFactory.RuleInput2, 0)));
        }

        [Fact]
        public void InputValueChanged_OnlyDividend_DivisorDefaultsToOne()
        {
            Assert.Equal(7.0, Result(Set(DivisionLogicFactory.RuleInput1, 7)));
        }

        [Fact]
        public void InputValueChanged_OnlyDivisor_DividendDefaultsToOne()
        {
            Assert.Equal(0.25, Result(Set(DivisionLogicFactory.RuleInput2, 4)));
        }

        [Fact]
        public void InputValueChanged_Null_IsIgnoredAndKeepsLastValue()
        {
            Set(DivisionLogicFactory.RuleInput1, 9);
            Set(DivisionLogicFactory.RuleInput2, 3);

            Assert.Equal(3.0, Result(Set(DivisionLogicFactory.RuleInput2, null)));
        }

        [Fact]
        public void InputValueChanged_String_UsesCurrentCulture()
        {
            using (new CultureScope("de-DE"))
            {
                Set(DivisionLogicFactory.RuleInput1, "7,5");
                Assert.Equal(2.5, Result(Set(DivisionLogicFactory.RuleInput2, "3")));
            }
        }

        [Fact]
        public void InputValueChanged_UnparsableString_EmitsNothingAndKeepsInputs()
        {
            Set(DivisionLogicFactory.RuleInput1, 9);
            Set(DivisionLogicFactory.RuleInput2, 3);

            Assert.Empty(Set(DivisionLogicFactory.RuleInput2, "zero"));
            Assert.Equal(6.0, Result(Set(DivisionLogicFactory.RuleInput1, 18)));
        }
    }
}
