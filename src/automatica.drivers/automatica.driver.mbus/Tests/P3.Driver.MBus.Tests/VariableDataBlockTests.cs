using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Driver.Utility;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.MBus.Frames;
using P3.Driver.MBus.Frames.VariableData;
using Xunit;

namespace P3.Driver.MBus.Tests
{
    public class VariableDataBlockTests
    {
        private static List<VariableDataBlock> Parse(string hex)
        {
            var frame = new VariableDataFrame();
            frame.ParseVariableDataFrame(Utils.StringToByteArray(hex), 0, NullLogger.Instance);
            return frame.DataBlocks;
        }

        private static VariableDataBlock Single(string hex) => Assert.Single(Parse(hex));

        [Theory]
        // energy Wh, 10^(nnn-3)
        [InlineData(0x00, Unit.EnergyW, -3)]
        [InlineData(0x03, Unit.EnergyW, 0)]
        [InlineData(0x07, Unit.EnergyW, 4)]
        // energy J, 10^nnn
        [InlineData(0x08, Unit.EnergyJ, 0)]
        [InlineData(0x0F, Unit.EnergyJ, 7)]
        // volume m3, 10^(nnn-6)
        [InlineData(0x10, Unit.Volume, -6)]
        [InlineData(0x13, Unit.Volume, -3)]
        [InlineData(0x17, Unit.Volume, 1)]
        // mass kg, 10^(nnn-3)
        [InlineData(0x18, Unit.Mass, -3)]
        [InlineData(0x1F, Unit.Mass, 4)]
        // power W, 10^(nnn-3)
        [InlineData(0x28, Unit.PowerW, -3)]
        [InlineData(0x2B, Unit.PowerW, 0)]
        // power J/h, 10^nnn
        [InlineData(0x30, Unit.PowerJh, 0)]
        [InlineData(0x37, Unit.PowerJh, 7)]
        // volume flow
        [InlineData(0x38, Unit.VolumeFlowh, -6)]
        [InlineData(0x3B, Unit.VolumeFlowh, -3)]
        [InlineData(0x40, Unit.VolumeFlowmin, -7)]
        [InlineData(0x48, Unit.VolumeFlows, -9)]
        [InlineData(0x50, Unit.MassFlow, -3)]
        // temperatures, 10^(nn-3)
        [InlineData(0x58, Unit.FlowTemperature, -3)]
        [InlineData(0x5B, Unit.FlowTemperature, 0)]
        [InlineData(0x5C, Unit.ReturnTemperature, -3)]
        [InlineData(0x5F, Unit.ReturnTemperature, 0)]
        [InlineData(0x60, Unit.TemperatureDifference, -3)]
        [InlineData(0x64, Unit.ExternalTemperature, -3)]
        [InlineData(0x68, Unit.Pressure, -3)]
        [InlineData(0x6B, Unit.Pressure, 0)]
        // durations, time points and specials
        [InlineData(0x20, Unit.OnTime, 0)]
        [InlineData(0x24, Unit.OnTime, 0)]
        [InlineData(0x70, Unit.OnTime, 0)]
        [InlineData(0x6C, Unit.TimePoint, 0)]
        [InlineData(0x6D, Unit.TimePoint, 0)]
        [InlineData(0x6E, Unit.UnitsForHca, 0)]
        [InlineData(0x6F, Unit.Reserved1, 0)]
        [InlineData(0x78, Unit.FabricationNumber, 0)]
        [InlineData(0x7A, Unit.BusAddress, 0)]
        [InlineData(0x7F, Unit.ManufacturerSpecific, 0)]
        [InlineData(0x79, Unit.Unknown, 0)]
        public void ValueInformationField_Parse_MapsPrimaryVifTable(byte vif, Unit unit, int multiplier)
        {
            var field = ValueInformationField.Parse(vif);

            Assert.Equal(unit, field.Unit);
            Assert.Equal(multiplier, field.Multiplier);
            Assert.False(field.HasExtension);
            Assert.Equal(vif, field.RawData);
        }

