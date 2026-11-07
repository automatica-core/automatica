using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace P3.Driver.NetworkPresence.Driver.Network
{
    /// <summary>
    /// An IPv4 network in CIDR notation
    /// </summary>
    internal sealed record Subnet(IPAddress Network, int PrefixLength)
    {
        /// <summary>
        /// The largest network that is swept host by host (1022 hosts). Larger networks are cut to the /24 around the local address.
        /// </summary>
        public const int MinSweepPrefix = 22;

        private static readonly string[] IgnoredInterfacePrefixes = { "docker", "br-", "veth", "virbr", "vEthernet", "cni", "flannel", "tun", "tap", "zt", "tailscale", "wg" };

        public static bool TryParse(string text, out Subnet subnet)
        {
            subnet = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var parts = text.Trim().Split('/');
            if (!IPAddress.TryParse(parts[0], out var ip) || ip.AddressFamily != AddressFamily.InterNetwork)
            {
                return false;
            }

            var prefix = 24;
            if (parts.Length > 1 && (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out prefix) || prefix < 0 || prefix > 32))
            {
                return false;
            }

            subnet = Create(ip, prefix);
            return true;
        }

        /// <summary>
        /// Parses a comma or semicolon separated list, ignores invalid entries
        /// </summary>
        public static IReadOnlyList<Subnet> ParseList(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<Subnet>();
            }
            return text.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => TryParse(t, out var s) ? s : null)
                .Where(s => s != null)
                .Distinct()
                .ToList();
        }

        public static Subnet Create(IPAddress address, int prefixLength)
        {
            var mask = Mask(prefixLength);
            var network = ToUInt(address) & mask;
            return new Subnet(FromUInt(network), prefixLength);
        }

        public bool Contains(IPAddress address)
        {
            if (address.AddressFamily != AddressFamily.InterNetwork)
            {
                return false;
            }
            var mask = Mask(PrefixLength);
            return (ToUInt(address) & mask) == ToUInt(Network);
        }

        /// <summary>
        /// All host addresses (without network and broadcast address)
        /// </summary>
        public IEnumerable<IPAddress> Hosts()
        {
            var effective = PrefixLength < MinSweepPrefix ? Create(Network, 24) : this;
            var start = ToUInt(effective.Network);
            var count = 1u << (32 - effective.PrefixLength);
            if (count <= 2)
            {
                for (uint i = 0; i < count; i++)
                {
                    yield return FromUInt(start + i);
                }
                yield break;
            }
            for (uint i = 1; i < count - 1; i++)
            {
                yield return FromUInt(start + i);
            }
        }

        public override string ToString() => $"{Network}/{PrefixLength}";

        /// <summary>
        /// The IPv4 networks of the local interfaces that are up (loopback, container and VPN interfaces are skipped)
        /// </summary>
        public static IReadOnlyList<Subnet> FromLocalInterfaces()
        {
            var result = new List<Subnet>();
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                    IgnoredInterfacePrefixes.Any(p => nic.Name.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                foreach (var address in nic.GetIPProperties().UnicastAddresses)
                {
                    if (address.Address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(address.Address))
                    {
                        continue;
                    }
                    var bytes = address.Address.GetAddressBytes();
                    if (bytes[0] == 169 && bytes[1] == 254)
                    {
                        continue; // link local
                    }

                    var prefix = address.PrefixLength;
                    // around the own address when the network is too big to sweep
                    result.Add(prefix < MinSweepPrefix ? Create(address.Address, 24) : Create(address.Address, prefix));
                }
            }
            return result.Distinct().ToList();
        }

        private static uint Mask(int prefixLength) => prefixLength == 0 ? 0 : uint.MaxValue << (32 - prefixLength);

        private static uint ToUInt(IPAddress address)
        {
            var b = address.GetAddressBytes();
            return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
        }

        private static IPAddress FromUInt(uint value)
        {
            return new IPAddress(new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value });
        }
    }
}
