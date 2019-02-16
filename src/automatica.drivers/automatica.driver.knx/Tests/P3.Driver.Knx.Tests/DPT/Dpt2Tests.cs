using System;
using System.Collections.Generic;
using System.Text;
using Automatica.Core.Base.Templates;
using P3.Knx.Core.DPT;
using P3.Knx.Core.DPT.Base;
using P3.Knx.Core.Driver;
using Xunit;

namespace P3.Driver.Knx.Tests.DPT
{
    public class Dpt2Tests
    {
        [Fact]
        public void Test002_1()
        {
            var input = new byte[] { 0x03 };
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt2).EnumValue, input);

            Assert.True(value is Dpt2Value);

            var dpt2Value = (Dpt2Value)value;

            Assert.True(dpt2Value.Control);
            Assert.True(dpt2Value.Value);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt2).EnumValue, value);

            Assert.Equal(input, busValue);
        }

        [Fact]
        public void Test002_2()
        {
            var input = new byte[] { 0x01 };
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt2).EnumValue, input);

            Assert.True(value is Dpt2Value);

            var dpt2Value = (Dpt2Value)value;

            Assert.True(!dpt2Value.Control);
            Assert.True(dpt2Value.Value);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt2).EnumValue, value);

            Assert.Equal(input, busValue);
        }


        [Fact]
        public void Test002_3()
        {
            var input = new byte[] { 0x02 };
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt2).EnumValue, input);

            Assert.True(value is Dpt2Value);

            var dpt2Value = (Dpt2Value)value;

            Assert.True(dpt2Value.Control);
            Assert.True(!dpt2Value.Value);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt2).EnumValue, value);

            Assert.Equal(input, busValue);
        }

        [Fact]
        public void Test002_ToDatapointException1()
        {
            Assert.Throws<ToDataPointException>(() => DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt2).EnumValue, null));
        }
        [Fact]
        public void Test002_FromDatapointException1()
        {
            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt2).EnumValue, null));
        }

        [Fact]
        public void Test002_ToDatapointException2()
        {
            Assert.Throws<ToDataPointException>(() => DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt2).EnumValue, new object()));
        }
        [Fact]
        public void Test002_FromDatapointException2()
        {
            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt2).EnumValue, new byte[2]));
        }

    }
}
