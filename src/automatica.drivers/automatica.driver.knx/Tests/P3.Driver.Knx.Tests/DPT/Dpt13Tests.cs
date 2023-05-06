using System.Linq;
using Automatica.Core.Base.Templates;
using P3.Knx.Core.Driver;
using P3.Knx.Core.Driver.DPT;
using Xunit;

namespace P3.Driver.Knx.Tests.DPT
{
    public class Dpt13Tests
    {
        [Fact]
        public void TestDpt13_FromDatapoint1()
        {
            var input = new byte[] { 0, 0, 0, 8 }.ToArray();
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt13).EnumValue, input);

            Assert.True(value is int);

            var intValue = (int) value;

            Assert.Equal(8, intValue);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt13).EnumValue, intValue);
            Assert.Equal(input, busValue);
        }


        [Fact]
        public void TestDpt13_FromDatapoint2()
        {
            var input = new byte[] { 0,0, 9, 72 }.ToArray();
            var value = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt13).EnumValue, input);

            Assert.True(value is int);

            var intValue = (int)value;

            Assert.Equal(2376, intValue);

            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(DptType.Dpt13).EnumValue, intValue);
            Assert.Equal(input, busValue);
        }

    }
}
