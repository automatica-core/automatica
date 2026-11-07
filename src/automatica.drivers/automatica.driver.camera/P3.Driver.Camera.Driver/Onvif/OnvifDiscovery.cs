using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace P3.Driver.Camera.Driver.Onvif
{
    internal sealed record DiscoveredCamera(string Host, int Port, string Name, string Hardware, string Address);

    internal interface IOnvifDiscovery
    {
        Task<IReadOnlyList<DiscoveredCamera>> ProbeAsync(TimeSpan duration, CancellationToken token = default);
    }

    /// <summary>
    /// WS-Discovery probe for ONVIF video transmitters (multicast 239.255.255.250:3702) on every IPv4 interface
    /// </summary>
    internal sealed class OnvifDiscovery : IOnvifDiscovery
    {
        private static readonly IPEndPoint Multicast = new(IPAddress.Parse("239.255.255.250"), 3702);

        public async Task<IReadOnlyList<DiscoveredCamera>> ProbeAsync(TimeSpan duration, CancellationToken token = default)
        {
            var probe = Encoding.UTF8.GetBytes(BuildProbe(Guid.NewGuid()));
            var found = new Dictionary<string, DiscoveredCamera>(StringComparer.OrdinalIgnoreCase);
            var sockets = new List<UdpClient>();

            try
            {
                foreach (var local in LocalAddresses())
                {
                    try
                    {
                        var client = new UdpClient(new IPEndPoint(local, 0));
                        client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, local.GetAddressBytes());
                        sockets.Add(client);
                        await client.SendAsync(probe, probe.Length, Multicast);
                    }
                    catch (SocketException)
                    {
                        // interface without multicast
                    }
                }

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(duration);
                await Task.WhenAll(sockets.Select(s => ReceiveAsync(s, found, cts.Token)));
            }
            finally
            {
                foreach (var socket in sockets)
                {
                    socket.Dispose();
                }
            }

            token.ThrowIfCancellationRequested();
            lock (found)
            {
                return found.Values.OrderBy(c => c.Host).ToList();
            }
        }

        private static async Task ReceiveAsync(UdpClient client, Dictionary<string, DiscoveredCamera> found, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                UdpReceiveResult result;
                try
                {
                    result = await client.ReceiveAsync(token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (SocketException)
                {
                    return;
                }

                foreach (var camera in ParseProbeMatches(Encoding.UTF8.GetString(result.Buffer)))
                {
                    lock (found)
                    {
                        found.TryAdd(camera.Host, camera);
                    }
                }
            }
        }

        private static IEnumerable<IPAddress> LocalAddresses()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback && n.SupportsMulticast)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Select(a => a.Address)
                .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                .Distinct();
        }

        internal static string BuildProbe(Guid messageId)
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                   "<e:Envelope xmlns:e=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:w=\"http://schemas.xmlsoap.org/ws/2004/08/addressing\" " +
                   "xmlns:d=\"http://schemas.xmlsoap.org/ws/2005/04/discovery\" xmlns:dn=\"http://www.onvif.org/ver10/network/wsdl\">" +
                   $"<e:Header><w:MessageID>uuid:{messageId}</w:MessageID>" +
                   "<w:To e:mustUnderstand=\"true\">urn:schemas-xmlsoap-org:ws:2005:04:discovery</w:To>" +
                   "<w:Action e:mustUnderstand=\"true\">http://schemas.xmlsoap.org/ws/2005/04/discovery/Probe</w:Action></e:Header>" +
                   "<e:Body><d:Probe><d:Types>dn:NetworkVideoTransmitter</d:Types></d:Probe></e:Body></e:Envelope>";
        }

        internal static IReadOnlyList<DiscoveredCamera> ParseProbeMatches(string xml)
        {
            var result = new List<DiscoveredCamera>();
            XDocument doc;
            try
            {
                doc = XDocument.Parse(xml);
            }
            catch (System.Xml.XmlException)
            {
                return result;
            }

            foreach (var match in doc.Descendants().Where(e => e.Name.LocalName == "ProbeMatch"))
            {
                var addresses = match.Elements().FirstOrDefault(e => e.Name.LocalName == "XAddrs")?.Value
                    .Split((char[])null, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                var scopes = match.Elements().FirstOrDefault(e => e.Name.LocalName == "Scopes")?.Value
                    .Split((char[])null, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

                // prefer an IPv4 address, cameras often list a link local IPv6 one too
                var address = addresses
                    .Select(a => Uri.TryCreate(a, UriKind.Absolute, out var u) ? u : null)
                    .Where(u => u != null)
                    .OrderBy(u => u.HostNameType == UriHostNameType.IPv4 ? 0 : 1)
                    .FirstOrDefault();
                if (address == null)
                {
                    continue;
                }

                result.Add(new DiscoveredCamera(address.Host, address.Port, Scope(scopes, "name"), Scope(scopes, "hardware"), address.ToString()));
            }
            return result;
        }

        private static string Scope(IEnumerable<string> scopes, string key)
        {
            var prefix = $"onvif://www.onvif.org/{key}/";
            var scope = scopes.FirstOrDefault(s => s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            return scope == null ? null : Uri.UnescapeDataString(scope[prefix.Length..]).Replace('_', ' ').Trim();
        }
    }
}
