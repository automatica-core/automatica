using P3.Logic.StringOperations.Status;
using Xunit;

namespace P3.Logic.StringOperations.Tests.Status
{
    public class StatusConditionTests
    {
        private static bool Eval(string condition, params object[] inputs)
        {
            Assert.True(StatusCondition.TryParse(condition, out var parsed, out var error), error);
            var values = new object[StatusCondition.InputCount];
            inputs.CopyTo(values, 0);
            return parsed.Evaluate(values);
        }

        [Theory]
        // the Loxone operators
        [InlineData("I1 == 5", 5, true)]
        [InlineData("I1 == 5", 5.0000000001, true)]
        [InlineData("I1 = 5", 6, false)]
        [InlineData("I1 != 5", 6, true)]
        [InlineData("I1 > 20", 20.5, true)]
        [InlineData("I1 > 20", 20, false)]
        [InlineData("I1 >= 20", 20, true)]
        [InlineData("I1 < 0", -0.5, true)]
        [InlineData("I1 <= -1", -1, true)]
        [InlineData("I1 *= arm", "Alarm!", true)]
        [InlineData("I1 !* arm", "Alarm!", false)]
        [InlineData("I1 : Al", "Alarm", true)]
        [InlineData("I1 !: Al", "Alarm", false)]
        // text ignores the case, numbers compare as numbers ("05" == 5)
        [InlineData("I1 == open", "OPEN", true)]
        [InlineData("I1 == 5", "05", true)]
        [InlineData("I1 == \"front door\"", "Front Door", true)]
        [InlineData("I1 == 'a b'", "a b", true)]
        // numeric operators need numbers (Loxone: text with < > is not supported)
        [InlineData("I1 > 5", "abc", false)]
        [InlineData("I1 < 5", "abc", false)]
        // bools are 1 / 0
        [InlineData("I1 == 1", true, true)]
        [InlineData("I1 == 0", false, true)]
        // decimal comma in the input text
        [InlineData("I1 > 21", "21,5", true)]
        // shorthand: starts with the operator = input 1
        [InlineData("> 20", 25, true)]
        [InlineData("*= rain", "light rain", true)]
        [InlineData("I1==5", 5, true)]
        [InlineData("<v1> >= 3", 3, true)]
        public void SingleComparison(string condition, object input, bool expected)
        {
            Assert.Equal(expected, Eval(condition, input));
        }

        [Fact]
        public void CompareTwoInputs()
        {
            Assert.True(Eval("I1 > I2", 5, 3));
            Assert.False(Eval("I1 > <v2>", 3, 5));
            Assert.True(Eval("I1 == <v2>", "x", "X"));
        }

        [Fact]
        public void AndBindsStrongerThanOr()
        {
            // I1 > 20 && I2 == 1 || I3 == 1
            Assert.True(Eval("I1 > 20 && I2 == 1 || I3 == 1", 25, 1, 0));
            Assert.False(Eval("I1 > 20 && I2 == 1 || I3 == 1", 25, 0, 0));
            Assert.True(Eval("I1 > 20 && I2 == 1 || I3 == 1", 0, 0, 1));
            Assert.True(Eval("I1 > 20 and I2 == 1 or I3 == 1", 25, 1, 0));
        }

        [Fact]
        public void InputWithoutValue_IsNoNumber()
        {
            // nothing received yet: "I1 < 5" must not be true and "I1 == 0" neither
            Assert.False(Eval("I1 < 5"));
            Assert.False(Eval("I1 == 0"));
            Assert.True(Eval("I1 == \"\""));
            Assert.True(Eval("I1 != 0"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("I1")]
        [InlineData("I1 >")]
        [InlineData("I1 > 5 I2")]
        [InlineData("I1 == \"open")]
        [InlineData("I1 > 5 &&")]
        [InlineData("I1 > 5 && > 3")]
        public void InvalidConditions_AreRejected(string condition)
        {
            Assert.False(StatusCondition.TryParse(condition, out _, out var error));
            Assert.False(string.IsNullOrEmpty(error));
        }

        [Fact]
        public void LiteralThatLooksLikeAnInput_OnlyI1ToI8()
        {
            // I9 is a text, not an input
            Assert.True(Eval("I1 == I9", "I9"));
        }
    }
}
