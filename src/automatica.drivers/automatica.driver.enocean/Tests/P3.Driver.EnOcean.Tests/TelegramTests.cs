using P3.Driver.EnOcean.Data;
using P3.Driver.EnOcean.Data.Packets;
using Xunit;

namespace P3.Driver.EnOcean.Tests
{
    public class TelegramTests
    {
        [Fact]
        public void Test_Telegram1()
        {
            var byteData = Automatica.Core.Driver.Utility.Utils.StringToByteArray("55000A0701EBA500008E48FFA8AB000001FFFFFFFF560073");

            var packet = EnOceanPacket.Parse(byteData);

            var telegram = EnOceanTelegramFactory.FromPacket(packet) as RadioErp1Packet;

            Assert.NotNull(telegram);

            Assert.IsType<RadioErp1Packet>(telegram);

            Assert.Equal(Rorg.FourBs, telegram.Rorg);
        }
    }
}
