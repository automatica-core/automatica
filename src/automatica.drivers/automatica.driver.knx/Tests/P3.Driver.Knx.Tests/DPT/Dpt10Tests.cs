using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Automatica.Core.Base.Templates;
using P3.Knx.Core.DPT;
using P3.Knx.Core.DPT.Base;
using P3.Knx.Core.Driver;
using Xunit;

namespace P3.Driver.Knx.Tests.DPT
{
    public class Dpt10Tests
    {
        [Fact]
        public void TestDpt10_1()
        {
            var input = new byte[] {0x68, 0x27, 0x14}.ToArray();
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt10).EnumValue, input);

            Assert.True(value is Dpt10Value);

            var time = (Dpt10Value) value;

            Assert.True(time.TimeOfDay.Hours == 8);
            Assert.True(time.TimeOfDay.Minutes == 39);
            Assert.True(time.TimeOfDay.Seconds == 20);
            Assert.True(time.Weekday == 3);


            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt10).EnumValue, time).ToList();
            busValue.RemoveAt(0);
            Assert.Equal(input, busValue.ToArray());
        }

        [Fact]
        public void TestDpt10_2()
        {
            var input = new byte[] { 0x14, 0x21, 0x27 }.ToArray();
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt10).EnumValue, input);

            Assert.True(value is Dpt10Value);

            var time = (Dpt10Value)value;

            Assert.True(time.TimeOfDay.Hours == 20);
            Assert.True(time.TimeOfDay.Minutes == 33);
            Assert.True(time.TimeOfDay.Seconds == 39);
            Assert.True(time.Weekday == 0);


            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt10).EnumValue, time).ToList();
            busValue.RemoveAt(0);
            Assert.Equal(input, busValue.ToArray());
        }
        [Fact]
        public void Test_ToDatapointException1()
        {
            Assert.Throws<ToDataPointException>(() => DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt10).EnumValue, null));
        }
        [Fact]
        public void Test_FromDatapointException1()
        {
            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt10).EnumValue, null));
        }

        [Fact]
        public void Test_ToDatapointException2()
        {
            Assert.Throws<ToDataPointException>(() => DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt10).EnumValue, new object()));
        }
        [Fact]
        public void Test_FromDatapointException2()
        {
            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt10).EnumValue, new byte[0]));
        }
    }
}
