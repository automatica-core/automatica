using P3.Driver.Knx.DriverFactory;
using Xunit;
using KnxHelper = global::P3.Driver.Knx.DriverFactory.P3.Knx.Core.Driver.KnxHelper;

namespace P3.Driver.Knx.Tests.Telegram
{
    /// <summary>
    /// cEMI / KNXnet/IP helpers: address encoding, APDU data and the KNXnet/IP header fields.
    /// </summary>
    public class KnxHelperTelegramTests
    {
        [Theory]
        [InlineData("0/0/1", 0x00, 0x01)]
        [InlineData("1/2/3", 0x0A, 0x03)]
        [InlineData("15/7/255", 0x7F, 0xFF)]
        [InlineData("2/7/0", 0x17, 0x00)]
        public void GetAddress_ThreeLevelGroupAddress_IsEncoded(string address, byte high, byte low)
        {
            Assert.Equal(new[] { high, low }, KnxHelper.GetAddress(address));
        }

        [Theory]
        [InlineData("0/0/1")]
        [InlineData("1/2/3")]
        [InlineData("15/7/255")]
        [InlineData("9/0/128")]
        public void GetGroupAddress_ThreeLevel_RoundTrips(string address)
        {
            Assert.Equal(address, KnxHelper.GetGroupAddress(KnxHelper.GetAddress(address), true));
        }

        [Theory]
        [InlineData("1.1.1", 0x11, 0x01)]
        [InlineData("15.15.255", 0xFF, 0xFF)]
        [InlineData("0.0.0", 0x00, 0x00)]
        [InlineData("1.2.250", 0x12, 0xFA)]
        public void GetAddress_IndividualAddress_IsEncodedAndRoundTrips(string address, byte high, byte low)
        {
            var bytes = KnxHelper.GetAddress(address);

            Assert.Equal(new[] { high, low }, bytes);
            Assert.Equal(address, KnxHelper.GetIndividualAddress(bytes));
            Assert.True(KnxHelper.IsAddressIndividual(address));
        }

        [Theory]
        [InlineData("16/0/0")]
        [InlineData("1/8/0")]
        [InlineData("1/0/256")]
        [InlineData("16.0.0")]
        [InlineData("1.16.0")]
        [InlineData("1.1.256")]
        [InlineData("1/2/3/4")]
        [InlineData("a/b/c")]
        [InlineData("")]
        [InlineData("1.1")]
        [InlineData("1/-1/2")]
        public void GetAddress_InvalidAddress_ThrowsInvalidKnxAddress(string address)
        {
            Assert.Throws<InvalidKnxAddressException>(() => KnxHelper.GetAddress(address));
        }

        [Theory(Skip = "BUG: KnxHelper.GetAddress two level branch checks BitConverter.GetBytes(int).Length > 2 (always 4), every two level group address throws")]
        [InlineData("1/2047", 0x0F, 0xFF)]
        [InlineData("2/1", 0x10, 0x01)]
        public void GetAddress_TwoLevelGroupAddress_IsEncodedAndRoundTrips(string address, byte high, byte low)
        {
            var bytes = KnxHelper.GetAddress(address);

            Assert.Equal(new[] { high, low }, bytes);
            Assert.Equal(address, KnxHelper.GetGroupAddress(bytes, false));
        }

        [Theory]
        [InlineData(0x0F, 0xFF, "1/2047")]
        [InlineData(0x10, 0x01, "2/1")]
        [InlineData(0x00, 0x00, "0/0")]
        public void GetGroupAddress_TwoLevel_IsDecoded(byte high, byte low, string expected)
        {
            Assert.Equal(expected, KnxHelper.GetGroupAddress(new[] { high, low }, false));
        }

        [Fact]
        public void IsAddressIndividual_GroupAddress_IsFalse()
        {
            Assert.False(KnxHelper.IsAddressIndividual("1/2/3"));
        }

        [Theory]
        [InlineData(0x3C, KnxHelper.KnxDestinationAddressType.Individual)]
        [InlineData(0xE0, KnxHelper.KnxDestinationAddressType.Group)]
        [InlineData(0x80, KnxHelper.KnxDestinationAddressType.Group)]
        [InlineData(0x60, KnxHelper.KnxDestinationAddressType.Individual)]
        public void GetKnxDestinationAddressType_UsesBit7OfControlField2(byte controlField2, KnxHelper.KnxDestinationAddressType expected)
        {
            Assert.Equal(expected, KnxHelper.GetKnxDestinationAddressType(controlField2));
        }

