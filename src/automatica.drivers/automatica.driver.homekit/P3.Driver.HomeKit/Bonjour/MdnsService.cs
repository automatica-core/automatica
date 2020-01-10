using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver.Utility;
using Automatica.Core.Driver.Utility.Network;
using Microsoft.Extensions.Logging;
using P3.Driver.HomeKit.Hap;

namespace P3.Driver.HomeKit.Bonjour
{
    public class MdnsService
    {
        private readonly ILogger _logger;
        private readonly int _hapPort;
        private readonly string _name;
        private bool _isRunning = true;
        private Socket _socket;

        private Socket _socket6;

        private CancellationTokenSource _cancel;

        const int MulticastPort = 5353;

        static readonly IPAddress MulticastAddressIp4 = IPAddress.Parse("224.0.0.251");
        static readonly IPAddress MulticastAddressIp6 = IPAddress.Parse("FF02::FB");
        static readonly IPEndPoint MdnsEndpointIp6 = new IPEndPoint(MulticastAddressIp6, MulticastPort);
        static readonly IPEndPoint MdnsEndpointIp4 = new IPEndPoint(MulticastAddressIp4, MulticastPort);


        public MdnsService(ILogger logger, int hapPort, string name)
        {
            _logger = logger;
            _hapPort = hapPort;
            _name = name;
        }


        public void Stop()
        {
            _cancel.Cancel();
            _isRunning = false;
            _socket.Close();
            _socket6.Close();
        }

