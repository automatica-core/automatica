using P3.Logic.StringOperations.CommandRecognition;
using P3.Logic.StringOperations.Tests.TestSupport;
using Xunit;

namespace P3.Logic.StringOperations.Tests.CommandRecognition
{
    public class CommandPatternTests
    {
        private const string GasPrices = "Energy prices 08.11.2026 Natural Gas 10:42 AM 3.215 USD Crude Oil 10:41 AM 71.3 USD";

        [Theory]
        // the three variants of the Loxone example (natural gas price behind the time)
        [InlineData(@"\iNatural Gas\i\b\d\d\.\d\d\b\a\a\b\v")]
        [InlineData(@"\iNatural Gas\i\#\.\#\w\v")]
        [InlineData(@"\iNatural Gas \i\s8\b\v")]
        public void LoxoneExample_NaturalGas(string pattern)
        {
            Assert.Equal(3.215, CommandPattern.Extract(pattern, GasPrices, false));
        }

        [Theory]
        [InlineData(@"Temp: \v", "Temp: 21.5", 21.5)]
        [InlineData(@"Temp: \v", "Temp: -3", -3)]
        [InlineData(@"Temp:\v", "Temp:   7,25", 7.25)]
        [InlineData(@"\v", "42abc", 42)]
        [InlineData(@"\iVal=\i\h", "xx Val=0A0B", 2571)]
        [InlineData(@"\iV\i\v;\v", "V1;2", 2)]  // the last value
        [InlineData(@"\.\.\v", "ab5", 5)]
        [InlineData(@"\a\m\d\v", "Z9 7 8", null)] // \d needs a digit right after \m
        [InlineData(@"\a\m\d\v", "Z97 8", 8)]
        [InlineData(@"a\\b\v", @"a\b9", 9)]
        [InlineData(@"\x41\v", "A1", 1)]
        [InlineData(@"\tx\v", "\tx4", 4)]
        [InlineData(@"\i\r\n\i\v", "head\r\n77", 77)]
        public void Patterns(string pattern, string text, object expected)
        {
            Assert.Equal(expected == null ? null : System.Convert.ToDouble(expected), CommandPattern.Extract(pattern, text, false));
        }

        [Theory]
        [InlineData(@"Temp: \v", "Temperature 21")]   // literal does not match
        [InlineData(@"\iHumidity\i\v", "Temp: 21")]    // jump target missing
        [InlineData(@"\iTemp\i\v", "Temp: abc")]       // no number
        [InlineData(@"\s20\v", "short")]               // skip beyond the end
        [InlineData(@"Temp", "Temp")]                  // nothing extracted
        [InlineData("", "Temp")]
        public void NoMatch_IsNull(string pattern, string text)
        {
            Assert.Null(CommandPattern.Extract(pattern, text, false));
        }

        [Fact]
        public void Bytes_LowestFirst_AndSigned()
        {
            // \2\1: first character is the high byte
            Assert.Equal(0x0A0B, CommandPattern.Extract(@"\2\1", "\u000A\u000B", false));
            Assert.Equal(76, CommandPattern.Extract(@"\1", "Loxone", false));
            Assert.Equal(255, CommandPattern.Extract(@"\1", "ÿ", false));
            Assert.Equal(-1, CommandPattern.Extract(@"\1", "ÿ", true));
            Assert.Equal(-2, CommandPattern.Extract(@"\2\1", "ÿþ", true));
            Assert.Equal(0x7FFE, CommandPattern.Extract(@"\2\1", "\u007Fþ", true));
        }
    }

    public class CommandRecognitionTests : TextLogicTest<CommandRecognitionLogicFactory>
    {
        [Fact]
        public void SendsTheValue_KeepsItWhenNoMatch()
        {
            Param(CommandRecognitionLogicFactory.ParamPattern, @"\iT=\i\v");
            Start();

            Input(CommandRecognitionLogicFactory.InputText, "T=21.5;H=40");
            Assert.Equal(21.5, Last(CommandRecognitionLogicFactory.OutputValue));

            Assert.Empty(Input(CommandRecognitionLogicFactory.InputText, "error"));
            Assert.Equal(21.5, Last(CommandRecognitionLogicFactory.OutputValue));
        }

        [Fact]
        public void SignedParameter()
        {
            Param(CommandRecognitionLogicFactory.ParamPattern, @"\1");
            Param(CommandRecognitionLogicFactory.ParamSigned, true);
            Start();
            Input(CommandRecognitionLogicFactory.InputText, "ÿ");
            Assert.Equal(-1.0, Last(CommandRecognitionLogicFactory.OutputValue));
        }
    }
}
