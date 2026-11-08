using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace P3.Driver.Matter.Discovery
{
    /// <summary>One network interface as seen by the advertiser (a snapshot, so the selection is testable).</summary>
    public sealed record MdnsInterface(string Name, NetworkInterfaceType Type, OperationalStatus Status, byte[] PhysicalAddress, IReadOnlyList<IPAddress> Addresses);

    /// <summary>
    /// Picks the interfaces and addresses the bridge announces. Without this, every IPv4 address and only the IPv6
    /// link-local ones were announced, including those of Docker bridges and veth pairs (host networking). Alexa,
    /// Google and Apple talk Matter over IPv6, keep only a few of the announced addresses and then could not reach
    /// the bridge.
    /// </summary>
    public static class MdnsNetwork
    {
        // container, VM and VPN plumbing that a controller on the LAN can never reach
        private static readonly string[] VirtualPrefixes =
        {
            "docker", "veth", "br-", "virbr", "cni", "flannel", "cali", "vxlan", "kube", "podman", "lxc", "lxdbr",
            "vmnet", "vboxnet", "tun", "tap", "wg", "zt", "tailscale", "utun", "hassio"
        };

        public static bool IsVirtual(MdnsInterface nic)
        {
            if (nic.Type is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
            {
                return true;
            }

            var name = nic.Name ?? "";
            return VirtualPrefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>The interfaces to listen and announce on: up, not loopback and not container/VPN plumbing (all up ones if that leaves none).</summary>
        public static IReadOnlyList<MdnsInterface> SelectInterfaces(IEnumerable<MdnsInterface> all)
        {
            var up = all.Where(n => n.Status == OperationalStatus.Up && n.Type != NetworkInterfaceType.Loopback && n.Addresses.Any(IsUsable)).ToList();
            var real = up.Where(n => !IsVirtual(n)).ToList();
            return real.Count > 0 ? real : up;
        }

        /// <summary>
        /// The addresses to announce: routable IPv6 (ULA, global) first, then IPv6 link-local, then IPv4.
        /// Controllers keep only the first few addresses (the Matter SDK keeps 5) and prefer IPv6.
        /// </summary>
        public static IReadOnlyList<IPAddress> SelectAddresses(IEnumerable<MdnsInterface> interfaces)
        {
            var addresses = interfaces.SelectMany(n => n.Addresses).Where(IsUsable).Distinct().ToList();
            return addresses.OrderBy(Rank).ToList();
        }

        /// <summary>The host name (spec 4.3.1: 12 or 16 upper-case hex digits of the MAC, else random), without the ".local" domain.</summary>
        public static string HostLabel(IEnumerable<MdnsInterface> interfaces)
        {
            var mac = interfaces.Select(n => n.PhysicalAddress)
                .FirstOrDefault(m => m != null && (m.Length == 6 || m.Length == 8) && m.Any(b => b != 0));
            return Convert.ToHexString(mac ?? Crypto.MatterCrypto.RandomBytes(8));
        }

        public static IReadOnlyList<MdnsInterface> Snapshot(IEnumerable<NetworkInterface> nics) =>
            nics.Select(n =>
            {
                IReadOnlyList<IPAddress> addresses;
                try
                {
                    addresses = n.GetIPProperties().UnicastAddresses.Select(u => u.Address).ToList();
                }
                catch (NetworkInformationException)
                {
                    addresses = Array.Empty<IPAddress>();
                }

                byte[] mac;
                try
                {
                    mac = n.GetPhysicalAddress()?.GetAddressBytes();
                }
                catch (NetworkInformationException)
                {
                    mac = null;
                }

                return new MdnsInterface(n.Name, n.NetworkInterfaceType, n.OperationalStatus, mac, addresses);
            }).ToList();

        public static IReadOnlyList<MdnsInterface> Current() => Snapshot(NetworkInterface.GetAllNetworkInterfaces());

        private static bool IsUsable(IPAddress a)
        {
            if (IPAddress.IsLoopback(a))
            {
                return false;
            }

            if (a.AddressFamily == AddressFamily.InterNetwork)
            {
                var b = a.GetAddressBytes();
                return !(b[0] == 169 && b[1] == 254) && !(b[0] == 0);
            }

            return a.AddressFamily == AddressFamily.InterNetworkV6 && !a.IsIPv6Multicast && !a.IsIPv4MappedToIPv6 && !a.IsIPv6Teredo;
        }

        private static int Rank(IPAddress a)
        {
            if (a.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return a.IsIPv6LinkLocal ? 1 : 0;
            }

            return 2;
        }
    }
}