        public void Start()
        {
            try
            {
                _cancel = new CancellationTokenSource();
                var ipAddress = NetworkHelper.GetActiveIp();
              
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
                {
                    EnableBroadcast = true,
                    ExclusiveAddressUse = false,
                    MulticastLoopback = true
                };
                try
                {
                   
                    _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership,
                        new MulticastOption(MulticastAddressIp4, IPAddress.Parse(ipAddress)));
                    _socket.Bind(new IPEndPoint(IPAddress.Any, MulticastPort));
                    _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);

                    _logger.LogDebug($"Create IPv4 Socket for multicast on {MulticastAddressIp4}");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error binding multicast IPv4 socket...");
                    _socket = null;
                }
                _socket6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp)
                {
                    EnableBroadcast = true,
                    ExclusiveAddressUse = false
                };
                try
                {
                  
                    _socket6.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    _socket6.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership,
                        new IPv6MulticastOption(MulticastAddressIp6));
                    _socket6.Bind(new IPEndPoint(IPAddress.IPv6Any, MulticastPort));
                    //  _socket6.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastLoopback, true);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error binding multicast IPv6 socket...");
                    _socket6 = null;
                }

                _logger.LogDebug($"Create IPv4 Socket for multicast on {MulticastAddressIp6}");

                var currentNics = GetNetworkInterfaces().ToList();
                var allNics = NetworkInterface.GetAllNetworkInterfaces();

                foreach (var nic in allNics)
                {
                    _logger.LogDebug($"Interface {nic.Name} has addresses Type: {nic.NetworkInterfaceType} | Status: {nic.OperationalStatus}");

                    foreach (var ip in nic.GetIPProperties().UnicastAddresses)
                    {
                        _logger.LogDebug($"UnicastAddress: {ip.Address} {ip.Address.AddressFamily}");
                    }
                    foreach (var ip in nic.GetIPProperties().MulticastAddresses)
                    {
                        _logger.LogDebug($"MulticastAddress: {ip.Address} {ip.Address.AddressFamily}");
                    }
                }

                var addreses = currentNics
                    .SelectMany(GetNetworkInterfaceLocalAddresses)
                    .Where(a => (a.AddressFamily == AddressFamily.InterNetwork)
                                || (a.AddressFamily == AddressFamily.InterNetworkV6));
                foreach (var address in addreses)
                {
                    _logger.LogDebug($"IPAddress {address} on address familiy {address.AddressFamily}");

                    var localEndpoint = new IPEndPoint(address, MulticastPort);
                    var sender = new UdpClient(address.AddressFamily);
                    try
                    {
                        switch (address.AddressFamily)
                        {
                            case AddressFamily.InterNetwork:
                                _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership,
                                    new MulticastOption(MulticastAddressIp4, address));
                                sender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress,
                                    true);
                                sender.Client.Bind(localEndpoint);
                                sender.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership,
                                    new MulticastOption(MulticastAddressIp4));
                                sender.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback,
                                    true);

                                _logger.LogDebug($"Listen to multicast on {address}");
                                break;
                            case AddressFamily.InterNetworkV6:
                                _socket6.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership,
                                    new IPv6MulticastOption(MulticastAddressIp6, address.ScopeId));
                                sender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress,
                                    true);
                                sender.Client.Bind(localEndpoint);
                                sender.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership,
                                    new IPv6MulticastOption(MulticastAddressIp6));
                                sender.Client.SetSocketOption(SocketOptionLevel.IPv6,
                                    SocketOptionName.MulticastLoopback, true);


                                _logger.LogDebug($"Listen to multicast on ipv6 {address}");
                                break;
                            default:
                                throw new NotSupportedException($"Address family {address.AddressFamily}.");
                        }
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressNotAvailable)
                    {
                        // VPN NetworkInterfaces
                        sender.Dispose();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error binding multicast");
                        sender.Dispose();
                    }
                }

                if (_socket != null)
                {
                    Task.Run(() => Listen(_socket), _cancel.Token);
                }

                if (_socket6 != null)
                {
                    Task.Run(() => Listen(_socket6), _cancel.Token);
                }

            }
            catch (Exception exp)
            {
                _logger.LogError(exp, $"Error binding socket {exp.StackTrace}");
            }
        }

        public static IEnumerable<NetworkInterface> GetNetworkInterfaces()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up || nic.OperationalStatus == OperationalStatus.Unknown)
                .Where(nic => nic.NetworkInterfaceType != NetworkInterfaceType.Loopback);
        }

        /// <summary>
        ///   Get the IP addresses of the local machine.
        /// </summary>
        /// <returns>
        ///   A sequence of IP addresses of the local machine.
        /// </returns>
        public static IEnumerable<IPAddress> GetIPAddresses()
        {
            return GetNetworkInterfaces()
                .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
                .Select(u => u.Address);
        }
        public static IEnumerable<UnicastIPAddressInformation> GetUnicastAddresses()
        {
            return GetNetworkInterfaces()
                .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
                .Select(u => u);
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

        /// <summary>
        ///   Get the link local IP addresses of the local machine.
        /// </summary>
        /// <returns>
        ///   A sequence of IP addresses.
        /// </returns>
        /// <remarks>
        ///   All IPv4 addresses are considered link local.
        /// </remarks>
        /// <seealso href="https://en.wikipedia.org/wiki/Link-local_address"/>
        public static IEnumerable<IPAddress> GetLinkLocalAddresses()
        {
            return GetIPAddresses()
                .Where(a => a.AddressFamily == AddressFamily.InterNetwork ||
                            (a.AddressFamily == AddressFamily.InterNetworkV6 && a.IsIPv6LinkLocal));
        }


        private void Listen(Socket socket)
        {
            _logger.LogDebug($"Listen on socket {socket.LocalEndPoint} {socket.AddressFamily}");

      
            while (_isRunning)
            {
                try
                {
                    EndPoint senderRemote;
                    if (socket.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        senderRemote = new IPEndPoint(IPAddress.IPv6Any, 0);
                    }
                    else
                    {
                        senderRemote = new IPEndPoint(IPAddress.Any, 0);
                    }

                    var buffer = new byte[2048];
                    int numberOfbytesReceived = socket.ReceiveFrom(buffer, ref senderRemote);

                    if (senderRemote is IPEndPoint senderRemoteIp)
                    {
                       // _logger.LogDebug($"Received request from {senderRemoteIp.Address}");
                        var content = new byte[numberOfbytesReceived];
                        Array.Copy(buffer, 0, content, 0, numberOfbytesReceived);

                        ByteArrayToStringDump(content);

                        if (content[2] != 0x00)
                        {
                            //Console.WriteLine("Not a query. Ignoring.");
                            continue;
                        }

                        // Build the header that indicates this is a response.
                        //

                        var outputBuffer = GenerateDnsRecord(socket.AddressFamily, senderRemoteIp);
                        var bytesSent = socket.SendTo(outputBuffer, 0, outputBuffer.Length, SocketFlags.None,
                            senderRemote);
                    }
                }
                catch (TaskCanceledException)
                {
                    return;
                }
                catch (Exception exp)
                {
                    _logger.LogError(exp, "Error processing data");
                }
            }
        }

        private byte[] GenerateDnsRecord(AddressFamily addressFamily, IPEndPoint senderRemote)
        {
            var outputBuffer = new byte[0];

            var flags = new byte[2];

            var bitArray = new BitArray(flags);

            // We're using 15 and 10 since the endianness of this bytes is reversed :)
            //
            bitArray.Set(15, true); // QR
            bitArray.Set(10, true); // AA

            bitArray.CopyTo(flags, 0);

            var questionCount = BitConverter.GetBytes((short)1).Reverse().ToArray();
            var answerCount = BitConverter.GetBytes((short)4).Reverse().ToArray();
            var additionalCounts = BitConverter.GetBytes((short)1).Reverse().ToArray();
            var otherCounts = BitConverter.GetBytes((short)0).Reverse().ToArray();

            // Add the header to the output buffer.
            //
            outputBuffer = outputBuffer.Concat(otherCounts).Concat(flags.Reverse()).Concat(questionCount)
                .Concat(answerCount).Concat(otherCounts).Concat(additionalCounts).ToArray();

            int endOfHeaderBufferLength = outputBuffer.Length;

            var nodeName = GetName("_services._dns-sd._udp.local");

            outputBuffer = outputBuffer.Concat(nodeName).ToArray();

            var typeBytes = BitConverter.GetBytes((short)12).Reverse().ToArray(); // PTR

            outputBuffer = outputBuffer.Concat(typeBytes).ToArray();

            var @class = BitConverter.GetBytes((short)1).Reverse().ToArray(); // Internet

            outputBuffer = outputBuffer.Concat(@class).ToArray();

            Dictionary<string, string> values = new Dictionary<string, string>();
            values.Add("sf", "1");
            values.Add("ff", "0x00");
            values.Add("ci", "2");
            values.Add("id", HapControllerServer.HapControllerId);
            values.Add("md", _name);
            values.Add("s#", "1");
            values.Add("c#", $"{HapControllerServer.ConfigVersion}");

            outputBuffer = AddTxt(outputBuffer, $"{_name}._hap._tcp.local", values);
            outputBuffer = AddPtr(outputBuffer, "_services._dns-sd._udp.local", "_hap._tcp.local");
            outputBuffer = AddPtr(outputBuffer, "_hap._tcp.local", $"{_name}._hap._tcp.local");
            outputBuffer = AddSrv(outputBuffer, $"{_name}._hap._tcp.local", 0, 0, _hapPort,
                $"{_name}.local");


            var ipAddresses = GetUnicastAddresses();

            if (addressFamily == AddressFamily.InterNetworkV6)
            {
                ipAddresses = ipAddresses.Where(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6);
            }
            else
            {
                ipAddresses = ipAddresses.Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
            }

            foreach (var address in ipAddresses)
            {
                if (address.Address.IsInSameSubnet(senderRemote.Address, address.IPv4Mask))
                {
                    var ip = address.Address.ToString();
                  //  _logger.LogDebug($"Set A record to value: {ip}");
                    outputBuffer = AddARecord(outputBuffer, $"{_name}.local", ip, addressFamily == AddressFamily.InterNetworkV6);
                }
            }

            ByteArrayToStringDump(outputBuffer);

            Thread.Sleep(50);
            return outputBuffer;
        }

        private byte[] AddARecord(byte[] outputBuffer, string hostName, string ipAddress, bool isIPv6)
        {
            var nodeName = GetName(hostName);

            outputBuffer = outputBuffer.Concat(nodeName).ToArray();

            var dnsType = 1;

            if (isIPv6)
            {
                dnsType = 28;
            }

            var typeBytes = BitConverter.GetBytes((short)dnsType).Reverse().ToArray(); // A or AAAA

            outputBuffer = outputBuffer.Concat(typeBytes).ToArray();

            var @class = BitConverter.GetBytes((short)1).Reverse().ToArray(); // Internet
            @class[0] = @class[0].SetBit(7); // set flush to true

            outputBuffer = outputBuffer.Concat(@class).ToArray();

            var ttl = BitConverter.GetBytes(120).Reverse().ToArray();

            outputBuffer = outputBuffer.Concat(ttl).ToArray();

            var address = ConvertIpAddress(ipAddress);

            // For IP4, this will be an int32
            var dataLength = BitConverter.GetBytes((short)address.Length).Reverse().ToArray();

            outputBuffer = outputBuffer.Concat(dataLength).ToArray();

            outputBuffer = outputBuffer.Concat(address).ToArray();

            //outputBuffer = outputBuffer.Concat(new byte[2] { 0xC0, 0x0C }).ToArray();

            return outputBuffer;
        }

        private byte[] ConvertIpAddress(string ipAddress)
        {
            if (ipAddress.Contains(":"))
            {
                var ip = IPAddress.Parse(ipAddress);

                return ip.GetAddressBytes();
            }
            else
            {
                var parts = ipAddress.Split('.');

                byte[] result = new byte[4];

                int index = 0;

                foreach (var part in parts)
                {
                    result[index++] = byte.Parse(part);
                }

                return result;
            }
        }

        private byte[] AddSrv(byte[] outputBuffer, string host, short priority, short weight, int port, string v4)
        {
            var nodeName = GetName(host);

            outputBuffer = outputBuffer.Concat(nodeName).ToArray();

            var type = BitConverter.GetBytes((short)33).Reverse().ToArray(); // SRV

            outputBuffer = outputBuffer.Concat(type).ToArray();

            var @class = BitConverter.GetBytes((short)1).Reverse().ToArray(); // Internet
            @class[0] = @class[0].SetBit(7); // set flush to true

            outputBuffer = outputBuffer.Concat(@class).ToArray();

            var ttl = BitConverter.GetBytes(120).Reverse().ToArray();

            outputBuffer = outputBuffer.Concat(ttl).ToArray();

            var svrName = GetName(v4);

            int totalLength = svrName.Length + 2 + 2 + 2; // name + priority + weight + port

            var dataLength = BitConverter.GetBytes((short)totalLength).Reverse().ToArray();

            outputBuffer = outputBuffer.Concat(dataLength).ToArray();

            var priorityBytes = BitConverter.GetBytes(priority).Reverse().ToArray();

            outputBuffer = outputBuffer.Concat(priorityBytes).ToArray();

            var weightBytes = BitConverter.GetBytes(weight).Reverse().ToArray();

            outputBuffer = outputBuffer.Concat(weightBytes).ToArray();

            var portBytes = BitConverter.GetBytes((short)port).Reverse().ToArray();

            outputBuffer = outputBuffer.Concat(portBytes).ToArray();

            outputBuffer = outputBuffer.Concat(svrName).ToArray();

            return outputBuffer;
        }

        private byte[] AddTxt(byte[] outputBuffer, string v, Dictionary<string, string> values)
        {
            var nodeName = GetName(v);

            outputBuffer = outputBuffer.Concat(nodeName).ToArray();

            var type = BitConverter.GetBytes((short)16).Reverse().ToArray(); // TXT

            outputBuffer = outputBuffer.Concat(type).ToArray();

            var @class = BitConverter.GetBytes((short)1).Reverse().ToArray(); // Internet
            @class[0] = @class[0].SetBit(7); // set flush to true

            outputBuffer = outputBuffer.Concat(@class).ToArray();

            var ttl = BitConverter.GetBytes(120).Reverse().ToArray();

            outputBuffer = outputBuffer.Concat(ttl).ToArray();

            var txtRecord = GetTxtRecord(values);

            var recordLength = BitConverter.GetBytes((short)txtRecord.Length).Reverse().ToArray();

            outputBuffer = outputBuffer.Concat(recordLength).ToArray();

            outputBuffer = outputBuffer.Concat(txtRecord).ToArray();

            return outputBuffer;
        }

        private byte[] AddPtr(byte[] outputBuffer, string v1, string v2)
        {
            var ptrNodeName = GetName(v1);

            outputBuffer = outputBuffer.Concat(ptrNodeName).ToArray();

            var type = BitConverter.GetBytes((short)12).Reverse().ToArray(); // PTR

            outputBuffer = outputBuffer.Concat(type).ToArray();

            var @class = BitConverter.GetBytes((short)1).Reverse().ToArray(); // Internet
            //@class[0] = @class[0].SetBit(7); // set flush to true

            outputBuffer = outputBuffer.Concat(@class).ToArray();

            var ttl = BitConverter.GetBytes(120).Reverse().ToArray();

            outputBuffer = outputBuffer.Concat(ttl).ToArray();

            var ptrServiceName = GetName(v2);

            var recordLength = BitConverter.GetBytes((short)ptrServiceName.Length).Reverse().ToArray();

            outputBuffer = outputBuffer.Concat(recordLength).ToArray();

            outputBuffer = outputBuffer.Concat(ptrServiceName).ToArray();

            return outputBuffer;
        }

        private byte[] GetTxtRecord(Dictionary<string, string> values)
        {
            var result = new byte[0];

            foreach (var keypair in values)
            {
                string fullKeyPair = $"{keypair.Key}={keypair.Value}";
                result = result.Concat(new byte[1] { (byte)fullKeyPair.Length }).Concat(Encoding.UTF8.GetBytes(fullKeyPair)).ToArray();
            }

            return result;
        }

        private void ByteArrayToStringDump(byte[] ba)
        {
            //_logger.LogDebug($"Send mDNS Response");
            //_logger.LogHexOut(ba);
        }

        private byte[] GetName(string v)
        {
            var parts = v.Split('.');

            var result = new byte[0];

            foreach (var part in parts)
            {
                int length = part.Length;
                byte lengthByte = Convert.ToByte(length);
                result = result.Concat(new byte[1] { lengthByte }).Concat(Encoding.UTF8.GetBytes(part)).ToArray();
            }

            // Null terminator.
            //
            return result.Concat(new byte[1] { 0x00 }).ToArray();
        }

    }

    internal static class ByteExtensions
    {
        public static byte SetBit(this byte b, int pos)
        {
            if (pos < 0 || pos > 7)
                throw new ArgumentOutOfRangeException("pos", "Index must be in the range of 0-7.");

            return (byte)(b | (1 << pos));
        }
    }
}