        [Fact]
        public void ValueInformationField_Parse_ExtensionBitIsStrippedFromTheCode()
        {
            var field = ValueInformationField.Parse(0x83);

            Assert.True(field.HasExtension);
            Assert.Equal(Unit.EnergyW, field.Unit);
            Assert.Equal(0, field.Multiplier);
        }

        [Theory]
        [InlineData(0x40, -9)]
        [InlineData(0x49, 0)]
        [InlineData(0x4F, 6)]
        public void ParseExtensionUnit_VoltRange(byte vife, int multiplier)
        {
            var vif = ValueInformationField.Parse(0xFD);
            vif.ParseExtensionUnit(ValueInformationField.Parse(vife));

            Assert.Equal(Unit.Volt, vif.Unit);
            Assert.Equal(multiplier, vif.Multiplier);
        }

        [Theory]
        [InlineData(0x50, -12)]
        [InlineData(0x5C, 0)]
        [InlineData(0x5F, 3)]
        public void ParseExtensionUnit_AmpereRange(byte vife, int multiplier)
        {
            var vif = ValueInformationField.Parse(0xFD);
            vif.ParseExtensionUnit(ValueInformationField.Parse(vife));

            Assert.Equal(Unit.Ampere, vif.Unit);
            Assert.Equal(multiplier, vif.Multiplier);
        }

        [Theory]
        [InlineData(0x00, DataFieldType.NoData, 0)]
        [InlineData(0x01, DataFieldType.Integer8Bit, 1)]
        [InlineData(0x02, DataFieldType.Integer16Bit, 2)]
        [InlineData(0x03, DataFieldType.Integer24Bit, 3)]
        [InlineData(0x04, DataFieldType.Integer32Bit, 4)]
        [InlineData(0x05, DataFieldType.Real32Bit, 4)]
        [InlineData(0x06, DataFieldType.Integer48Bit, 6)]
        [InlineData(0x07, DataFieldType.Integer64Bit, 8)]
        [InlineData(0x08, DataFieldType.SelectionForReadout, 0)]
        [InlineData(0x09, DataFieldType.Digit2Bcd, 1)]
        [InlineData(0x0A, DataFieldType.Digit4Bcd, 2)]
        [InlineData(0x0B, DataFieldType.Digit6Bcd, 3)]
        [InlineData(0x0C, DataFieldType.Digit8Bcd, 4)]
        [InlineData(0x0D, DataFieldType.VariableLength, -1)]
        [InlineData(0x0E, DataFieldType.Digit12Bcd, 6)]
        [InlineData(0x0F, DataFieldType.SpecialFunction, 1)]
        public void DataInformationField_Parse_DataFieldTypeAndLength(byte dif, DataFieldType type, int length)
        {
            var field = DataInformationField.Parse(dif);

            Assert.Equal(type, field.DataFieldType);
            Assert.Equal(length, field.DataFieldLength);
            Assert.False(field.HasExtension);
            Assert.False(field.LsbStorageNumber);
            Assert.Equal(DataInformationFunction.InstantaneousValue, field.Function);
        }

        [Fact]
        public void DataInformationField_Parse_ErrorStateFunction()
        {
            var field = DataInformationField.Parse(0x34);

            Assert.Equal(DataFieldType.Integer32Bit, field.DataFieldType);
            Assert.Equal(DataInformationFunction.ValueDurringErrorState, field.Function);
        }

        [Fact(Skip = "BUG: DIF function bits 01/10 are mapped to Minimum/Maximum, EN 13757-3 defines 01 = maximum, 10 = minimum (P3.Driver.MBus/Frames/VariableData/DataInformationField.cs:5-11, unused so far)")]
        public void DataInformationField_Parse_MaximumAndMinimumFunctionBits()
        {
            Assert.Equal(DataInformationFunction.MaximumValue, DataInformationField.Parse(0x14).Function);
            Assert.Equal(DataInformationFunction.MinimumValue, DataInformationField.Parse(0x24).Function);
        }

