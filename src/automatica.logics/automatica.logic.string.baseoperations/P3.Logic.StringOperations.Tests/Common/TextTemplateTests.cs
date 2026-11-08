using P3.Logic.StringOperations.Common;
using Xunit;

namespace P3.Logic.StringOperations.Tests.Common
{
    public class TextTemplateTests
    {
        [Theory]
        [InlineData("Temp <v1> °C", "Temp 21.5 °C")]
        [InlineData("<v1.1>|<v1.0>|<v1.3>", "21.5|22|21.500")]
        [InlineData("<v> / <v.2>", "21.5 / 21.50")]
        [InlineData("<v2> <V2>", "open open")]
        [InlineData("<v3>", "1")]
        [InlineData("[<v4>]", "[]")]
        [InlineData("<v9> <x1> <v1", "<v9> <x1> <v1")]
        [InlineData("", "")]
        public void Render_ReplacesThePlaceholders(string template, string expected)
        {
            var values = new object[] { 21.5, "open", true, null };
            Assert.Equal(expected, TextTemplate.Render(template, values));
        }

        [Fact]
        public void Render_NamePlaceholder()
        {
            Assert.Equal("Door: 1", TextTemplate.Render("<n>: <v>", new object[] { 1 }, "Door"));
            // a block without names keeps <n>
            Assert.Equal("<n>: 1", TextTemplate.Render("<n>: <v>", new object[] { 1 }));
        }

        [Fact]
        public void Render_TextWithDecimals_StaysText()
        {
            Assert.Equal("open", TextTemplate.Render("<v1.2>", new object[] { "open" }));
            Assert.Equal("3.14", TextTemplate.Render("<v1.2>", new object[] { "3.14159" }));
        }

        [Theory]
        [InlineData(1.0, "1")]
        [InlineData(0.1 + 0.2, "0.3")]
        [InlineData(-12.25, "-12.25")]
        [InlineData(1234567.5, "1234567.5")]
        public void ToText_FormatsNumbersInvariant(double value, string expected)
        {
            Assert.Equal(expected, TextValue.ToText(value));
        }

        [Theory]
        [InlineData("21,5", true, 21.5)]
        [InlineData(" 7 ", true, 7)]
        [InlineData("1,000.5", false, 0)]
        [InlineData("abc", false, 0)]
        [InlineData("", false, 0)]
        public void TryParse_AcceptsDecimalComma(string text, bool ok, double expected)
        {
            Assert.Equal(ok, TextValue.TryParse(text, out var value));
            Assert.Equal(expected, value);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        [InlineData(1, true)]
        [InlineData(0.0, false)]
        [InlineData("true", true)]
        [InlineData("0", false)]
        [InlineData("on", false)]
        [InlineData(null, false)]
        public void IsTruthy(object value, bool expected)
        {
            Assert.Equal(expected, TextValue.IsTruthy(value));
        }
    }
}
