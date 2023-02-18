using System;
using Automatica.Core.Base.Templates;
using P3.Knx.Core.Driver;
using P3.Knx.Core.Driver.DPT;
using P3.Knx.Core.Driver.DPT.Base;
using P3.Knx.Core.Driver.DPT.Dpt6;
using Xunit;

namespace P3.Driver.Knx.Tests.DPT
{
    public class Dpt6Tests
    {
        [Fact]
        public void TestDpt6_001_1()
        {
            var input = new byte[] { 0x41 };
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt6Type.Dpt6_001).EnumValue, input);

            Assert.True(Convert.ToInt32(value) == 65);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt6Type.Dpt6_001).EnumValue, 65);

            Assert.Equal(input, busValue);
        }
        [Fact]
        public void TestDpt6_001_2()
        {
            var input = new [] { unchecked((byte) -104) };
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt6Type.Dpt6_001).EnumValue, input);

            Assert.True(Convert.ToInt32(value) == -104);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt6Type.Dpt6_001).EnumValue, -104);

            Assert.Equal(input, busValue);
        }


        [Fact]
        public void TestDpt6_010_1()
        {
            var input = new byte[] { 0x41 };
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt6Type.Dpt6_010).EnumValue, input);

            Assert.True(Convert.ToInt32(value) == 65);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt6Type.Dpt6_010).EnumValue, 65);

            Assert.Equal(input, busValue);
        }
        [Fact]
        public void TestDpt6_010_2()
        {
            var input = new[] { unchecked((byte)-104) };
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt6Type.Dpt6_010).EnumValue, input);

            Assert.True(Convert.ToInt32(value) == -104);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt6Type.Dpt6_010).EnumValue, -104);

            Assert.Equal(input, busValue);
        }



        [Fact]
        public void TestDpt6_020_1()
        {
            var input = new byte[] { 0xFF };
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt6_020).EnumValue, input);

            Assert.True(value.GetType() == typeof(Dpt6020Value));
            var dpt6020Value = (Dpt6020Value) value;

            Assert.True(dpt6020Value.A);
            Assert.True(dpt6020Value.B);
            Assert.True(dpt6020Value.C);
            Assert.True(dpt6020Value.D);
            Assert.True(dpt6020Value.E);
            Assert.True(dpt6020Value.Mode == 7);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt6_020).EnumValue, dpt6020Value);

            Assert.Equal(input, busValue);
        }
        [Fact]
        public void TestDpt6_020_2()
        {
            var input = new byte[] { 0xAA };
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt6_020).EnumValue, input);

            Assert.True(value.GetType() == typeof(Dpt6020Value));
            var dpt6020Value = (Dpt6020Value)value;

            Assert.True(dpt6020Value.A);
            Assert.False(dpt6020Value.B);
            Assert.True(dpt6020Value.C);
            Assert.False(dpt6020Value.D);
            Assert.True(dpt6020Value.E);
            Assert.True(dpt6020Value.Mode == 2);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt6_020).EnumValue, dpt6020Value);

            Assert.Equal(input, busValue);
        }


        [Fact]
        public void Test_ToDatapointException1()
        {
            Assert.Throws<ToDataPointException>(() => DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt6Type.Dpt6_010).EnumValue, null));
        }
        [Fact]
        public void Test_FromDatapointException1()
        {
            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt6Type.Dpt6_010).EnumValue, null));
        }

        [Fact]
        public void Test_ToDatapointException2()
        {
            Assert.Throws<ToDataPointException>(() => DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt6Type.Dpt6_010).EnumValue, new object()));
        }
        [Fact]
        public void Test_FromDatapointException2()
        {
            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt6Type.Dpt6_010).EnumValue, new byte[2]));
        }
    }
}
