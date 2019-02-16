using System;
using Automatica.Core.Base.Templates;
using P3.Knx.Core.DPT;
using P3.Knx.Core.DPT.Base;
using P3.Knx.Core.Driver;
using Xunit;

namespace P3.Driver.Knx.Tests.DPT
{
    public class Dpt1Tests
    {
        [Fact]
        public void Test_False()
        {
            var test = new Dpt1Translator();

            var value = test.FromDataPoint(new byte[] { 0 });
            Assert.False((bool)value);
        }

        [Fact]
        public void Test1_True()
        {
            var test = new Dpt1Translator();

            var value = test.FromDataPoint(new byte[] { 1 });
            Assert.True((bool)value);
        }

        [Fact]
        public void Test2_True()
        {
            var test = new Dpt1Translator();

            var value = test.FromDataPoint(new byte[] { 255 });
            Assert.True((bool)value);
        }

        [Fact]
        public void Test_FromDataPointException()
        {
            var test = new Dpt1Translator();

            Assert.Throws<FromDataPointException>(() => test.FromDataPoint(new byte[] { 255, 12 }));

        }

        [Fact]
        public void Test_DptTranslator()
        {
            Assert.True((bool)DptTranslator.Instance.FromDataPoint("1", new byte[] {1}));
        }

        [Fact]
        public void Test_DptEnumTranslator()
        {
            Assert.True((bool) DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt1).EnumValue, new byte[] {1}));
        }

        [Fact]
        public void Test_ToBusValue_true()
        {
            Assert.Equal(DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt1).EnumValue, true), new byte[] { 1 });
        }
        [Fact]
        public void Test_ToBusValue_false()
        {
            Assert.Equal(DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt1).EnumValue, false), new byte[] { 0 });
        }


        [Fact]
        public void Test_ToBusValue_1()
        {
            Assert.Equal(DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt1).EnumValue, 1), new byte[] { 1 });
        }
        [Fact]
        public void Test_ToBusValue_0()
        {
            Assert.Equal(DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt1).EnumValue, 0), new byte[] { 0 });
        }

        [Fact]
        public void Test_ToBusValue_100()
        {
            Assert.Equal(DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt1).EnumValue, 100), new byte[] { 1 });
        }
        [Fact]
        public void Test_ToBusValue_1d()
        {
            Assert.Equal(DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt1).EnumValue, 1d), new byte[] { 1 });
        }
        [Fact]
        public void Test_ToBusValue_0d()
        {
            Assert.Equal(DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt1).EnumValue, 0d), new byte[] { 0 });
        }

        [Fact]
        public void Test_ToBusValue_100d()
        {
            Assert.Equal(DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt1).EnumValue, 100d), new byte[] { 1 });
        }

        [Fact]
        public void Test_ToDatapointException1()
        {
            Assert.Throws<ToDataPointException>(() => DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt1).EnumValue, null));
        }
        [Fact]
        public void Test_FromDatapointException1()
        {
            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt1).EnumValue, null));
        }

        [Fact]
        public void Test_ToDatapointException2()
        {
            Assert.Throws<ToDataPointException>(() => DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt1).EnumValue, new object()));
        }
        [Fact]
        public void Test_FromDatapointException2()
        {
            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt1).EnumValue, new byte[2]));
        }


        [Fact]
        public void Test_ToDatapoint_NullReferenceException()
        {
            Assert.Throws<ToDataPointException>(() => DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt1).EnumValue, null));
        }

        [Fact]
        public void Test_FromDatapoint_NullReferenceException()
        {
            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt1).EnumValue, null));
        }
    }
}