        [Fact]
        public void DataInformationFieldExtension_Parse_SplitsStorageTariffAndUnit()
        {
            // 1 1 10 0101: extension, device unit, tariff 2, storage 5
            var dife = DataInformationFieldExtension.Parse(0xE5);

            Assert.True(dife.HasExtension);
            Assert.True(dife.DeviceUnit);
            Assert.Equal(2, dife.Tariff);
            Assert.Equal(5, dife.StorageNumber);
        }

        [Fact]
        public void Parse_StorageNumber_CombinesDifLsbAndDifeBits()
        {
            // DIF 0xC4: extension + LSB storage, DIFE 0x03: storage bits 1-4 = 3 -> 1 | (3 << 1) = 7
            var block = Single("C4030301000000");

            Assert.Equal(7, block.StorageNumber);
            Assert.Equal(0, block.Tariff);
            Assert.Equal(1, block.Value);
        }

        [Fact]
        public void Parse_SameUnitTwiceWithSameStorage_SecondGetsTheNextStorageNumber()
        {
            var blocks = Parse("040301000000" + "040302000000");

            Assert.Equal(2, blocks.Count);
            Assert.Equal(0, blocks[0].StorageNumber);
            Assert.Equal(1, blocks[1].StorageNumber);
            Assert.Equal(2, blocks[1].Value);
        }

        [Theory]
        [InlineData("0103FF", (byte)255)]
        [InlineData("0203FFFF", (short)-1)]
        [InlineData("02033412", (short)0x1234)]
        [InlineData("0403FFFFFFFF", -1)]
        [InlineData("040378563412", 0x12345678)]
        [InlineData("0703FFFFFFFFFFFFFFFF", -1L)]
        [InlineData("07030100000000000080", long.MinValue + 1)]
        public void Parse_BinaryIntegers_AreLittleEndian(string hex, object expected)
        {
            Assert.Equal(expected, Single(hex).Value);
        }

        [Fact]
        public void Parse_Integer24Bit_Positive()
        {
            Assert.Equal(0x123456, Single("0303563412").Value);
        }

        [Fact(Skip = "BUG: 24 bit integers are not sign extended (16/32/48/64 bit are), FFFFFF reads as 16777215 (P3.Driver.MBus/Frames/VariableData/VariableDataBlock.cs:311-320)")]
        public void Parse_Integer24Bit_NegativeIsSignExtended()
        {
            Assert.Equal(-1, Single("0303FFFFFF").Value);
        }

        [Theory]
        [InlineData("0A033412", 1234d)]
        [InlineData("0B03563412", 123456d)]
        [InlineData("0B035634F2", -23456d)]
        public void Parse_Bcd_DecodesDigits(string hex, double expected)
        {
            Assert.Equal(expected, Single(hex).Value);
        }

        [Fact]
        public void Parse_BcdWithInvalidDigit_ValueStaysNull()
        {
            Assert.Null(Single("0A033A12").Value);
        }

        [Fact]
        public void DecodeBcd_EmptyOrUnsigned()
        {
            Assert.Null(VariableDataBlock.DecodeBcd(new byte[0], true));
            // without sign support an F nibble is not a digit
            Assert.Null(VariableDataBlock.DecodeBcd(new byte[] { 0x01, 0xF0 }, false));
            Assert.Equal(-1d, VariableDataBlock.DecodeBcd(new byte[] { 0x01, 0xF0 }, true));
        }

        [Fact]
        public void Parse_MultiplierIsApplied_ToBinaryValues()
        {
            // VIF 0x5A = flow temperature 10^-1 degC, 0x00E1 = 225 -> 22.5
            var block = Single("025AE100");

            Assert.Equal(Unit.FlowTemperature, block.ValueInformationField.Unit);
            Assert.Equal(22.5, (double)block.Value, 9);
        }

