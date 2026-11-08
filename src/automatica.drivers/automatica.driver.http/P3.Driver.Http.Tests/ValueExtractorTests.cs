using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using P3.Driver.Http.Driver.Extraction;
using Xunit;

namespace P3.Driver.Http.Tests
{
    public class ValueExtractorTests
    {
        private const string Json = "{\"sensors\":[{\"name\":\"hall\",\"temp\":21.5},{\"name\":\"garage\",\"temp\":-3}],\"on\":true,\"meta data\":{\"fw\":\"1.2\"},\"none\":null}";

        [Theory]
        [InlineData("$.sensors[0].temp", "21.5")]
        [InlineData("sensors.1.temp", "-3")]
        [InlineData("$.sensors[1].name", "garage")]
        [InlineData("$.on", "true")]
        [InlineData("$['meta data'].fw", "1.2")]
        [InlineData("$.sensors[5].temp", null)]
        [InlineData("$.missing", null)]
        [InlineData("$.none", null)]
        [InlineData("$.sensors[0]", "{\"name\":\"hall\",\"temp\":21.5}")]
        public void JsonPath(string path, string expected)
        {
            Assert.Equal(expected, ValueExtractor.Extract(Json, ExtractMode.Json, path));
        }

        [Fact]
        public void JsonPath_InvalidJson_Throws()
        {
            Assert.ThrowsAny<JsonException>(() => ValueExtractor.Extract("<html>", ExtractMode.Json, "$.a"));
        }

        [Theory]
        [InlineData("temp=([0-9.]+)", "21.5")]
        [InlineData("hum=\\d+", "hum=40")]
        [InlineData("(x)?temp=(\\d+)", "21")]
        [InlineData("nothing", null)]
        public void Regex_FirstMatchingGroupOrWholeMatch(string pattern, string expected)
        {
            Assert.Equal(expected, ValueExtractor.Extract("status ok temp=21.5 hum=40", ExtractMode.Regex, pattern));
        }

        [Fact]
        public void Regex_Invalid_IsAFormatError()
        {
            Assert.Throws<FormatException>(() => ValueExtractor.Extract("x", ExtractMode.Regex, "(unclosed"));
        }

        [Fact]
        public void Regex_CatastrophicBacktracking_TimesOut()
        {
            var text = new string('a', 40) + "!";
            Assert.Throws<RegexMatchTimeoutException>(() => ValueExtractor.Extract(text, ExtractMode.Regex, "^(a+)+$"));
        }

        [Fact]
        public void Text_IsTheTrimmedResponse()
        {
            Assert.Equal("42", ValueExtractor.Extract("  42\n", ExtractMode.Text, null));
        }

        [Theory]
        [InlineData("Temperature: 21.5 C", "\\iTemperature:\\i\\v", "21.5")]
        [InlineData("a=1;b=-2.25;c=3", "\\ib=\\i\\v", "-2.25")]
        [InlineData("power 1.5e3 W", "\\ipower\\i\\v", "1.5e3")]
        [InlineData("id:FF10", "\\iid:\\i\\h", "65296")]
        [InlineData("ABCDvalue 7", "\\s4value \\v", "7")]
        [InlineData("x1y22", "\\a\\d\\a\\v", "22")]
        [InlineData("<b>12</b>", "\\i<b>\\i\\v", "12")]
        [InlineData("line1\nT=3", "\\i\\nT=\\i\\v", "3")]
        [InlineData("tab\t9", "tab\\t\\v", "9")]
        [InlineData("a\\b 5", "a\\\\b \\v", "5")]
        [InlineData("word123 4", "\\w \\v", "4")]
        [InlineData("#12 5", "#\\# \\v", "5")]
        [InlineData("\u000A6", "\\x0A\\v", "6")]
        [InlineData("Temperature: n/a", "\\iTemperature:\\i\\v", null)]
        [InlineData("Humidity: 40", "\\iTemperature:\\i\\v", null)]
        [InlineData("xyz", "abc", null)]
        public void LoxonePattern(string text, string pattern, string expected)
        {
            Assert.Equal(expected, ValueExtractor.Extract(text, ExtractMode.Pattern, pattern));
        }

        [Fact]
        public void LoxonePattern_Bytes_AreLittleEndian()
        {
            var text = "S" + (char)0x34 + (char)0x12;
            Assert.Equal("4660", ValueExtractor.Extract(text, ExtractMode.Pattern, "S\\1\\2"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("\\iopen")]
        [InlineData("\\s")]
        [InlineData("abc\\")]
        [InlineData("\\xZZ")]
        public void LoxonePattern_Invalid_IsAFormatError(string pattern)
        {
            Assert.Throws<FormatException>(() => ValueExtractor.Extract("abc", ExtractMode.Pattern, pattern));
        }

        [Theory]
        [InlineData("21.5", 21.5)]
        [InlineData(" -3 ", -3.0)]
        [InlineData("21,5", 21.5)]
        [InlineData("true", 1.0)]
        [InlineData("1e3", 1000.0)]
        [InlineData("1,234.5", null)]
        [InlineData("NaN", null)]
        [InlineData("abc", null)]
        [InlineData(null, null)]
        public void ToNumber(string text, double? expected)
        {
            Assert.Equal(expected, ValueExtractor.ToNumber(text));
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("ON", true)]
        [InlineData("closed", false)]
        [InlineData("0", false)]
        [InlineData("2.5", true)]
        [InlineData("maybe", null)]
        public void ToBool(string text, bool? expected)
        {
            Assert.Equal(expected, ValueExtractor.ToBool(text));
        }
    }
}
