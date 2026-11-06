using System;
using System.Collections.Generic;
using System.Net;

namespace P3.Driver.ModBusDriver.Slave.Tcp
{
    public class ModBusSlaveTcpConfig : ModBusSlaveConfig
    {
        public ushort Port { get; set; }

        public bool IgnoreDeviceId { get; set; } = true;

        public List<int> DeviceIds { get; set; }

        /// <summary>
        /// Local address the slave listens on, default all interfaces
        /// </summary>
        public IPAddress BindAddress { get; set; } = IPAddress.Any;

        /// <summary>
        /// Masters that may connect (single addresses or CIDR networks). Empty = everybody.
        /// </summary>
        public List<IPNetwork> AllowedClients { get; set; } = new List<IPNetwork>();

        public int MaxConnections { get; set; } = 16;

        /// <summary>
        /// A connection without a request for this long is closed
        /// </summary>
        public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Time to receive the rest of a started frame
        /// </summary>
        public TimeSpan FrameTimeout { get; set; } = TimeSpan.FromSeconds(10);

        public bool ReadOnly { get; set; }

        /// <summary>
        /// Parses a comma or semicolon separated list of IP addresses and CIDR networks
        /// </summary>
        public static List<IPNetwork> ParseAllowedClients(string value, out List<string> invalid)
        {
            var result = new List<IPNetwork>();
            invalid = new List<string>();

            if (string.IsNullOrWhiteSpace(value))
            {
                return result;
            }

            foreach (var entry in value.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (entry.Contains("/") && IPNetwork.TryParse(entry, out var network))
                {
                    result.Add(network);
                }
                else if (IPAddress.TryParse(entry, out var address))
                {
                    result.Add(new IPNetwork(address, address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? 128 : 32));
                }
                else
                {
                    invalid.Add(entry);
                }
            }

            return result;
        }

        public bool IsClientAllowed(IPAddress address)
        {
            if (AllowedClients == null || AllowedClients.Count == 0)
            {
                return true;
            }

            if (address == null)
            {
                return false;
            }

            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            foreach (var network in AllowedClients)
            {
                if (network.Contains(address))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
