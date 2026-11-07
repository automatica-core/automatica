using System;
using P3.Driver.EnOcean.Data;
using P3.Driver.EnOcean.Data.Packets;
using Xunit;
using static P3.Driver.EnOcean.Tests.Esp3FramingTests;

namespace P3.Driver.EnOcean.Tests
{
    public class RadioErp1PacketTests
    {
        private static RadioErp1Packet Radio(params byte[] erp1)
        {
            var packet = EnOceanPacket.Parse(Frame(EnOcean.PacketType.RadioErp1, erp1, BroadcastOptional));
            return Assert.IsType<RadioErp1Packet>(EnOceanTelegramFactory.FromPacket(packet));
        }

        [Fact]
        public void Rps_HasOneDataByte()
        {
            var radio = Radio(0xF6, 0x50, 0xFE, 0xF7, 0xD9, 0x6E, 0x30);

            Assert.Equal(Rorg.Rps, radio.Rorg);
            Assert.Equal(new byte[] { 0x50 }, radio.Data.ToArray());
            Assert.Equal("FEF7D96E", radio.SenderIdString);
            Assert.Equal(0x30, radio.Status);
        }

        [Fact]
        public void FourBs_HasFourDataBytes()
        {
            var radio = Radio(0xA5, 0x01, 0x23, 0x45, 0x0E, 0x01, 0x81, 0xB7, 0x44, 0x00);

            Assert.Equal(Rorg.FourBs, radio.Rorg);
            Assert.Equal(new byte[] { 0x01, 0x23, 0x45, 0x0E }, radio.Data.ToArray());
            Assert.Equal("0181B744", radio.SenderIdString);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(6)]
        [InlineData(14)]
        public void Vld_DataLengthFollowsThePacketLength(int payloadLength)
        {
            // regression: VLD (D2) used to get a data length of 0, the payload was lost
            var erp1 = new byte[1 + payloadLength + 5];
            erp1[0] = 0xD2;
            for (var i = 0; i < payloadLength; i++)
            {
                erp1[1 + i] = (byte)(0x10 + i);
            }
            erp1[^5] = 0x05;
            erp1[^4] = 0x0A;
            erp1[^3] = 0x0B;
            erp1[^2] = 0x0C;
            erp1[^1] = 0x80;

            var radio = Radio(erp1);

            Assert.Equal(Rorg.Vld, radio.Rorg);
            Assert.Equal(payloadLength, radio.Data.Length);
            Assert.Equal(0x10, radio.Data.Span[0]);
            Assert.Equal((byte)(0x10 + payloadLength - 1), radio.Data.Span[^1]);
            Assert.Equal("050A0B0C", radio.SenderIdString);
            Assert.Equal(0x80, radio.Status);
        }

        [Fact]
        public void Ute_IsRecognizedWithItsSevenDataBytes()
        {
            var radio = Radio(0xD4, 0xA0, 0x02, 0x46, 0x00, 0x12, 0x01, 0xD2, 0x01, 0x94, 0xE3, 0xB9, 0x00);

            Assert.Equal(Rorg.Ute, radio.Rorg);
            Assert.Equal(7, radio.Data.Length);
            Assert.Equal("0194E3B9", radio.SenderIdString);
        }

        [Fact]
        public void TooShortPacket_Throws()
        {
            var packet = EnOceanPacket.Parse(Frame(EnOcean.PacketType.RadioErp1, new byte[] { 0xF6, 0x50, 0x01, 0x02, 0x03 }, BroadcastOptional));

            Assert.Throws<ArgumentException>(() => EnOceanTelegramFactory.FromPacket(packet));
        }

        [Theory]
        [InlineData(0x50)]
        [InlineData(0x00)]
        public void Rps_IsAlwaysTeachIn_ButNeverLrnTeachIn(byte data)
        {
            var radio = Radio(0xF6, data, 0xFE, 0xF7, 0xD9, 0x6E, 0x30);

            Assert.True(RadioErp1Packet.IsTechIn(radio));
            Assert.False(RadioErp1Packet.IsLrnTeachIn(radio));
        }

