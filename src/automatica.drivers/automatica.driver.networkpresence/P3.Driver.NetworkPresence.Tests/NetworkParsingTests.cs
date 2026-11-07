using System.Linq;
using System.Net;
using P3.Driver.NetworkPresence.Driver.Network;
using Xunit;

namespace P3.Driver.NetworkPresence.Tests
{
    public class NetworkParsingTests
    {
        private const string ProcNetArp =
            "IP address       HW type     Flags       HW address            Mask     Device\n" +
            "192.168.1.1      0x1         0x2         a4:2b:b0:11:22:33     *        eth0\n" +
            "192.168.1.23     0x1         0x2         DA:A1:19:00:AB:CD     *        eth0\n" +
            "192.168.1.40     0x1         0x0         00:00:00:00:00:00     *        eth0\n" +
            "192.168.1.41     0x1         0x6         3c:22:fb:01:02:03     *        eth0\n" +
            "garbage line\n";

        [Fact]
        public void ParseProcNetArp_ReadsCompleteAndIncompleteEntries()
        {
            var entries = NeighborTableParser.ParseProcNetArp(ProcNetArp);

            Assert.Equal(4, entries.Count);
            Assert.Equal(new NeighborEntry(IPAddress.Parse("192.168.1.1"), "A4:2B:B0:11:22:33", true), entries[0]);
            Assert.Equal("DA:A1:19:00:AB:CD", entries[1].Mac);
            Assert.True(entries[1].Complete);
            // failed / incomplete resolution: all-zero MAC, flags 0x0
            Assert.Null(entries[2].Mac);
            Assert.False(entries[2].Complete);
            // permanent + complete (0x6) counts as complete
            Assert.True(entries[3].Complete);
        }

        [Fact]
        public void ParseArpCommand_Windows()
        {
            const string output = "\r\nInterface: 192.168.1.5 --- 0x7\r\n  Internet Address      Physical Address      Type\r\n" +
                                  "  192.168.1.1           a4-2b-b0-11-22-33     dynamic   \r\n" +
                                  "  192.168.1.255         ff-ff-ff-ff-ff-ff     static    \r\n" +
                                  "  224.0.0.22            01-00-5e-00-00-16     static    \r\n";

            var entries = NeighborTableParser.ParseArpCommand(output);

            Assert.Equal(2, entries.Count); // broadcast is dropped
            Assert.Equal("A4:2B:B0:11:22:33", entries[0].Mac);
            Assert.Equal(IPAddress.Parse("224.0.0.22"), entries[1].Address);
        }

        [Fact]
        public void ParseArpCommand_BsdWithShortOctets()
        {
            const string output = "? (192.168.1.23) at da:a1:19:0:ab:cd on en0 ifscope [ethernet]\n" +
                                  "? (192.168.1.40) at (incomplete) on en0 ifscope [ethernet]\n";

            var entry = Assert.Single(NeighborTableParser.ParseArpCommand(output));

            Assert.Equal(IPAddress.Parse("192.168.1.23"), entry.Address);
            Assert.Equal("DA:A1:19:00:AB:CD", entry.Mac);
        }

        [Theory]
        [InlineData("aa:bb:cc:dd:ee:ff", "AA:BB:CC:DD:EE:FF")]
        [InlineData("AA-BB-CC-DD-EE-FF", "AA:BB:CC:DD:EE:FF")]
        [InlineData("aabb.ccdd.eeff", "AA:BB:CC:DD:EE:FF")]
        [InlineData(" aabbccddeeff ", "AA:BB:CC:DD:EE:FF")]
        [InlineData("00:00:00:00:00:00", null)]
        [InlineData("ff:ff:ff:ff:ff:ff", null)]
        [InlineData("aa:bb:cc", null)]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void NormalizeMac(string input, string expected)
        {
            Assert.Equal(expected, NeighborEntry.NormalizeMac(input));
        }

        [Theory]
        [InlineData("DA:A1:19:00:AB:CD", true)]
        [InlineData("A6:00:00:00:00:01", true)]
        [InlineData("A4:2B:B0:11:22:33", false)]
        [InlineData("3C:22:FB:01:02:03", false)]
        public void IsRandomized_LocallyAdministeredBit(string mac, bool randomized)
        {
            Assert.Equal(randomized, NeighborEntry.IsRandomized(mac));
        }

        [Fact]
        public void Subnet_ParsesAndEnumeratesHosts()
        {
            Assert.True(Subnet.TryParse("192.168.1.77/24", out var subnet));

            Assert.Equal("192.168.1.0/24", subnet.ToString());
            var hosts = subnet.Hosts().ToList();
            Assert.Equal(254, hosts.Count);
            Assert.Equal(IPAddress.Parse("192.168.1.1"), hosts.First());
            Assert.Equal(IPAddress.Parse("192.168.1.254"), hosts.Last());
            Assert.True(subnet.Contains(IPAddress.Parse("192.168.1.200")));
            Assert.False(subnet.Contains(IPAddress.Parse("192.168.2.1")));
        }

        [Fact]
        public void Subnet_WithoutPrefix_IsA24()
        {
            Assert.True(Subnet.TryParse("10.0.5.9", out var subnet));
            Assert.Equal("10.0.5.0/24", subnet.ToString());
        }

        [Fact]
        public void Subnet_TooBigToSweep_OnlySweepsThe24AroundTheNetworkAddress()
        {
            Assert.True(Subnet.TryParse("10.0.0.0/16", out var subnet));

            Assert.Equal(254, subnet.Hosts().Count());
        }

        [Fact]
        public void Subnet_22_SweepsAllHosts()
        {
            Assert.True(Subnet.TryParse("192.168.4.0/22", out var subnet));

            Assert.Equal(1022, subnet.Hosts().Count());
        }

        [Theory]
        [InlineData("fe80::1/64")]
        [InlineData("192.168.1.0/33")]
        [InlineData("not an ip")]
        [InlineData("")]
        public void Subnet_Invalid(string text)
        {
            Assert.False(Subnet.TryParse(text, out _));
        }

        [Fact]
        public void Subnet_ParseList_SkipsInvalidAndDuplicates()
        {
            var list = Subnet.ParseList("192.168.1.0/24, nonsense;192.168.1.5/24 10.1.0.0/24");

            Assert.Equal(new[] { "192.168.1.0/24", "10.1.0.0/24" }, list.Select(s => s.ToString()));
        }
    }
}
