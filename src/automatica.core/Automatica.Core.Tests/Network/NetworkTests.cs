using Automatica.Core.Driver.Utility.Network;
using Xunit;

namespace Automatica.Core.Tests.Network
{
    public class NetworkTests
    {
        [Fact]
        public void TestGetLocalIpAddress()
        {
            var local = NetworkHelper.GetLocalIpAddress();

            Assert.NotNull(local);
        }

        [Fact]
        public void TestFreeTcpPort()
        {
            var port = NetworkHelper.GetFreeTcpPort();

            Assert.True(port >= 0);
            Assert.True(port <= ushort.MaxValue);
        }

        [Fact]
        public void TestGetActiveIp()
        {
            var ip = NetworkHelper.GetActiveIp();

            Assert.NotNull(ip);
        }

        [Fact]
        public void TestGetIpAddresses()
        {
            var ips = NetworkHelper.GetIpAddresses();

            Assert.NotNull(ips);
            Assert.True(ips.Length > 0);
        }
    }
}
