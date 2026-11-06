using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Matter.Transport
{
    /// <summary>The Matter UDP transport (spec 4.3), dual-stack on IPv6 with an IPv4 fallback.</summary>
    public sealed class UdpTransport : IDisposable
    {
        public const int DefaultPort = 5540;

        private readonly ILogger _logger;
        private Socket _socket;
        private CancellationTokenSource _cts;
        private Task _receiveTask;

        public UdpTransport(ILogger logger)
        {
            _logger = logger;
        }

        public int Port { get; private set; }

        public Action<byte[], IPEndPoint> MessageReceived { get; set; }

        public void Start(int port)
        {
            try
            {
                _socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp) { DualMode = true };
                _socket.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
            }
            catch (SocketException e) when (e.SocketErrorCode is SocketError.AddressFamilyNotSupported or SocketError.ProtocolNotSupported)
            {
                _logger.LogWarning("IPv6 is not available, Matter falls back to IPv4 only");
                _socket?.Dispose();
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _socket.Bind(new IPEndPoint(IPAddress.Any, port));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // ignore ICMP port unreachable, otherwise the next receive throws WSAECONNRESET
                const int sioUdpConnReset = -1744830452;
                _socket.IOControl(sioUdpConnReset, new byte[] { 0 }, null);
            }

            Port = ((IPEndPoint)_socket.LocalEndPoint!).Port;
            _cts = new CancellationTokenSource();
            _receiveTask = Task.Run(() => ReceiveLoop(_cts.Token));
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            var buffer = new byte[2048];
            EndPoint any = _socket.AddressFamily == AddressFamily.InterNetworkV6
                ? new IPEndPoint(IPAddress.IPv6Any, 0)
                : new IPEndPoint(IPAddress.Any, 0);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await _socket.ReceiveFromAsync(buffer, SocketFlags.None, any, token);
                    var data = buffer.AsSpan(0, result.ReceivedBytes).ToArray();
                    var from = (IPEndPoint)result.RemoteEndPoint;
                    if (from.Address.IsIPv4MappedToIPv6)
                    {
                        from = new IPEndPoint(from.Address.MapToIPv4(), from.Port);
                    }

                    MessageReceived?.Invoke(data, from);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException e)
                {
                    _logger.LogDebug(e, "UDP receive failed");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error handling a Matter datagram");
                }
            }
        }

        public async Task SendAsync(byte[] data, IPEndPoint to)
        {
            var socket = _socket;
            if (socket == null)
            {
                return;
            }

            var target = to;
            if (socket.AddressFamily == AddressFamily.InterNetworkV6 && to.AddressFamily == AddressFamily.InterNetwork)
            {
                target = new IPEndPoint(to.Address.MapToIPv6(), to.Port);
            }
            else if (socket.AddressFamily == AddressFamily.InterNetwork && to.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (!to.Address.IsIPv4MappedToIPv6)
                {
                    return;
                }

                target = new IPEndPoint(to.Address.MapToIPv4(), to.Port);
            }

            try
            {
                await socket.SendToAsync(data, SocketFlags.None, target);
            }
            catch (Exception e) when (e is SocketException or ObjectDisposedException)
            {
                _logger.LogDebug(e, "UDP send to {Target} failed", target);
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _socket?.Dispose();
            _socket = null;
            try
            {
                _receiveTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch
            {
                // ignore
            }

            _cts?.Dispose();
        }
    }
}
