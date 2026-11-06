using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.Internals.Cache.Common;
using Makaretu.Dns;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Automatica.Discovery
{
    /// <summary>
    /// Announces the server as an mDNS/Bonjour service, so the mobile app can find it without
    /// typing an address. SSDP (<see cref="DiscoveryService"/>) is not usable from the app: sending
    /// multicast on iOS needs an entitlement Apple hands out on request, browsing Bonjour does not.
    /// </summary>
    public sealed class MdnsAdvertiser : IHostedService, IDisposable
    {
        /// <summary>Service type the app browses for (with ".local." appended).</summary>
        public const string ServiceType = "_automatica._tcp";

        private readonly ILogger<MdnsAdvertiser> _logger;
        private readonly ISettingsCache _settingsCache;
        private ServiceDiscovery _serviceDiscovery;
        private ServiceProfile _profile;

        public MdnsAdvertiser(ILogger<MdnsAdvertiser> logger, ISettingsCache settingsCache)
        {
            _logger = logger;
            _settingsCache = settingsCache;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!ushort.TryParse(ServerInfo.WebPort, out var port))
                {
                    _logger.LogWarning("No http port configured, the server is not announced via mDNS");
                    return Task.CompletedTask;
                }

                var addresses = GetLanAddresses();
                if (addresses.Length == 0)
                {
                    _logger.LogWarning("No LAN address found, the server is not announced via mDNS");
                    return Task.CompletedTask;
                }

                var instanceName = GetInstanceName();

                _profile = new ServiceProfile(instanceName, ServiceType, port, addresses);
                _profile.AddProperty("id", ServerInfo.ServerUid.ToString());
                _profile.AddProperty("name", instanceName);
                _profile.AddProperty("version", ServerInfo.GetServerVersion());
                _profile.AddProperty("scheme", "http");
                if (ushort.TryParse(ServerInfo.SslWebPort, out var sslPort))
                {
                    // the Android app is served from https://localhost and cannot call plain http (mixed content)
                    _profile.AddProperty("https_port", sslPort.ToString());
                }

                _serviceDiscovery = new ServiceDiscovery();
                _serviceDiscovery.Advertise(_profile);
                _serviceDiscovery.Announce(_profile);

                _logger.LogInformation($"Announced \"{instanceName}\" as {ServiceType} on port {port} ({string.Join(", ", addresses.Select(a => a.ToString()))})");
            }
            catch (Exception e)
            {
                // discovery is a convenience, the server has to start without it (e.g. port 5353 not available)
                _logger.LogWarning(e, "Could not announce the server via mDNS");
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_profile != null)
                {
                    _serviceDiscovery?.Unadvertise(_profile);
                }
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, "Could not withdraw the mDNS announcement");
            }

            return Task.CompletedTask;
        }

        private string GetInstanceName()
        {
            var name = _settingsCache.GetByKey("projectName")?.ValueText;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = $"Automatica.Core {Environment.MachineName}";
            }

            // DNS labels are at most 63 bytes, dots would split the instance name into labels
            name = name.Replace('.', ' ').Trim();
            return name.Length > 63 ? name[..63] : name;
        }

        /// <summary>
        /// IPv4 addresses of the interfaces that are up, without loopback and the virtual adapters
        /// (Docker, WSL, Hyper-V, VPN) - announcing those sends the app to an address it cannot reach.
        /// </summary>
        private static IPAddress[] GetLanAddresses()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                            && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                            && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel
                            && !IsVirtual(n))
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Select(a => a.Address)
                .Where(a => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a)
                            && !a.ToString().StartsWith("169.254.")) // link-local, no DHCP
                .Distinct()
                .ToArray();
        }

        private static bool IsVirtual(NetworkInterface n)
        {
            var name = (n.Name + " " + n.Description).ToLowerInvariant();
            return name.Contains("docker") || name.Contains("veth") || name.Contains("br-")
                   || name.Contains("vethernet") || name.Contains("hyper-v") || name.Contains("wsl")
                   || name.Contains("virtualbox") || name.Contains("vmware") || name.Contains("tailscale")
                   || name.Contains("zerotier") || name.Contains("wireguard");
        }

        public void Dispose()
        {
            _serviceDiscovery?.Dispose();
        }
    }
}
