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
    public class Dpt4Tests
    {
        [Fact]
        public void TestDpt4_1()
        {
            Assert.True((char)DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt4).EnumValue, new byte[] { 0x41 }) == 'A');
        }

        [Fact]
        public void Test_ToDatapointException1()
        {
            Assert.Throws<ToDataPointException>(() => DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt4).EnumValue, null));
        }
        [Fact]
        public void Test_FromDatapointException1()
        {
            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt4).EnumValue, null));
        }

        [Fact]
        public void Test_ToDatapointException2()
        {
            Assert.Throws<ToDataPointException>(() => DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt4).EnumValue, new object()));
        }
        [Fact]
        public void Test_FromDatapointException2()
        {
            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt4).EnumValue, new byte[2]));
        }
    }
}
