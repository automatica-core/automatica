using System;
using System.Linq;
using System.Text;
using Automatica.Core.Base.Templates;
using P3.Knx.Core.Driver;
using P3.Knx.Core.Driver.DPT;
using P3.Knx.Core.Driver.DPT.Base;
using Xunit;

namespace P3.Driver.Knx.Tests.DPT
{
    public class Dpt16Tests
    {
        [Fact]
        public void TestDpt16_000_0()
        {
            var shouldBe = "KNX is OK";

            var input = new byte[] { 0x4B, 0x4E, 0x58, 0x20, 0x69, 0x73, 0x20, 0x4F, 0x4B, 0x00, 0x00, 0x00, 0x00, 0x00 }.ToArray();
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt16Type.Dpt16_000).EnumValue, input);

            Assert.True(value is string);

            Assert.Equal(shouldBe, value);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt16Type.Dpt16_000).EnumValue, value).AsSpan().Slice(1, 14).ToArray();
            
            Assert.Equal(14, busValue.Length);
            Assert.Equal(input, busValue);
        }

        [Fact]
        public void TestDpt16_000_11()
        {
            var input = new byte[] {0x37, 0x31, 0x34, 0x31, 0x2E, 0x36, 0x31, 0x24, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt16Type.Dpt16_000).EnumValue, input);

            Assert.Equal("7141.61$", value);

        }

        [Fact]
        public void TestDpt16_001_1()
        {
            var input = Encoding.ASCII.GetBytes("this_string_is_to_long");

            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt16Type.Dpt16_000).EnumValue, input));
        }

        [Fact]
        public void TestDpt16_001_2()
        {
            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt16Type.Dpt16_000).EnumValue, new byte[0]));
        }

        [Fact]
        public void TestDpt16_001_3()
        {
            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt16Type.Dpt16_000).EnumValue, null));
        }

        [Fact]
        public void TestDpt16_001_4()
        {

            var busValue = DptTranslator.Instance.ToDataPoint(
                    PropertyHelper.GetNameAttributeFromEnumValue(Dpt16Type.Dpt16_001).EnumValue,
                    "this_string_is_to_long").AsSpan().Slice(1, 14).ToArray();

            Assert.Equal(14, busValue.Length);

        }

        [Fact]
        public void TestDpt16_001_0()
        {
            var shouldBe = "KNX is OK";

            var input = new byte[] { 0x4B, 0x4E, 0x58, 0x20, 0x69, 0x73, 0x20, 0x4F, 0x4B, 0x00, 0x00, 0x00, 0x00, 0x00 }.ToArray();
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt16Type.Dpt16_001).EnumValue, input);

            Assert.True(value is string);

            Assert.Equal(shouldBe, value);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt16Type.Dpt16_001).EnumValue, value).AsSpan().Slice(1, 14).ToArray();
            Assert.Equal(14, busValue.Length);
            Assert.Equal(input, busValue);
        }
    }
}
