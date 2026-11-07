using System;
using P3.Logic.Math.BasicOperations.Formula;
using Xunit;

namespace P3.Logic.Math.BasicOperations.Tests.Formula
{
    public class FormulaExpressionTests
    {
        [Theory]
        [InlineData("1 + 2 * 3", 7)]
        [InlineData("(1 + 2) * 3", 9)]
        [InlineData("2 ^ 3 ^ 2", 512)]          // right associative
        [InlineData("-2 ^ 2", -4)]
        [InlineData("2 ^ -1", 0.5)]
        [InlineData("10 % 4", 2)]
        [InlineData("1.5e2", 150)]
        [InlineData(".5 + .25", 0.75)]
        [InlineData("3 > 2", 1)]
        [InlineData("3 <= 2", 0)]
        [InlineData("2 == 2 && 1 != 2", 1)]
        [InlineData("0 || !0", 1)]
        [InlineData("!1", 0)]
        [InlineData("abs(-3) + sign(-5)", 2)]
        [InlineData("min(4, 2, 8) + max(1, 9)", 11)]
        [InlineData("round(2.5)", 3)]
        [InlineData("round(1.2345, 2)", 1.23)]
        [InlineData("floor(1.9) + ceil(1.1)", 3)]
        [InlineData("sqrt(16) + pow(2, 3)", 12)]
        [InlineData("clamp(150, 0, 100)", 100)]
        [InlineData("if(1 > 2, 10, 20)", 20)]
        [InlineData("log(1000) + ln(e)", 4)]
        [InlineData("round(sin(pi / 2))", 1)]
        public void Evaluates(string formula, double expected)
        {
            Assert.Equal(expected, FormulaExpression.Parse(formula).Evaluate(), 9);
        }

        [Fact]
        public void Variables_AreCaseInsensitive()
        {
            var f = FormulaExpression.Parse("(I1 + i2) / 2 + I3 * I4");
            Assert.Equal(17, f.Evaluate(10, 20, 1, 2));
        }

        [Fact]
        public void MissingVariables_AreZero()
        {
            Assert.Equal(5, FormulaExpression.Parse("I1 + I4").Evaluate(5));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("1 +")]
        [InlineData("(1 + 2")]
        [InlineData("1 2")]
        [InlineData("I5")]
        [InlineData("foo(1)")]
        [InlineData("round(1, 2, 3)")]
        [InlineData("1.2.3")]
        [InlineData("1 # 2")]
        [InlineData("System.Math.Pow(1,2)")]
        public void Invalid_Throws(string formula)
        {
            Assert.Throws<FormatException>(() => FormulaExpression.Parse(formula));
        }

        [Fact]
        public void DivisionByZero_IsInfinity()
        {
            Assert.True(double.IsInfinity(FormulaExpression.Parse("1 / I1").Evaluate(0)));
        }
    }
}
