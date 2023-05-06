using Automatica.Core.Base.Templates;
using P3.Knx.Core.Driver;
using P3.Knx.Core.Driver.DPT;
using P3.Knx.Core.Driver.DPT.Base;
using P3.Knx.Core.Driver.DPT.Dpt8;
using Xunit;

namespace P3.Driver.Knx.Tests.DPT
{
    public class Dpt8Tests
    {
        [Fact]
        public void TestDpt8_001()
        {
            var input = new byte[] { 0x8e, 0x1d };
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt8Type.Dpt8_001).EnumValue, input);

            Assert.True((short) value == 7566);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt8Type.Dpt8_001).EnumValue, 7566);

            Assert.Equal(input, busValue);
        }

     


        [Fact]
        public void TestDpt8_003()
        {
            var input = new byte[] { 0x8e, 0x1d };
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt8Type.Dpt8_003).EnumValue, input);

            Assert.True((double)value == 75.66);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt8Type.Dpt8_003).EnumValue, 75.66);

            Assert.Equal(input, busValue);
        }

        [Fact]
        public void TestDpt8_003_2()
        {
            var translator = new Dpt8Type2Translator();
            Assert.Equal(short.MaxValue, translator.ConvertToBusValueInternal(int.MaxValue));
        }

        [Fact]
        public void TestDpt8_004()
        {
            var input = new byte[] { 0x8e, 0x1d };
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt8Type.Dpt8_004).EnumValue, input);

            Assert.True((double)value == 756.6);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt8Type.Dpt8_004).EnumValue, 756.6);

            Assert.Equal(input, busValue);
        }



        [Fact]
        public void Test_ToDatapointException1()
        {
            Assert.Throws<ToDataPointException>(() => DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt8Type.Dpt8_004).EnumValue, null));
        }
        [Fact]
        public void Test_FromDatapointException1()
        {
            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt8Type.Dpt8_004).EnumValue, null));
        }

        [Fact]
        public void Test_ToDatapointException2()
        {
            Assert.Throws<ToDataPointException>(() => DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt8Type.Dpt8_004).EnumValue, new object()));
        }
        [Fact]
        public void Test_FromDatapointException2()
        {
            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt8Type.Dpt8_004).EnumValue, new byte[0]));
        }
    }
}
