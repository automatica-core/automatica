using System.Net;
using System.Net.NetworkInformation;
using P3.Driver.Matter.Discovery;

namespace P3.Driver.Matter.Tests
{
    /// <summary>What the bridge announces in a Docker container with host networking (the Synology case).</summary>
    public sealed class MdnsNetworkTests
    {
        private static MdnsInterface Nic(string name, params string[] addresses) =>
            Nic(name, NetworkInterfaceType.Ethernet, OperationalStatus.Up, addresses);

        private static MdnsInterface Nic(string name, NetworkInterfaceType type, OperationalStatus status, params string[] addresses) =>
            new(name, type, status, new byte[] { 0x00, 0x11, 0x32, 0xBD, 0x64, 0x5B }, addresses.Select(IPAddress.Parse).ToList());

        private static readonly MdnsInterface[] SynologyHost =
        {
            Nic("lo", NetworkInterfaceType.Loopback, OperationalStatus.Up, "127.0.0.1", "::1"),
            Nic("docker0", "172.17.0.1", "fe80::42:b8ff:fe37:d1bf"),
            Nic("veth1a2b3c", "fe80::545f:7cff:fe59:ecf4"),
            Nic("docker-6f1e", "172.18.0.1", "fe80::44d5:8ff:fe35:e180"),
            Nic("eth0", "192.168.8.15", "fe80::211:32ff:febd:645b", "fd55:6015:19a:9b55:211:32ff:febd:645b"),
            Nic("eth1", NetworkInterfaceType.Ethernet, OperationalStatus.Down, "169.254.1.1")
        };

        [Fact]
        public void LeavesOutLoopbackDownAndContainerInterfaces()
        {
            var names = MdnsNetwork.SelectInterfaces(SynologyHost).Select(n => n.Name);
            Assert.Equal(new[] { "eth0" }, names);
        }

        [Fact]
        public void AnnouncesRoutableIpv6FirstThenLinkLocalThenIpv4()
        {
            var addresses = MdnsNetwork.SelectAddresses(MdnsNetwork.SelectInterfaces(SynologyHost)).Select(a => a.ToString());
            Assert.Equal(new[] { "fd55:6015:19a:9b55:211:32ff:febd:645b", "fe80::211:32ff:febd:645b", "192.168.8.15" }, addresses);
        }

        [Fact]
        public void KeepsOpenVSwitchInterfaces()
        {
            // Synology names the LAN port ovs_eth0 once Virtual Machine Manager is installed
            var nics = new[] { Nic("ovs_eth0", "192.168.8.15"), Nic("docker0", "172.17.0.1") };
            Assert.Equal(new[] { "ovs_eth0" }, MdnsNetwork.SelectInterfaces(nics).Select(n => n.Name));
        }

        [Fact]
        public void FallsBackToAllUpInterfacesWhenOnlyVirtualOnesExist()
        {
            // bridge networking: the container only sees its own veth end
            var nics = new[] { Nic("veth0", "172.17.0.5"), Nic("lo", NetworkInterfaceType.Loopback, OperationalStatus.Up, "127.0.0.1") };
            Assert.Equal(new[] { "veth0" }, MdnsNetwork.SelectInterfaces(nics).Select(n => n.Name));
        }

        [Fact]
        public void HostNameIsTheMacInUpperCaseHex()
        {
            Assert.Equal("001132BD645B",MdnsNetwork.HostLabel(MdnsNetwork.SelectInterfaces(SynologyHost)));
        }

        [Fact]
        public void HostNameIsRandomWithoutAMac()
        {
            var nics = new[] { new MdnsInterface("eth0", NetworkInterfaceType.Ethernet, OperationalStatus.Up, Array.Empty<byte>(), new[] { IPAddress.Parse("10.0.0.2") }) };
            Assert.Matches("^[0-9A-F]{16}$", MdnsNetwork.HostLabel(nics));
        }
    }
}
