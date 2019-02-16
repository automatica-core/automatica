using System;
using Automatica.Core.Base.Templates;
using P3.Knx.Core.DPT;
using P3.Knx.Core.DPT.Base;
using P3.Knx.Core.DPT.Dpt5;
using P3.Knx.Core.Driver;
using Xunit;

namespace P3.Driver.Knx.Tests.DPT
{
    public class Dpt5Tests
    {
        [Fact]
        public void TestDpt5_001_1()
        {
            var input = new byte[] {0xB3};
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt5Type.Dpt5_001).EnumValue, input);

            Assert.True(Convert.ToInt16(value) == 70);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt5Type.Dpt5_001).EnumValue, 70);

            Assert.Equal(input, busValue);
        }


        [Fact]
        public void TestDpt5_001_2()
        {
            var input = new byte[] { 0xFF };
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt5Type.Dpt5_001).EnumValue, input);

            Assert.True(Convert.ToInt16(value) == 100);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt5Type.Dpt5_001).EnumValue, 120);

            Assert.Equal(input, busValue);
        }

        [Fact]
        public void TestDpt5_003_1()
        {
            var input = new byte[] { 0x32 };
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt5Type.Dpt5_003).EnumValue, input);

            Assert.True(Convert.ToInt16(value) == 70);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt5Type.Dpt5_003).EnumValue, 70);

            Assert.Equal(input, busValue);
        }


        [Fact]
        public void TestDpt5_003_2()
        {
            var input = new byte[] { 0xFF };
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt5Type.Dpt5_003).EnumValue, input);

            Assert.True(Convert.ToInt16(value) == 360);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt5Type.Dpt5_003).EnumValue, 360);

            Assert.Equal(input, busValue);
        }


        [Fact]
        public void TestDpt5_006_1()
        {
            var input = new byte[] { 0x32 };
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt5Type.Dpt5_006).EnumValue, input);

            Assert.True(Convert.ToInt16(value) == 50);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt5Type.Dpt5_006).EnumValue, 50);

            Assert.Equal(input, busValue);
        }


        [Fact]
        public void TestDpt5_006_2()
        {
            var input = new byte[] { 0xFF };
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt5Type.Dpt5_006).EnumValue, input);

            Assert.True(Convert.ToInt16(value) == 254);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt5Type.Dpt5_006).EnumValue, 254);

            Assert.Equal(new byte[] {254}, busValue);
        }

        [Fact]
        public void TestDpt5_006_3()
        {
            var translator = new Dpt5006TypeTranslator();
            Assert.Equal(254, translator.ConvertToBusValue(255));
        }

        [Fact]
        public void TestDpt5_004_1()
        {
            var input = new byte[] { 0x32 };
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt5Type.Dpt5_004).EnumValue, input);

            Assert.True(Convert.ToInt16(value) == 50);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt5Type.Dpt5_004).EnumValue, 50);

            Assert.Equal(input, busValue);
        }


        [Fact]
        public void TestDpt5_004_2()
        {
            var input = new byte[] { 0xFF };
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt5Type.Dpt5_004).EnumValue, input);

            Assert.True(Convert.ToInt16(value) == 255);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt5Type.Dpt5_004).EnumValue, 255);

            Assert.Equal(input, busValue);
        }


        [Fact]
        public void Test_ToDatapointException1()
        {
            Assert.Throws<ToDataPointException>(() => DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt5Type.Dpt5_004).EnumValue, null));
        }
        [Fact]
        public void Test_FromDatapointException1()
        {
            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt5Type.Dpt5_004).EnumValue, null));
        }

        [Fact]
        public void Test_ToDatapointException2()
        {
            Assert.Throws<ToDataPointException>(() => DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt5Type.Dpt5_004).EnumValue, new object()));
        }
        [Fact]
        public void Test_FromDatapointException2()
        {
            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt5Type.Dpt5_004).EnumValue, new byte[2]));
        }
    }
}
