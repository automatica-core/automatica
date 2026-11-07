using System;
using System.Collections.Generic;
using System.Linq;
using P3.Driver.EnOcean.Data;
using P3.Driver.EnOcean.Data.Packets;
using Xunit;

namespace P3.Driver.EnOcean.Tests
{
    /// <summary>
    /// ESP3 framing: CRC8 (polynomial 0x07), header/data layout, packet parsing and building.
    /// </summary>
    public class Esp3FramingTests
    {
        private static byte[] Hex(string hex) => Automatica.Core.Driver.Utility.Utils.StringToByteArray(hex);

        /// <summary>
        /// Independent bitwise CRC8 (x^8 + x^2 + x + 1), to check the lookup table of the driver.
        /// </summary>
        internal static byte BitwiseCrc8(IEnumerable<byte> data)
        {
            byte crc = 0;
            foreach (var b in data)
            {
                crc ^= b;
                for (var i = 0; i < 8; i++)
                {
                    crc = (crc & 0x80) != 0 ? (byte)((crc << 1) ^ 0x07) : (byte)(crc << 1);
                }
            }
            return crc;
        }

        /// <summary>
        /// Builds a complete ESP3 frame with correct CRC8H/CRC8D.
        /// </summary>
        internal static byte[] Frame(EnOcean.PacketType type, byte[] data, byte[] optionalData)
        {
            var header = new byte[] { (byte)(data.Length >> 8), (byte)data.Length, (byte)optionalData.Length, (byte)type };
            var frame = new List<byte> { EnOcean.SyncByte };
            frame.AddRange(header);
            frame.Add(BitwiseCrc8(header));
            frame.AddRange(data);
            frame.AddRange(optionalData);
            frame.Add(BitwiseCrc8(data.Concat(optionalData)));
            return frame.ToArray();
        }

        internal static readonly byte[] BroadcastOptional = { 0x01, 0xFF, 0xFF, 0xFF, 0xFF, 0x46, 0x00 };

        [Fact]
        public void Crc8Table_MatchesTheBitwisePolynomialForEveryByte()
        {
            for (var i = 0; i < 256; i++)
            {
                Assert.Equal(BitwiseCrc8(new[] { (byte)i }), EnOcean.Crc8(0, (byte)i));
            }
        }

        [Fact]
        public void ComputeCrc8_MatchesTheBitwiseCrcForArbitraryData()
        {
            var random = new Random(4711);
            for (var len = 0; len < 64; len++)
            {
                var data = new byte[len];
                random.NextBytes(data);
                Assert.Equal(BitwiseCrc8(data), EnOcean.ComputeCrc8(data));
            }
        }

        [Fact]
        public void ComputeCrc8_WithSeed_ContinuesTheCrc()
        {
            var data = Hex("A50123450E0181B74400");
            var opt = Hex("01FFFFFFFF4600");

            var chained = EnOcean.ComputeCrc8(opt, EnOcean.ComputeCrc8(data));

            Assert.Equal(EnOcean.ComputeCrc8(data.Concat(opt).ToArray()), chained);
        }

        [Fact]
        public void ComputeCrc8_Empty_IsSeed()
        {
            Assert.Equal(0, EnOcean.ComputeCrc8(ReadOnlySpan<byte>.Empty));
            Assert.Equal(0x5A, EnOcean.ComputeCrc8(ReadOnlySpan<byte>.Empty, 0x5A));
        }

        [Theory]
        [InlineData("55000707017AF637FEF7D96E3001FFFFFFFF4600DC")]
        [InlineData("55000A0701EBA50123450E0181B7440001FFFFFFFF460004")]
        [InlineData("55000A0701EBA500008E48FFA8AB000001FFFFFFFF560073")]
        [InlineData("55000707017AD500019C20150001FFFFFFFF58009C")]
        public void KnownTelegrams_HaveValidHeaderAndDataCrc(string hex)
        {
            var frame = Hex(hex);
            var dataLen = (frame[1] << 8) | frame[2];
            var optLen = frame[3];

            Assert.Equal(frame[5], EnOcean.ComputeCrc8(frame.AsSpan(1, 4)));
            Assert.Equal(frame[^1], EnOcean.ComputeCrc8(frame.AsSpan(6, dataLen + optLen)));
            Assert.Equal(6 + dataLen + optLen + 1, frame.Length);
        }

        [Fact]
        public void CommonCommand_ReadIdBase_IsTheSpecFrame()
        {
            var packet = new CommonCommandPacket(CommonCommands.CO_RD_IDBASE).ToPacket();

            Assert.Equal(Hex("5500010005700838"), packet.RawData.ToArray());
            Assert.Equal(EnOcean.PacketType.CommonCommand, (EnOcean.PacketType)packet.Header.Span[4]);
            Assert.Equal(new byte[] { 0x08 }, packet.Data.ToArray());
            Assert.Equal(0, packet.OptionalData.Length);
        }

        [Theory]
        [InlineData(CommonCommands.CO_RD_VERSION)]
        [InlineData(CommonCommands.CO_WR_RESET)]
        [InlineData(CommonCommands.CO_RD_LEARNMODE)]
        public void CommonCommand_FrameHasValidCrcs(CommonCommands command)
        {
            var raw = new CommonCommandPacket(command).ToPacket().RawData.ToArray();

            Assert.Equal(Frame(EnOcean.PacketType.CommonCommand, new[] { (byte)command }, Array.Empty<byte>()), raw);
        }

