using System;
using System.Collections.Generic;
using System.Globalization;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.StringOperations.Concat;
using Xunit;

namespace P3.Logic.StringOperations.Tests.Concat
{
    public class StringConcatEdgeCaseTests : LogicTest<StringConcatFactory>
    {
        private sealed class CultureScope : IDisposable
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

        private IList<ILogicOutputChanged> Set(Guid input, object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(input), Dispatchable, value);
        private static object Result(IList<ILogicOutputChanged> result) => Assert.Single(result).Value;

        [Fact]
        public void InputValueChanged_OnlyOneInput_OthersAreEmpty()
        {
            Assert.Equal("b", Result(Set(StringConcatFactory.RuleInput2, "b")));
        }

        [Fact]
        public void InputValueChanged_InputsSetInAnyOrder_AreConcatenatedI1ToI4()
        {
            Set(StringConcatFactory.RuleInput4, "d");
            Set(StringConcatFactory.RuleInput2, "b");
            Set(StringConcatFactory.RuleInput3, "c");
            var result = Set(StringConcatFactory.RuleInput1, "a");

            Assert.Equal("abcd", Result(result));
            Assert.Equal(StringConcatFactory.RuleOutput, result[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate);
        }

        [Fact]
        public void InputValueChanged_OutputIsString()
        {
            Assert.IsType<string>(Result(Set(StringConcatFactory.RuleInput1, 5)));
        }

        [Fact]
        public void InputValueChanged_Null_ClearsThatInput()
        {
            Set(StringConcatFactory.RuleInput1, "a");
            Set(StringConcatFactory.RuleInput2, "b");

            Assert.Equal("b", Result(Set(StringConcatFactory.RuleInput1, null)));
        }

        [Fact]
        public void InputValueChanged_AllNull_EmitsEmptyString()
        {
            Assert.Equal(string.Empty, Result(Set(StringConcatFactory.RuleInput3, null)));
        }

        [Fact]
        public void InputValueChanged_EmptyAndWhitespace_ArePreserved()
        {
            Set(StringConcatFactory.RuleInput1, " ");
            Set(StringConcatFactory.RuleInput2, string.Empty);
            Set(StringConcatFactory.RuleInput3, "\t");

            Assert.Equal(" \tx", Result(Set(StringConcatFactory.RuleInput4, "x")));
        }

        [Fact]
        public void InputValueChanged_Unicode_IsPreserved()
        {
            Set(StringConcatFactory.RuleInput1, "Grüße ");

            Assert.Equal("Grüße 🙂", Result(Set(StringConcatFactory.RuleInput2, "🙂")));
        }

        [Theory]
        [InlineData(5, "5")]
        [InlineData(-12L, "-12")]
        [InlineData(true, "True")]
        [InlineData(false, "False")]
        [InlineData('c', "c")]
        public void InputValueChanged_NonString_UsesToString(object value, string expected)
        {
            Assert.Equal(expected, Result(Set(StringConcatFactory.RuleInput1, value)));
        }

        [Fact]
        public void InputValueChanged_Double_UsesCurrentCulture()
        {
            using (new CultureScope("de-DE"))
            {
                Assert.Equal("1,5", Result(Set(StringConcatFactory.RuleInput1, 1.5)));
            }

            using (new CultureScope("en-US"))
            {
                Assert.Equal("1.5", Result(Set(StringConcatFactory.RuleInput1, 1.5)));
            }
        }

        [Fact]
        public void InputValueChanged_NaN_UsesToString()
        {
            using (new CultureScope("en-US"))
            {
                Assert.Equal("NaN", Result(Set(StringConcatFactory.RuleInput1, double.NaN)));
            }
        }

        [Fact]
        public void InputValueChanged_SameValueTwice_IsEmittedAgain()
        {
            Assert.Equal("x", Result(Set(StringConcatFactory.RuleInput1, "x")));
            Assert.Equal("x", Result(Set(StringConcatFactory.RuleInput1, "x")));
        }

        [Fact]
        public void InputValueChanged_ValueReplaced_OnlyThatPartChanges()
        {
            Set(StringConcatFactory.RuleInput1, "a");
            Set(StringConcatFactory.RuleInput2, "b");
            Set(StringConcatFactory.RuleInput3, "c");

            Assert.Equal("aXc", Result(Set(StringConcatFactory.RuleInput2, "X")));
        }

        [Fact]
        public void InputValueChanged_LongStrings_AreConcatenated()
        {
            var part = new string('a', 10_000);
            Set(StringConcatFactory.RuleInput1, part);

            Assert.Equal(20_000, ((string)Result(Set(StringConcatFactory.RuleInput2, part))).Length);
        }
    }
}
