#nullable enable annotations
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Automatica.Driver.Shelly.CoIoT
{
    public delegate void CoIoTMessageHandler(CoIoTMessage message, IPAddress sender);

    /// <summary>
    /// Listens for Gen1 CoIoT packets (UDP 5683, multicast group 224.0.1.187). In docker the plugin needs host networking for multicast.
    /// </summary>
    public sealed class CoIoTListener : IDisposable
    {
        public const int DefaultPort = 5683;
        public static readonly IPAddress MulticastGroup = IPAddress.Parse("224.0.1.187");

        private readonly ILogger _logger;
        private readonly UdpClient _udp;
        private readonly CancellationTokenSource _cts = new();
        private Task? _loop;

        public event CoIoTMessageHandler? MessageReceived;

        public int Port { get; }

        /// <param name="port">5683, 0 for an ephemeral port (tests)</param>
        /// <param name="joinMulticast">join 224.0.1.187, off for the loopback tests</param>
        public CoIoTListener(ILogger logger, int port = DefaultPort, bool joinMulticast = true)
        {
            _logger = logger;
            _udp = new UdpClient(AddressFamily.InterNetwork);
            if (port != 0)
            {
                // 5683 is shared with other CoAP listeners on the host; an ephemeral port (tests) must stay exclusive,
                // Windows can hand out the same ephemeral port twice to reuse-address sockets
                _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            }
            _udp.Client.Bind(new IPEndPoint(IPAddress.Any, port));
            Port = ((IPEndPoint)_udp.Client.LocalEndPoint!).Port;

            if (joinMulticast)
            {
                try
                {
                    _udp.JoinMulticastGroup(MulticastGroup);
                }
                catch (SocketException e)
                {
                    _logger.LogWarning($"Could not join the CoIoT multicast group {MulticastGroup}: {e.Message}, only unicast CoIoT is received");
                }
            }
        }

        public void Start()
        {
            _loop = Task.Run(ReceiveLoop);
        }

        private async Task ReceiveLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                UdpReceiveResult result;
                try
                {
                    result = await _udp.ReceiveAsync(_cts.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException e)
                {
                    // e.g. ICMP port unreachable on Windows
                    _logger.LogDebug($"CoIoT receive: {e.Message}");
                    continue;
                }

                try
                {
                    var message = CoIoTMessage.Parse(result.Buffer);
                    if (message != null)
                    {
                        MessageReceived?.Invoke(message, result.RemoteEndPoint.Address);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error handling a CoIoT packet of {result.RemoteEndPoint}");
                }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _udp.Dispose();
            try
            {
                _loop?.Wait(TimeSpan.FromSeconds(2));
            }
            catch
            {
                // ignored on dispose
            }
            _cts.Dispose();
        }
    }
}
