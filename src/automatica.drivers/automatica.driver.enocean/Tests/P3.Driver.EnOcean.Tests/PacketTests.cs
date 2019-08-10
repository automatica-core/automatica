using P3.Driver.EnOcean.Data;
using P3.Driver.EnOcean.Data.Packets;
using Xunit;

namespace P3.Driver.EnOcean.Tests
{
    public class PacketTests
    {
        [Fact]
        public void Test_PackageParse()
        {
            var byteData =
                Automatica.Core.Driver.Utility.Utils.StringToByteArray("55000A0701EBA500010948FF0000000001FFFFFFFF490066");

            var packet = EnOceanPacket.Parse(byteData);

            Assert.Equal(EnOcean.PacketType.RadioErp1, packet.PacketType);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, packet.DestinationId);
            Assert.Equal((byte)73, packet.Dbm);
            Assert.False(packet.SecurityLevel);
        }
        [Fact]
        public void Test_PackageParse2()
        {
            var byteData =
                Automatica.Core.Driver.Utility.Utils.StringToByteArray("55000A0701EBA500008E48FFA8AB000001FFFFFFFF560073");

            var packet = EnOceanPacket.Parse(byteData);

            Assert.Equal(EnOcean.PacketType.RadioErp1, packet.PacketType);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, packet.DestinationId);
            Assert.Equal((byte)86, packet.Dbm);
            Assert.False(packet.SecurityLevel);
        }
        [Fact]
        public void Test_PackageParse3()
        {
            var byteData =
                Automatica.Core.Driver.Utility.Utils.StringToByteArray("55000A0701EBA50000C148FFD677000001FFFFFFFF470091");

            var packet = EnOceanPacket.Parse(byteData);

            Assert.Equal(EnOcean.PacketType.RadioErp1, packet.PacketType);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, packet.DestinationId);
            Assert.Equal((byte)71, packet.Dbm);
            Assert.False(packet.SecurityLevel);
        }

        [Fact]
        public void Test_PackageParseLearnMode()
        {
            var byteData = Automatica.Core.Driver.Utility.Utils.StringToByteArray("55000707017AD500019C20150001FFFFFFFF58009C");

            var packet = EnOceanPacket.Parse(byteData);

            var radioErp1 = new RadioErp1Packet();
            radioErp1.FromPacket(packet);

            Assert.True(RadioErp1Packet.IsTechIn(radioErp1));
            Assert.Equal(EnOcean.PacketType.RadioErp1, packet.PacketType);
            Assert.False(packet.SecurityLevel);
        }



        [Fact]
        public void Test_PackageParseNoLearnMode()
        {
            var byteData = Automatica.Core.Driver.Utility.Utils.StringToByteArray("55000707017AD508019C20150001FFFFFFFF500088");

            var packet = EnOceanPacket.Parse(byteData);

            var radioErp1 = new RadioErp1Packet();
            radioErp1.FromPacket(packet);

            Assert.False(RadioErp1Packet.IsTechIn(radioErp1));
            Assert.Equal(EnOcean.PacketType.RadioErp1, packet.PacketType);
            Assert.False(packet.SecurityLevel);
        }
    }
}