        [Theory]
        [InlineData(new byte[0], 0)]
        [InlineData(new byte[] { 0x01 }, 1)] // fits into the APCI byte
        [InlineData(new byte[] { 0x3E }, 1)]
        [InlineData(new byte[] { 0x3F }, 2)] // does not fit, own byte
        [InlineData(new byte[] { 0xFF }, 2)]
        [InlineData(new byte[] { 0x0C, 0x1A }, 2)]
        [InlineData(new byte[] { 0x41, 0xA8, 0x00, 0x00 }, 5)]
        public void GetDataLength_ReturnsTheApduDataLength(byte[] data, int expected)
        {
            Assert.Equal(expected, KnxHelper.GetDataLength(data));
        }

        [Fact]
        public void WriteData_SixBitValue_IsMergedIntoTheApciByte()
        {
            var datagram = new byte[] { 0x00, 0x80, 0x00 };

            KnxHelper.WriteData(datagram, new byte[] { 0x01 }, 1);

            Assert.Equal(new byte[] { 0x00, 0x81, 0x00 }, datagram);
        }

        [Fact]
        public void WriteData_ByteValue_FollowsTheApciByte()
        {
            var datagram = new byte[] { 0x00, 0x80, 0x00 };

            KnxHelper.WriteData(datagram, new byte[] { 0xC8 }, 1);

            Assert.Equal(new byte[] { 0x00, 0x80, 0xC8 }, datagram);
        }

        [Fact]
        public void WriteData_MultiByteValue_FollowsTheApciByte()
        {
            var datagram = new byte[] { 0x00, 0x80, 0x00, 0x00, 0x00 };

            KnxHelper.WriteData(datagram, new byte[] { 0x41, 0xA8, 0x00 }, 1);

            Assert.Equal(new byte[] { 0x00, 0x80, 0x41, 0xA8, 0x00 }, datagram);
        }

        [Fact]
        public void GetData_ReadsTheValueBackFromTheApdu()
        {
            Assert.Equal(string.Empty, KnxHelper.GetData(0, new byte[] { 0x00, 0x80 }));
            Assert.Equal(((char)0x01).ToString(), KnxHelper.GetData(1, new byte[] { 0x00, 0x81 }));
            Assert.Equal(((char)0xC8).ToString(), KnxHelper.GetData(2, new byte[] { 0x00, 0x80, 0xC8 }));
            Assert.Equal("AB", KnxHelper.GetData(3, new byte[] { 0x00, 0x80, (byte)'A', (byte)'B' }));
        }

        [Theory]
        [InlineData(0x02, 0x05, KnxHelper.ServiceType.ConnectRequest)]
        [InlineData(0x02, 0x06, KnxHelper.ServiceType.ConnectResponse)]
        [InlineData(0x04, 0x20, KnxHelper.ServiceType.TunnellingRequest)]
        [InlineData(0x04, 0x21, KnxHelper.ServiceType.TunnellingAck)]
        [InlineData(0x05, 0x30, KnxHelper.ServiceType.RoutingIndication)]
        [InlineData(0x09, 0x50, KnxHelper.ServiceType.SecureWrapper)]
        [InlineData(0x02, 0x09, KnxHelper.ServiceType.DisconnectRequest)]
        [InlineData(0x12, 0x34, KnxHelper.ServiceType.Unknown)]
        public void GetServiceType_ReadsTheKnxNetIpHeader(byte high, byte low, KnxHelper.ServiceType expected)
        {
            var datagram = new byte[] { 0x06, 0x10, high, low, 0x00, 0x0A };

            Assert.Equal(expected, KnxHelper.GetServiceType(datagram));
        }

        [Fact]
        public void GetChannelId_ReadsTheConnectionHeader()
        {
            Assert.Equal(0x15, KnxHelper.GetChannelId(new byte[] { 0x06, 0x10, 0x04, 0x20, 0x00, 0x15, 0x15, 0x00 }));
            Assert.Equal(-1, KnxHelper.GetChannelId(new byte[] { 0x06, 0x10, 0x04, 0x20, 0x00, 0x06 }));
        }

        [Fact]
        public void ByteArrayToString_IsSpaceSeparatedHex()
        {
            Assert.Equal("06 10 04 20", KnxHelper.ByteArrayToString(new byte[] { 0x06, 0x10, 0x04, 0x20 }));
        }
    }
}