        [Theory]
        [InlineData(0x00, true)]  // LRN bit (DB0.3) cleared
        [InlineData(0x08, false)] // LRN bit set: data telegram
        [InlineData(0x09, false)]
        [InlineData(0xF7, true)]
        public void OneBs_TeachInFollowsTheLrnBit(byte db0, bool teachIn)
        {
            var radio = Radio(0xD5, db0, 0x01, 0x9C, 0x20, 0x15, 0x00);

            Assert.Equal(teachIn, RadioErp1Packet.IsLrnTeachIn(radio));
            Assert.Equal(teachIn, RadioErp1Packet.IsTechIn(radio));
        }

        [Theory]
        [InlineData(0x80, true)]  // regression: the old 0x16 mask missed this teach-in (A5-02-05 LRN type 1)
        [InlineData(0x00, true)]  // regression: ... and this one
        [InlineData(0x08, false)]
        [InlineData(0x0E, false)] // regression: ... and flagged this data telegram as teach-in
        [InlineData(0x10, true)]
        public void FourBs_TeachInFollowsTheLrnBitOfDb0(byte db0, bool teachIn)
        {
            var radio = Radio(0xA5, 0x08, 0x28, 0x46, db0, 0x01, 0x81, 0xB7, 0x44, 0x00);

            Assert.Equal(teachIn, RadioErp1Packet.IsLrnTeachIn(radio));
        }

        [Fact]
        public void Vld_IsNeverAnLrnTeachIn()
        {
            var radio = Radio(0xD2, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x00);

            Assert.False(RadioErp1Packet.IsLrnTeachIn(radio));
            Assert.False(RadioErp1Packet.IsTechIn(radio));
        }

        [Fact]
        public void ToPacket_UsesTheIdBaseAsSender_AndBroadcasts()
        {
            var telegram = new RadioErp1Packet(Rorg.Rps, new byte[] { 0x30 });
            telegram.SetIdBase(new byte[] { 0xFF, 0x80, 0x00, 0x01 });

            var packet = telegram.ToPacket();

            Assert.Equal(Frame(EnOcean.PacketType.RadioErp1, new byte[] { 0xF6, 0x30, 0xFF, 0x80, 0x00, 0x01, 0x30 },
                new byte[] { 0x03, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00 }), packet.RawData.ToArray());

            var roundTrip = Assert.IsType<RadioErp1Packet>(EnOceanTelegramFactory.FromPacket(EnOceanPacket.Parse(packet.RawData.ToArray())));
            Assert.Equal(Rorg.Rps, roundTrip.Rorg);
            Assert.Equal(new byte[] { 0x30 }, roundTrip.Data.ToArray());
            Assert.Equal("FF800001", roundTrip.SenderIdString);
            Assert.Equal("FFFFFFFF", roundTrip.Packet.DestinationIdString);
            Assert.Equal(3, roundTrip.Packet.SubTelNumber);
        }

        [Fact]
        public void ToPacket_FourBs_RoundTrips()
        {
            var telegram = new RadioErp1Packet(Rorg.FourBs, new byte[] { 0x01, 0x02, 0x03, 0x08 });
            telegram.SetIdBase(new byte[] { 0xFF, 0x80, 0x00, 0x7F });

            var roundTrip = Assert.IsType<RadioErp1Packet>(EnOceanTelegramFactory.FromPacket(EnOceanPacket.Parse(telegram.ToPacket().RawData.ToArray())));

            Assert.Equal(Rorg.FourBs, roundTrip.Rorg);
            Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x08 }, roundTrip.Data.ToArray());
            Assert.Equal("FF80007F", roundTrip.SenderIdString);
            Assert.False(RadioErp1Packet.IsLrnTeachIn(roundTrip));
        }
    }
}
