using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace P3.Driver.HomeKit.Bonjour
{
    /// <summary>
    ///   Performs the magic to send and receive datagrams over multicast
    ///   sockets.
    /// </summary>
    class MulticastClient : IDisposable
    {
        private static readonly ILogger Log = NullLogger.Instance;

        /// <summary>
        ///   The port number assigned to Multicast DNS.
        /// </summary>
        /// <value>
        ///   Port number 5353.
        /// </value>
        public static readonly int MulticastPort = 5353;

        static readonly IPAddress MulticastAddressIp4 = IPAddress.Parse("224.0.0.251");
        static readonly IPAddress MulticastAddressIp6 = IPAddress.Parse("FF02::FB");
        static readonly IPEndPoint MdnsEndpointIp6 = new IPEndPoint(MulticastAddressIp6, MulticastPort);
        static readonly IPEndPoint MdnsEndpointIp4 = new IPEndPoint(MulticastAddressIp4, MulticastPort);

        readonly List<UdpClient> _receivers;
        readonly ConcurrentDictionary<IPAddress, UdpClient> _senders = new ConcurrentDictionary<IPAddress, UdpClient>();

        public event EventHandler<UdpReceiveResult> MessageReceived;

        public MulticastClient(bool useIPv4, bool useIpv6, IEnumerable<NetworkInterface> nics)
        {
            // Setup the receivers.
            _receivers = new List<UdpClient>();

            UdpClient receiver4 = null;
            if (useIPv4)
            {
                receiver4 = new UdpClient(AddressFamily.InterNetwork);
                receiver4.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                receiver4.Client.Bind(new IPEndPoint(IPAddress.Any, MulticastPort));
                _receivers.Add(receiver4);
            }

            UdpClient receiver6 = null;
            if (useIpv6)
            {
                receiver6 = new UdpClient(AddressFamily.InterNetworkV6);
                receiver6.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                receiver6.Client.Bind(new IPEndPoint(IPAddress.IPv6Any, MulticastPort));
                _receivers.Add(receiver6);
            }

            // Get the IP addresses that we should send to.
            var addreses = nics
                .SelectMany(GetNetworkInterfaceLocalAddresses)
                .Where(a => (useIPv4 && a.AddressFamily == AddressFamily.InterNetwork)
                    || (useIpv6 && a.AddressFamily == AddressFamily.InterNetworkV6));
            foreach (var address in addreses)
            {
                if (_senders.Keys.Contains(address))
                {
                    continue;
                }

                var localEndpoint = new IPEndPoint(address, MulticastPort);
                var sender = new UdpClient(address.AddressFamily);
                try
                {
                    switch (address.AddressFamily)
                    {
                        case AddressFamily.InterNetwork:
                            receiver4.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(MulticastAddressIp4, address));
                            sender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                            sender.Client.Bind(localEndpoint);
                            sender.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(MulticastAddressIp4));
                            sender.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);
                            break;
                        case AddressFamily.InterNetworkV6:
                            receiver6.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new IPv6MulticastOption(MulticastAddressIp6, address.ScopeId));
                            sender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                            sender.Client.Bind(localEndpoint);
                            sender.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new IPv6MulticastOption(MulticastAddressIp6));
                            sender.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastLoopback, true);
                            break;
                        default:
                            throw new NotSupportedException($"Address family {address.AddressFamily}.");
                    }

                    Log.LogDebug($"Will send via {localEndpoint}");
                    if (!_senders.TryAdd(address, sender)) // Should not fail
                    {
                        sender.Dispose();
                    }
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressNotAvailable)
                {
                    // VPN NetworkInterfaces
                    sender.Dispose();
                }
                catch (Exception e)
                {
                    Log.LogError(e, $"Cannot setup send socket for {address}: {e.Message}");
                    sender.Dispose();
                }
            }

            // Start listening for messages.
            foreach (var r in _receivers)
            {
                Listen(r);
            }
        }

        public async Task SendAsync(byte[] message)
        {
            foreach (var sender in _senders)
            {
                try
                {
                    var endpoint = sender.Key.AddressFamily == AddressFamily.InterNetwork ? MdnsEndpointIp4 : MdnsEndpointIp6;
                    await sender.Value.SendAsync(
                        message, message.Length, 
                        endpoint)
                    .ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Log.LogError(e, $"Sender {sender.Key} failure: {e.Message}");
                    // eat it.
                }
            }
        }

        void Listen(UdpClient receiver)
        {
            // ReceiveAsync does not support cancellation.  So the receiver is disposed
            // to stop it. See https://github.com/dotnet/corefx/issues/9848
            Task.Run(async () =>
            {
                try
                {
                    var task = receiver.ReceiveAsync();
                    _ = task.ContinueWith(x => Listen(receiver), TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.RunContinuationsAsynchronously);
                    _ = task.ContinueWith(x => MessageReceived?.Invoke(this, x.Result), TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.RunContinuationsAsynchronously);
                    await task.ConfigureAwait(false);
                }
                catch
                {
                    return;
                }
            });
        }

        IEnumerable<IPAddress> GetNetworkInterfaceLocalAddresses(NetworkInterface nic)
        {
            return nic
                .GetIPProperties()
                .UnicastAddresses
                .Select(x => x.Address)
                .Where(x => x.AddressFamily != AddressFamily.InterNetworkV6 || x.IsIPv6LinkLocal)
                ;
        }

        #region IDisposable Support

        private bool _disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    MessageReceived = null;

                    foreach (var receiver in _receivers)
                    {
                        try
                        {
                            receiver.Dispose();
                        }
                        catch
                        {
                            // eat it.
                        }
                    }
                    _receivers.Clear();

                    foreach (var address in _senders.Keys)
                    {
                        if (_senders.TryRemove(address, out var sender))
                        {
                            try
                            {
                                sender.Dispose();
                            }
                            catch
                            {
                                // eat it.
                            }
                        }
                    }
                    _senders.Clear();
                }

                _disposedValue = true;
            }
        }

        ~MulticastClient()
        {
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
