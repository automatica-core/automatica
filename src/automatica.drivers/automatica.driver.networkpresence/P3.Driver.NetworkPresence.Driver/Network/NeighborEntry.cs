using System;
using System.Globalization;
using System.Linq;
using System.Net;

namespace P3.Driver.NetworkPresence.Driver.Network
{
    /// <summary>
    /// One entry of the operating system's neighbor (ARP) table
    /// </summary>
    internal sealed record NeighborEntry(IPAddress Address, string Mac, bool Complete)
    {
        /// <summary>
        /// Normalizes a MAC address to upper case, colon separated ("AA:BB:CC:DD:EE:FF"). Returns null for anything that is not a MAC.
        /// </summary>
        public static string NormalizeMac(string mac)
        {
            if (string.IsNullOrWhiteSpace(mac))
            {
                return null;
            }

            var hex = new string(mac.Where(Uri.IsHexDigit).ToArray());
            if (hex.Length != 12)
            {
                return null;
            }

            var bytes = Enumerable.Range(0, 6).Select(i => hex.Substring(i * 2, 2).ToUpperInvariant());
            var normalized = string.Join(":", bytes);
            return normalized == "00:00:00:00:00:00" || normalized == "FF:FF:FF:FF:FF:FF" ? null : normalized;
        }

        /// <summary>
        /// A locally administered MAC (second hex digit 2, 6, A or E) is usually a randomized "private Wi-Fi address"
        /// </summary>
        public static bool IsRandomized(string mac)
        {
            var normalized = NormalizeMac(mac);
            if (normalized == null)
            {
                return false;
            }
            var firstOctet = byte.Parse(normalized.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return (firstOctet & 0x02) != 0;
        }
    }
}