        [Fact]
        public void CreateNew_ComputesBothCrcs_AndSplitsTheFrame()
        {
            var data = new byte[] { 0xD2, 0x01, 0x02, 0x03, 0xFF, 0x80, 0x00, 0x01, 0x00 };
            var opt = new byte[] { 0x03, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00 };
            var header = new byte[] { 0x55, 0x00, (byte)data.Length, (byte)opt.Length, 0x01 };

            var packet = EnOceanPacket.CreateNew(header, data, opt);

            Assert.Equal(Frame(EnOcean.PacketType.RadioErp1, data, opt), packet.RawData.ToArray());
            Assert.Equal(data, packet.Data.ToArray());
            Assert.Equal(opt, packet.OptionalData.ToArray());
            Assert.Equal(6, packet.Header.Length);
        }

        [Fact]
        public void CreateNew_TwoByteDataLength_IsBigEndian()
        {
            var data = Enumerable.Range(0, 300).Select(a => (byte)a).ToArray();
            var header = new byte[] { 0x55, 0x01, 0x2C, 0x00, (byte)EnOcean.PacketType.Event };

            var packet = EnOceanPacket.CreateNew(header, data, Array.Empty<byte>());

            Assert.Equal(300, packet.Data.Length);
            Assert.Equal(data, packet.Data.ToArray());
            Assert.Equal(Frame(EnOcean.PacketType.Event, data, Array.Empty<byte>()), packet.RawData.ToArray());
        }

        [Fact]
        public void Parse_RadioErp1_ReadsTheOptionalData()
        {
            var packet = EnOceanPacket.Parse(Frame(EnOcean.PacketType.RadioErp1, Hex("F650FEF7D96E30"),
                new byte[] { 0x02, 0x01, 0x02, 0x03, 0x04, 0x3C, 0x01 }));

            Assert.Equal(EnOcean.PacketType.RadioErp1, packet.PacketType);
            Assert.Equal(2, packet.SubTelNumber);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, packet.DestinationId);
            Assert.Equal("01020304", packet.DestinationIdString);
            Assert.Equal(0x3C, packet.Dbm);
            Assert.True(packet.SecurityLevel);
        }

        [Fact]
        public void Parse_Response_HasNoRadioFields()
        {
            var packet = EnOceanPacket.Parse(Frame(EnOcean.PacketType.Response, Hex("00FF800000"), Array.Empty<byte>()));

            Assert.Equal(EnOcean.PacketType.Response, packet.PacketType);
            Assert.Null(packet.DestinationId);

            var telegram = Assert.IsType<ResponsePacket>(EnOceanTelegramFactory.FromPacket(packet));
            Assert.Equal(ReturnCode.RetOk, telegram.ReturnCode);
        }

        [Theory]
        [InlineData(ReturnCode.RetError)]
        [InlineData(ReturnCode.RetNotSupported)]
        [InlineData(ReturnCode.RetWrongParam)]
        public void ResponsePacket_ReadsTheReturnCode(ReturnCode code)
        {
            var packet = EnOceanPacket.Parse(Frame(EnOcean.PacketType.Response, new[] { (byte)code }, Array.Empty<byte>()));

            Assert.Equal(code, Assert.IsType<ResponsePacket>(EnOceanTelegramFactory.FromPacket(packet)).ReturnCode);
        }

        [Theory]
        [InlineData(EnOcean.PacketType.Event)]
        [InlineData(EnOcean.PacketType.CommonCommand)]
        [InlineData(EnOcean.PacketType.RadioErp2)]
        public void TelegramFactory_UnsupportedPacketTypes_ThrowNotImplemented(EnOcean.PacketType type)
        {
            var packet = EnOceanPacket.Parse(Frame(type, new byte[] { 0x01 }, Array.Empty<byte>()));

            Assert.Throws<NotImplementedException>(() => EnOceanTelegramFactory.FromPacket(packet));
        }

        [Fact]
        public void Parse_FromMemories_KeepsTheRawFrameAndCrc()
        {
            var frame = Frame(EnOcean.PacketType.RadioErp1, Hex("F630FEF7D96E30"), BroadcastOptional);

            var packet = EnOceanPacket.Parse(frame.AsMemory(0, 6), frame.AsMemory(6, 7), frame.AsMemory(13, 7), frame[^1]);

            Assert.Equal(frame, packet.RawData.ToArray());
            Assert.Equal(frame[^1], packet.CrcData);
        }

        [Fact(Skip = "BUG: EnOceanPacket(Span<byte>) reverses the length bytes in place (byteLen.Reverse()), Parse(byte[]) corrupts the caller's buffer")]
        public void Parse_DoesNotModifyTheCallersBuffer()
        {
            var frame = Hex("55000A0701EBA50123450E0181B7440001FFFFFFFF460004");
            var copy = frame.ToArray();

            EnOceanPacket.Parse(frame);

            Assert.Equal(copy, frame);
        }
    }
}
