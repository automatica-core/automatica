using System.Collections.Generic;
using P3.Driver.EBus.Wolf;
using Xunit;
using static P3.Driver.EBus.Tests.FakeBus;

namespace P3.Driver.EBus.Tests
{
    public class EBusProtocolTests
    {
        [Fact]
        public void Crc_MatchesTheEBusReferenceTelegram()
        {
            // ebusd documentation: 10 08 b5 11 01 01 → 89
            Assert.Equal(0x89, Crc.Crc8(Hex("10 08 b5 11 01 01")));
        }

        [Theory]
        // register reads seen on a Wolf bus
        [InlineData(14, "cc 0e 00")]
        [InlineData(759, "ce f7 02")]
        [InlineData(836, "57 44 03")]
        public void WolfReadRequest_HasTheRegisterCrc(int register, string expected)
        {
            Assert.Equal(Hex(expected), WolfRegister.ReadRequest((ushort)register));
        }

        [Theory]
        [InlineData("0b 02", 523)]
        [InlineData("02 0b 02", 523)]
        public void WolfReadResponse_IsLittleEndian(string response, int expected)
        {
            Assert.True(WolfRegister.TryParseReadResponse(Hex(response), out var value));
            Assert.Equal((ushort)expected, value);
        }

        [Fact]
        public void WolfReadResponse_8000IsNotAvailable()
        {
            Assert.True(WolfRegister.TryParseReadResponse(Hex("00 80"), out var value));
            Assert.Null(value);
            Assert.False(WolfRegister.TryParseReadResponse(Hex("01"), out _));
        }

        [Fact]
        public void Escape_RoundTrips()
        {
            var data = Hex("01 aa a9 02 aa");
            var escaped = EBusProtocol.Escape(data);

            Assert.Equal(Hex("01 a9 01 a9 00 02 a9 01"), escaped);
            Assert.DoesNotContain(EBusProtocol.Syn, escaped);
            Assert.Equal(data, EBusProtocol.Unescape(escaped));
        }

        [Theory]
        [InlineData(0x03, true)]
        [InlineData(0x10, true)]
        [InlineData(0xF1, true)]
        [InlineData(0xFF, true)]
        [InlineData(0x08, false)]
        [InlineData(0x50, false)]
        public void MasterAddresses(int address, bool master)
        {
            Assert.Equal(master, EBusProtocol.IsMaster((byte)address));
            Assert.Equal(!master, EBusProtocol.IsSlave((byte)address));
        }

        [Fact]
        public void Parse_WolfRegisterRead_WithAnswer()
        {
            var t = EBusTelegram.TryParse(Hex("f1 50 50 22 03 cc 0e 00 8d 00 02 0b 02 01 00"));

            Assert.NotNull(t);
            Assert.Equal(0xF1, t.Source);
            Assert.Equal(0x50, t.Target);
            Assert.Equal(0x5022, t.Command);
            Assert.Equal(Hex("cc 0e 00"), t.Data);
            Assert.Equal(Hex("0b 02"), t.Response);
        }

        [Fact]
        public void Parse_MasterSlaveWithLongAnswer()
        {
            var t = EBusTelegram.TryParse(Hex("70 51 50 14 07 03 33 22 4d 1e 00 6a ae 00 09 00 01 80 1a 00 80 00 64 00 6d 00"));

            Assert.Equal(Hex("00 01 80 1a 00 80 00 64 00"), t.Response);
        }

        [Fact]
        public void Parse_BrokenCrc_IsDropped()
        {
            Assert.Null(EBusTelegram.TryParse(Hex("03 fe 05 03 08 01 00 48 ff 44 1e 34 02 d6")));
            Assert.Null(EBusTelegram.TryParse(Hex("03 fe 05")));
            Assert.Null(EBusTelegram.TryParse(Hex("00")));
        }

        [Fact]
        public void HeatGeneratorStatus_FromTheBroadcast()
        {
            var t = EBusTelegram.TryParse(Hex("03 fe 05 03 08 01 00 48 ff 44 1e 34 02 d5"));
            var status = EBusBroadcasts.TryDecodeHeatGeneratorStatus(t);

            Assert.True(t.IsBroadcast);
            Assert.True(status.Running);
            Assert.True(status.CirculationPump);
            Assert.Equal(34.0, status.FlowTemperature);
            Assert.Equal(30.0, status.ReturnTemperature);
            Assert.Equal(52.0, status.HotWaterTemperature);
            Assert.Equal(2.0, status.OutsideTemperature);
        }

        [Fact]
        public void WolfFaultCode_FromTheBroadcast()
        {
            var t = EBusTelegram.TryParse(Hex("03 fe fe 01 0a 45 30 30 30 20 30 36 3a 32 39 ed"));

            Assert.Equal(0, EBusBroadcasts.TryDecodeWolfFaultCode(t));
            Assert.Null(EBusBroadcasts.TryDecodeHeatGeneratorStatus(t));
        }

        [Fact]
        public void BuildMasterPart_EscapesTheCrcToo()
        {
            var built = EBusProtocol.BuildMasterPart(0xFF, 0x08, 0x50, 0x22, WolfRegister.ReadRequest(14));
            var parsed = EBusTelegram.TryParse(built);

            Assert.Equal(0x5022, parsed.Command);
            Assert.Equal(new List<byte>(WolfRegister.ReadRequest(14)), parsed.Data);
        }
    }
}