        [Fact]
        public void Parse_NoData_ValueIsNullAndConsumesOnlyDifAndVif()
        {
            var blocks = Parse("0003" + "010307");

            Assert.Equal(2, blocks.Count);
            Assert.Null(blocks[0].Value);
            Assert.Equal((byte)7, blocks[1].Value);
        }

        [Fact]
        public void Parse_SpecialFunction_ConsumesTheRestOfTheTelegram()
        {
            // 0x0F = manufacturer specific data follows until the end; the parser reads one byte as VIF first
            var block = Single("0F01020304");

            Assert.Equal(DataFieldType.SpecialFunction, block.DataInformationField.DataFieldType);
            Assert.Equal(new byte[] { 0x02, 0x03, 0x04 }, block.Data);
        }

        [Fact(Skip = "BUG: after DIF 0x0F the first manufacturer byte is consumed as a VIF (P3.Driver.MBus/Frames/VariableData/VariableDataBlock.cs:79-99)")]
        public void Parse_SpecialFunction_ManufacturerDataHasNoVif()
        {
            // EN 13757-3: DIF 0x0F is followed directly by the manufacturer specific data, there is no VIF
            var block = Single("0F01020304");

            Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04 }, block.Data);
        }

        [Theory]
        [InlineData("0D03C23412", 1234d)]     // LVAR C2 = positive BCD, 2 bytes
        [InlineData("0D03D23412", -1234d)]    // LVAR D2 = negative BCD, 2 bytes
        public void Parse_VariableLengthBcd(string hex, double expected)
        {
            Assert.Equal(expected, Single(hex).Value);
        }

        [Fact]
        public void Parse_VariableLengthBinary_IsLittleEndian()
        {
            // LVAR E3 = 3 byte binary
            var blocks = Parse("0D03E3010203" + "010307");

            Assert.Equal(2, blocks.Count);
            Assert.Equal(0x030201L, blocks[0].Value);
            Assert.Equal((byte)7, blocks[1].Value);
        }

        [Fact]
        public void Parse_VariableLengthReserved_ConsumesTheRest()
        {
            var blocks = Parse("0D03F5010203");

            var block = Assert.Single(blocks);
            Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, block.Data);
            Assert.Null(block.Value);
        }

        [Theory]
        [InlineData(0x00, 10, 0)]
        [InlineData(0x10, 10, 16)]
        [InlineData(0xBF, 300, 191)]
        [InlineData(0xC0, 10, 0)]
        [InlineData(0xC6, 10, 6)]
        [InlineData(0xD4, 10, 4)]
        [InlineData(0xE8, 10, 8)]
        [InlineData(0xF0, 10, 10)]
        [InlineData(0xFF, 3, 3)]
        public void GetVariableDataLength_FollowsTheLvarTable(byte lvar, int remaining, int expected)
        {
            Assert.Equal(expected, VariableDataBlock.GetVariableDataLength(lvar, remaining));
        }

        [Fact]
        public void Parse_TimePointTypeI_DateAndTimeWithSeconds()
        {
            // 6 byte type I: seconds 3, minutes 36, hours 14, day 27, month 6, year 15
            var block = Single("066D" + "0324CEFB1600");

            Assert.Equal(new System.DateTime(2015, 6, 27, 14, 36, 3), block.Value);
        }

        [Fact]
        public void Parse_TimePointTypeG_Date()
        {
            // 2 byte type G: day 27, month 6, year 15
            var block = Single("026C" + "FB16");

            Assert.Equal(new System.DateTime(2015, 6, 27), block.Value);
        }

        [Fact]
        public void Parse_TimePointTypeF_InvalidMonth_IsNull()
        {
            // month nibble 0x0D is out of range
            Assert.Null(Single("046D248EFB1D").Value);
        }

        [Fact]
        public void Parse_BlockWithoutVif_UsesTheDefaultUnknownVif()
        {
            // a lone DIF at the end of the telegram (filler)
            var block = Parse("0103" + "07" + "00").Last();

            Assert.Equal(Unit.Unknown, block.ValueInformationField.Unit);
        }
    }
}
