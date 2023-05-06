using Automatica.Core.Base.Templates;
using P3.Knx.Core.Driver;
using P3.Knx.Core.Driver.DPT;
using P3.Knx.Core.Driver.DPT.Base;
using P3.Knx.Core.Driver.DPT.Dpt7;
using Xunit;

namespace P3.Driver.Knx.Tests.DPT
{
    public class Dpt7Tests
    {
        [Fact]
        public void TestDpt7_001()
        {
            var input = new byte[] { 0x8e, 0x1d };
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt7Type.Dpt7_001).EnumValue, input);

            Assert.True((ushort) value == 7566);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt7Type.Dpt7_001).EnumValue, 7566);

            Assert.Equal(input, busValue);
        }


        [Fact]
        public void TestDpt7_003()
        {
            var input = new byte[] { 0x8e, 0x1d };
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt7Type.Dpt7_003).EnumValue, input);

            Assert.True((double)value == 75.66);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt7Type.Dpt7_003).EnumValue, 75.66);

            Assert.Equal(input, busValue);
        }

        [Fact]
        public void TestDpt7_003_2()
        {
            var translator = new Dpt7003TypeTranslator();
            Assert.Equal(ushort.MaxValue, translator.ConvertToBusValueInternal(int.MaxValue));
        }

        [Fact]
        public void TestDpt7_004()
        {
            var input = new byte[] { 0x8e, 0x1d };
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt7Type.Dpt7_004).EnumValue, input);

            Assert.True((double)value == 756.6);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt7Type.Dpt7_004).EnumValue, 756.6);

            Assert.Equal(input, busValue);
        }
        [Fact]
        public void TestDpt7_004_2()
        {
            var translator = new Dpt7004TypeTranslator();
            Assert.Equal(ushort.MaxValue, translator.ConvertToBusValueInternal(int.MaxValue));
        }

        [Fact]
        public void Test_ToDatapointException1()
        {
            Assert.Throws<ToDataPointException>(() => DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt7Type.Dpt7_004).EnumValue, null));
        }
        [Fact]
        public void Test_FromDatapointException1()
        {
            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt7Type.Dpt7_004).EnumValue, null));
        }

        [Fact]
        public void Test_ToDatapointException2()
        {
            Assert.Throws<ToDataPointException>(() => DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt7Type.Dpt7_004).EnumValue, new object()));
        }
        [Fact]
        public void Test_FromDatapointException2()
        {
            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt7Type.Dpt7_004).EnumValue, new byte[0]));
        }
    }
}
