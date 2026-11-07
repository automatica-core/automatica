using System.Text;
using P3.Driver.VkingBms.Driver;
using P3.Driver.VkingBms.Driver.Data;
using P3.Driver.VkingBms.Driver.Exception;
using Xunit;

namespace P3.Driver.VkingBms.Tests
{
    public class VkingProtocolTests
    {
        private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

        [Theory]
        [InlineData(1, 66, "~52014642E00201FD30\r")]
        [InlineData(2, 241, "~520246F1E00202FD1D\r")]
        [InlineData(16, 233, "~521046E9E00210FD18\r")]
        public void Command_ToByteArray_IsAsciiFrameWithLengthAndFrameChecksum(byte address, byte cid, string expected)
        {
            var command = new Command(address, cid);
            command.SetBody(new[] { address });

            Assert.Equal(expected, Encoding.ASCII.GetString(command.ToByteArray()));
            Assert.Equal(address, command.Address);
        }

        [Fact]
        public void Command_WithoutBody_HasZeroLength()
        {
            var frame = Encoding.ASCII.GetString(new Command(1, 66).ToByteArray());

            Assert.StartsWith("~520146420000", frame);
            Assert.EndsWith("\r", frame);
            Assert.Equal(1 + 12 + 4 + 1, frame.Length);
        }

        [Theory]
        [InlineData(0, 0x0000)]
        [InlineData(2, 0xE002)]
        [InlineData(0x12, 0xD012)]
        [InlineData(0xFFF, 0x3FFF)]
        public void CalLCheckSum_PutsTheNibbleChecksumIntoTheTopNibble(int length, int expected)
        {
            Assert.Equal((ushort)expected, Util.CalLCheckSum((ushort)length));
        }

        [Fact]
        public void CalFrameCheckSum_IsTwosComplementOfTheByteSum()
        {
            var data = Ascii("52014642E00201");
            var sum = 0;
            foreach (var b in data)
            {
                sum += b;
            }

            Assert.Equal((ushort)(~sum + 1), Util.CalFrameCheckSum(data));
            Assert.Equal(Util.CalFrameCheckSum(data), Util.CalFrameCheckSum(Ascii("~52014642E00201FD30\r"), 1, data.Length));
        }

        [Fact]
        public void CalFrameCheckSum_RangeOutsideTheBuffer_IsZero()
        {
            Assert.Equal(0, Util.CalFrameCheckSum(new byte[4], 2, 3));
            Assert.Equal(0, Util.CalFrameCheckSum(null, 0, 1));
        }

        [Theory]
        [InlineData("7F", 0, 0x7F)]
        [InlineData("ff", 0, 0xFF)]
        [InlineData("x0A", 1, 0x0A)]
        [InlineData("ZZ", 0, 0)]
        [InlineData("1", 0, 0)]
        public void GetByte_ParsesTwoHexChars(string message, int index, int expected)
        {
            Assert.Equal((byte)expected, Util.GetByte(Ascii(message), index));
        }

        [Theory]
        [InlineData("FF9C", -100)]
        [InlineData("7FFF", short.MaxValue)]
        [InlineData("8000", short.MinValue)]
        [InlineData("12", 0)]
        [InlineData("GGGG", 0)]
        public void GetShort_IsSignedHex(string message, int expected)
        {
            Assert.Equal((short)expected, Util.GetShort(Ascii(message), 0));
        }

        [Theory]
        [InlineData("FFFF", ushort.MaxValue)]
        [InlineData("0CE4", 3300)]
        [InlineData("0CE", 0)]
        public void GetUShort_IsUnsignedHex(string message, int expected)
        {
            Assert.Equal((ushort)expected, Util.GetUShort(Ascii(message), 0));
        }

        [Fact]
        public void GetIntAndUint_AreEightHexChars()
        {
            Assert.Equal(-1, Util.GetInt(Ascii("FFFFFFFF"), 0));
            Assert.Equal(uint.MaxValue, Util.GetUint(Ascii("FFFFFFFF"), 0));
            Assert.Equal(0, Util.GetInt(Ascii("FFFFFFF"), 0));
            Assert.Equal((sbyte)-1, Util.GetsByte(Ascii("FF"), 0));
            Assert.Equal('A', Util.GetChar(Ascii("41"), 0));
            Assert.Equal(' ', Util.GetChar(Ascii("4"), 0));
        }

        [Fact]
        public void ResponseSuccess_IsRtnZero()
        {
            Assert.True(Util.CheckResponseSuccess(Ascii("~5201460000")));
            Assert.False(Util.CheckResponseSuccess(Ascii("~5201460200")));
            Assert.Equal(2, Util.GetCmdType(Ascii("~5201460200")));
        }

        private const string AnalogPayload =
            "01" +                              // address
            "03" + "0CE4" + "0CE5" + "0CDA" +   // 3 cells: 3300, 3301, 3290 mV
            "02" + "001E" + "0041" +            // 2 temperatures: 30 - 40 = -10, 65 - 40 = 25
            "FF9C" +                            // current -100
            "2706" +                            // voltage 9990
            "1388" + "02" +                     // remaining capacity 5000, user defined byte
            "2710" +                            // full capacity 10000
            "0064" +                            // cycles 100
            "50" + "63" +                       // soc 80, soh 99
            "0001" + "0020" + "0002";           // balance 1, humidity, balance 2

        [Fact]
        public void AnalogDataResponse_ParsesTheAnalogBlock()
        {
            var response = new AnalogDataResponse(Ascii("~520146" + "00" + "E0C6" + AnalogPayload + "FFFF\r"));

            Assert.Equal(0x52, response.Version);
            Assert.Equal(1, response.PackAddress);
            Assert.Equal(0, response.Rtn);
            Assert.Equal(1, response.Address);
            Assert.Equal(new ushort[] { 3300, 3301, 3290 }, response.CellVoltages);
            Assert.Equal(new short[] { -10, 25 }, response.Temperatures);
            Assert.Equal(-100, response.Current);
            Assert.Equal(9990, response.Voltage);
            Assert.Equal(5000, response.RemainingCapacity);
            Assert.Equal(10000, response.FullCapacity);
            Assert.Equal(100, response.CycleNumber);
            Assert.Equal(80, response.Soc);
            Assert.Equal(99, response.Soh);
            Assert.Equal(1, response.BalanceState1);
            Assert.Equal(0x20, response.Humidity);
            Assert.Equal(2, response.BalanceState2);
        }

        [Fact]
        public void AnalogDataResponse_TruncatedFrame_DoesNotThrow()
        {
            var response = new AnalogDataResponse(Ascii("~520146" + "00" + "E0C6" + "0103" + "0CE4"));

            Assert.Equal(3, response.CellVoltages.Length);
            Assert.Equal(3300, response.CellVoltages[0]);
            Assert.Equal(0, response.CellVoltages[2]);
            Assert.Equal(0, response.Voltage);
        }

        [Fact]
        public void VersionIdResponse_ReadsThirtyAsciiChars()
        {
            var version = new StringBuilder();
            foreach (var c in "VK-BMS-1.2".PadRight(30))
            {
                version.Append(((byte)c).ToString("X2"));
            }

            var response = new VersionIdResponse(Ascii("~520146" + "00" + "E03C" + version + "FFFF\r"));

            Assert.Equal("VK-BMS-1.2".PadRight(30), response.VersionId);
        }

        [Fact]
        public void Exceptions_CrcIsADataReadException()
        {
            Assert.IsAssignableFrom<DataReadException>(new InvalidCrcException("x"));
            Assert.IsAssignableFrom<System.Exception>(new DataReadException());
        }
    }
}
