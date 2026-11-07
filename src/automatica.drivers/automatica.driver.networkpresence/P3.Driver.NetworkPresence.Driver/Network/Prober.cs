using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.NetworkPresence.Driver.Network
{
    internal interface IProber
    {
        /// <summary>
        /// Makes the operating system resolve (ARP) the given addresses, so they show up in the neighbor table
        /// </summary>
        Task ProbeAsync(IReadOnlyCollection<IPAddress> addresses, CancellationToken token = default);
    }

    /// <summary>
    /// Sends one UDP datagram to the discard port (needs no privileges, the kernel has to ARP to send it) and one ICMP echo.
    /// Phones in standby often ignore ICMP but still answer ARP.
    /// </summary>
    internal sealed class ArpProber : IProber
    {
        private const int DiscardPort = 9;
        private const int MaxParallel = 64;
        private const int PingTimeoutMs = 400;
        private static readonly byte[] Payload = { 0 };

        public async Task ProbeAsync(IReadOnlyCollection<IPAddress> addresses, CancellationToken token = default)
        {
            using var gate = new SemaphoreSlim(MaxParallel);
            var tasks = new List<Task>(addresses.Count);
            foreach (var address in addresses)
            {
                await gate.WaitAsync(token);
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await ProbeOneAsync(address);
                    }
                    finally
                    {
                        gate.Release();
                    }
                }, token));
            }
            await Task.WhenAll(tasks);
        }

        private static async Task ProbeOneAsync(IPAddress address)
        {
            try
            {
                using var udp = new UdpClient(address.AddressFamily);
                await udp.SendAsync(Payload, Payload.Length, new IPEndPoint(address, DiscardPort));
            }
            catch (SocketException)
            {
                // host unreachable etc.: nothing to learn here
            }

            try
            {
                using var ping = new Ping();
                await ping.SendPingAsync(address, PingTimeoutMs);
            }
            catch (Exception ex) when (ex is PingException or InvalidOperationException or NotSupportedException)
            {
                // ICMP may be unavailable in a container, the UDP probe is enough for ARP
            }
        }
    }
}
