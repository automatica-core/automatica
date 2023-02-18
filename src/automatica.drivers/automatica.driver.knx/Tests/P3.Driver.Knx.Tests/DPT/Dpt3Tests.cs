using Automatica.Core.Base.Templates;
using P3.Knx.Core.Driver;
using P3.Knx.Core.Driver.DPT;
using P3.Knx.Core.Driver.DPT.Base;
using Xunit;

namespace P3.Driver.Knx.Tests.DPT
{
    public class Dpt3Tests
    {
        [Fact]
        public void TestStepUp()
        {
            var data = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt3).EnumValue, new byte[] { 0x0b });
            if (data is Dpt3Data dpt3Data)
            {

                Assert.True(dpt3Data.Direction);
                Assert.True(dpt3Data.Value == 3);
            }
            else
            {
                Assert.False(true);
            }


            var dpt3Test = new Dpt3Data(true, 100);
            var rev = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt3).EnumValue, dpt3Test);

            Assert.True(rev[0] == 0x0F);
        }
        [Fact]
        public void TestStepDown()
        {
            var data = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt3).EnumValue, new byte[] { 0x03 });
            if (data is Dpt3Data dpt3Data)
            {

                Assert.False(dpt3Data.Direction);
                Assert.True(dpt3Data.Value == 3);
            }
            else
            {
                Assert.False(true);
            }

            var dpt3Test2 = new Dpt3Data(false, 6);
            var rev2 = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt3).EnumValue, dpt3Test2);

            Assert.True(rev2[0] == 0x06);
        }

        [Fact]
        public void Test_ToDatapointException1()
        {
            Assert.Throws<ToDataPointException>(() => DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt3).EnumValue, null));
        }
        [Fact]
        public void Test_FromDatapointException1()
        {
            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt3).EnumValue, null));
        }

        [Fact]
        public void Test_ToDatapointException2()
        {
            Assert.Throws<ToDataPointException>(() => DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt3).EnumValue, new object()));
        }
        [Fact]
        public void Test_FromDatapointException2()
        {
            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt3).EnumValue, new byte[2]));
        }
    }
}
