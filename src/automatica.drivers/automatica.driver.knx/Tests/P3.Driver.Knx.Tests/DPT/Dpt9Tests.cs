using System;
using Automatica.Core.Base.Templates;
using P3.Knx.Core.Driver;
using P3.Knx.Core.Driver.DPT;
using P3.Knx.Core.Driver.DPT.Base;
using P3.Knx.Core.Driver.DPT.Dpt9;
using Xunit;

namespace P3.Driver.Knx.Tests.DPT
{
    public class Dpt9Tests
    {
        [Fact]
        public void TestDpt9_000_1()
        {
            var input = new byte[] { 0x0d, 0xf2 };
            var data = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_001).EnumValue, input);

            Assert.True(Convert.ToDouble(data) == 30.44d);

            var comp = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_001).EnumValue, 30.44).AsSpan().Slice(1, 2);

            Assert.Equal(input, comp.ToArray());
        }

        [Fact]
        public void TestDpt9_001_1()
        {
            var input = new byte[] { 0x0d, 0xf2 };
            var data = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_001).EnumValue, input);

            Assert.True(Convert.ToDouble(data) == 30.44d);

            var comp = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_001).EnumValue, 30.44).AsSpan().Slice(1, 2);

            Assert.Equal(input, comp.ToArray());
        }



        [Fact]
        public void TestDpt9_001_2()
        {
            var input = new byte[] {0x81, 0xc6};
            var data = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_001).EnumValue, input);

            Assert.True(Convert.ToDouble(data) == -15.94d);

            var comp = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_001).EnumValue, -15.94).AsSpan().Slice(1, 2);

            Assert.Equal(input, comp.ToArray());
        }


        [Fact]
        public void TestDpt9_001_3()
        {
            var input = new byte[] { 0x00, 0x64 };
            var data = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_001).EnumValue, input);

            Assert.True(Convert.ToDouble(data) == 1d);

            var comp = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_001).EnumValue, 1d).AsSpan().Slice(1, 2);

            Assert.Equal(input, comp.ToArray());
        }



        [Fact]
        public void TestDpt9_001_4()
        {
            var input = new byte[] {0xA0, 0xAD};
            var data = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_001).EnumValue, input);

            Assert.True(Convert.ToDouble(data) == -273d);

            var comp = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_001).EnumValue, -300d).AsSpan().Slice(1, 2);

            var output = new byte[] { 0xA1, 0x56 };
            Assert.Equal(output, comp.ToArray());
        }

        [Fact]
        public void TestDpt9_001_5()
        {
            var translator = new Dpt9001TypeTranslator();
            Assert.Equal(-273, translator.ValidateMinMax(-400));
        }

        [Fact]
        public void TestDpt9_001_6()
        {
            var translator = new Dpt9001TypeTranslator();
            Assert.Equal(670760, translator.ValidateMinMax(670769));
        }


        [Fact]
        public void TestDpt9_004_1()
        {
            var input = new byte[] { 0x0d, 0xf2 };
            var data = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_004).EnumValue, input);

            Assert.True(Convert.ToDouble(data) == 30.44d);

            var comp = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_004).EnumValue, 30.44).AsSpan().Slice(1, 2);

            Assert.Equal(input, comp.ToArray());
        }

        [Fact]
        public void TestDpt9_004_2()
        {
            var input = new byte[] { 0x81, 0xc6 };
            var data = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_004).EnumValue, input);

            Assert.True(Convert.ToDouble(data) == 0);

            var comp = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_004).EnumValue, -15.94).AsSpan().Slice(1, 2);

            Assert.Equal(new byte[] {0,0}, comp.ToArray());
        }


        [Fact]
        public void TestDpt9_004_3()
        {
            var translator = new Dpt9004TypeTranslator();
            Assert.Equal(0, translator.ValidateMinMax(-400));
        }

        [Fact]
        public void TestDpt9_004_4()
        {
            var translator = new Dpt9004TypeTranslator();
            Assert.Equal(670760, translator.ValidateMinMax(670769));
        }


        [Fact]
        public void TestDpt9_024_1()
        {
            var input = new byte[] { 0x0d, 0xf2 };
            var data = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_024).EnumValue, input);

            Assert.True(Convert.ToDouble(data) == 30.44d);

            var comp = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_024).EnumValue, 30.44).AsSpan().Slice(1, 2);

            Assert.Equal(input, comp.ToArray());
        }

        [Fact]
        public void TestDpt9_024_2()
        {
            var input = new byte[] { 0x81, 0xc6 };
            var data = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_024).EnumValue, input);

            Assert.True(Convert.ToDouble(data) == -15.94);

            var comp = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_024).EnumValue, -15.94).AsSpan().Slice(1, 2);

            Assert.Equal(input, comp.ToArray());
        }



        [Fact]
        public void TestDpt9_026_1()
        {
            var input = new byte[] { 0x0d, 0xf2 };
            var data = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_026).EnumValue, input);

            Assert.True(Convert.ToDouble(data) == 30.44d);

            var comp = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_026).EnumValue, 30.44).AsSpan().Slice(1, 2);

            Assert.Equal(input, comp.ToArray());
        }

        [Fact]
        public void TestDpt9_026_2()
        {
            var input = new byte[] { 0x81, 0xc6 };
            var data = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_026).EnumValue, input);

            Assert.True(Convert.ToDouble(data) == -15.94);

            var comp = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_026).EnumValue, -15.94).AsSpan().Slice(1, 2);

            Assert.Equal(input, comp.ToArray());
        }


        [Fact]
        public void TestDpt9_027_1()
        {
            var input = new byte[] { 0x0d, 0xf2 };
            var data = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_027).EnumValue, input);

            Assert.True(Convert.ToDouble(data) == 30.44d);

            var comp = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_027).EnumValue, 30.44).AsSpan().Slice(1, 2);

            Assert.Equal(input, comp.ToArray());
        }

        [Fact]
        public void TestDpt9_027_2()
        {
            var input = new byte[] { 0x81, 0xc6 };
            var data = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_027).EnumValue, input);

            Assert.True(Convert.ToDouble(data) == -15.94);

            var comp = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_027).EnumValue, -15.94).AsSpan().Slice(1, 2);

            Assert.Equal(input, comp.ToArray());
        }


        [Fact]
        public void TestDpt9_027_3()
        {
            var input = new byte[] { 0xFF, 0x80 };
            var data = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_027).EnumValue, input);

            Assert.True(Convert.ToDouble(data) == -459.6d);

            var comp = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_027).EnumValue, -500d).AsSpan().Slice(1, 2);

            Assert.Equal(new byte[] { 0xAA, 0x64 }, comp.ToArray());
        }


        [Fact]
        public void TestDpt9_027_4()
        {
            var translator = new Dpt9027TypeTranslator();
            Assert.Equal(-459.6m, translator.ValidateMinMax(-470.6m));
        }

        [Fact]
        public void TestDpt9_027_5()
        {
            var translator = new Dpt9027TypeTranslator();
            Assert.Equal(670760.96m, translator.ValidateMinMax(670769.96m));
        }

        [Fact]
        public void TestDpt9_028_1()
        {
            var input = new byte[] { 0x0d, 0xf2 };
            var data = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_028).EnumValue, input);

            Assert.True(Convert.ToDouble(data) == 30.44d);

            var comp = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_028).EnumValue, 30.44).AsSpan().Slice(1, 2);

            Assert.Equal(input, comp.ToArray());
        }

        [Fact]
        public void TestDpt9_028_2()
        {
            var input = new byte[] { 0x81, 0xc6 };
            var data = DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_028).EnumValue, input);

            Assert.True(Convert.ToDouble(data) == 0);

            var comp = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_028).EnumValue, -15.94).AsSpan().Slice(1, 2);

            Assert.Equal(new byte[] {0,0}, comp.ToArray());
        }


        [Fact]
        public void TestDpt9_028_3()
        {
            var translator = new Dpt9028TypeTranslator();
            Assert.Equal(0, translator.ValidateMinMax(-470.6m));
        }

        [Fact]
        public void TestDpt9_028_4()
        {
            var translator = new Dpt9028TypeTranslator();
            Assert.Equal(670760.96m, translator.ValidateMinMax(670769.96m));
        }


        [Fact]
        public void Test_ToDatapointException1()
        {
            Assert.Throws<ToDataPointException>(() => DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_028).EnumValue, null));
        }
        [Fact]
        public void Test_FromDatapointException1()
        {
            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_028).EnumValue, null));
        }

        [Fact]
        public void Test_ToDatapointException2()
        {
            Assert.Throws<ToDataPointException>(() => DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_028).EnumValue, new object()));
        }
        [Fact]
        public void Test_FromDatapointException2()
        {
            Assert.Throws<FromDataPointException>(() => DptTranslator.Instance.FromDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt9Type.Dpt9_028).EnumValue, new byte[0]));
        }
    }
}
