using System;
using System.Linq;
using Automatica.Core.Base.Templates;
using P3.Knx.Core.Driver;
using P3.Knx.Core.Driver.DPT;
using P3.Knx.Core.Driver.DPT.Base;
using Xunit;

namespace P3.Driver.Knx.Tests.DPT
{
    public class Dpt11Tests
    {
        [Fact]
        public void TestDpt11_FromDatapoint1()
        {
            var input = new byte[] {11, 4, 18}.ToArray();
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt11).EnumValue, input);

            Assert.True(value is DateTime);

            var date = (DateTime) value;

            Assert.True(date.Day == 11);
            Assert.True(date.Month== 04);
            Assert.True(date.Year == 2018);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt11).EnumValue, date);
            Assert.Equal(input, busValue);
        }

        [Fact]
        public void TestDpt11_ToDatapointException()
        {
            var date = new DateTime(1960, 1, 1);
            Assert.Throws<ToDataPointException>(() => DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt11).EnumValue, date));
        }


        [Fact]
        public void TestDpt11_FromDatapointException()
        {
            var input = new byte[] { 99, 4, 18, 18 }.Reverse().ToArray();
            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt11).EnumValue, input));
        }

        [Fact]
        public void TestDpt11_FromDatapoint2()
        {
            var input = new byte[] { 11, 4, 99 }.ToArray();
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt11).EnumValue, input);

            Assert.True(value is DateTime);

            var date = (DateTime)value;

            Assert.True(date.Day == 11);
            Assert.True(date.Month == 04);
            Assert.True(date.Year == 1999);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt11).EnumValue, date);
            Assert.Equal(input, busValue);
        }

    }
}
