using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Driver.Discovery;
using Microsoft.Extensions.Logging;

namespace Automatica.Driver.ShellyFactory.Discovery
{
    public class ShellyDiscoveryService
    {
        private readonly IZeroconfDiscovery _discovery;
        private readonly ILogger _logger;

        public ShellyDiscoveryService(IZeroconfDiscovery discovery, ILogger logger)
        {
            _discovery = discovery;
            _logger = logger;
        }

        public async Task<List<ShellyDevice>> SearchShellys()
        {
            var httpLocal = await SearchShellys("_http._tcp.local.");
            var shellyTcp = await SearchShellys("_shelly._tcp.local.");

            var retList = new List<ShellyDevice>();
            retList.AddRange(httpLocal);
            retList.AddRange(shellyTcp);

            return retList.DistinctBy(a => a.Id).ToList();
        }

        /// <summary>
        /// Every Shelly announced over mDNS (Gen1 on _http._tcp with a shelly* host name, Gen2+ on _shelly._tcp and _http._tcp),
        /// whatever the model: the scan asks the device itself what it is.
        /// </summary>
        public async Task<List<ShellyDiscoveredHost>> SearchAllAsync()
        {
            var ret = new List<ShellyDiscoveredHost>();
            foreach (var service in new[] { "_shelly._tcp.local.", "_http._tcp.local." })
            {
                IEnumerable<ZeroconfHost> hosts;
                try
                {
                    hosts = await _discovery.DiscoverAsync(service, default);
                }
                catch (System.Exception e)
                {
                    _logger.LogWarning($"mDNS search for {service} failed: {e.Message}");
                    continue;
                }

                foreach (var host in hosts ?? Enumerable.Empty<ZeroconfHost>())
                {
                    var isShellyService = service.StartsWith("_shelly");
                    if (!isShellyService && (host.DisplayName == null || !host.DisplayName.StartsWith("shelly", System.StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(host.IpAddress) || ret.Any(a => a.IpAddress == host.IpAddress))
                    {
                        continue;
                    }

                    int? generation = null;
                    var properties = host.Services?.Values.SelectMany(a => a.Properties ?? new List<IReadOnlyDictionary<string, string>>()) ??
                                     Enumerable.Empty<IReadOnlyDictionary<string, string>>();
                    foreach (var p in properties)
                    {
                        if (p.TryGetValue("gen", out var gen) && int.TryParse(gen, out var g))
                        {
                            generation = g;
                        }
                    }

                    var id = host.DisplayName ?? host.Id ?? host.IpAddress;
                    ret.Add(new ShellyDiscoveredHost(id, host.IpAddress, generation ?? (isShellyService ? 2 : null)));
                }
            }

            return ret;
        }

        private async Task<List<ShellyDevice>> SearchShellys(string service)
        {
            var devices = await _discovery.DiscoverAsync(service, default);

            var shellyList = devices.Where(a => a.DisplayName.Contains("shelly") || a.Services.Any(a => a.Value.ServiceName.Contains("shelly")));
            var ret = new List<ShellyDevice>();

            foreach (var shelly in shellyList)
            {
                var id = shelly.DisplayName;

                _logger.LogDebug($"Found device with id {id} and ip {shelly.IpAddress}");

                var deviceAndId = id.Replace("shelly", "");
                var split = deviceAndId.Split("-");
                var deviceType = split[0];
                var name = split[1];

                ShellyDeviceType type;

                switch (deviceType.ToLowerInvariant())
                {
                    case "1":
                        type = ShellyDeviceType.Shelly1;
                        break;
                    case "1pm":
                        type = ShellyDeviceType.Shelly1Pm;
                        break;
                    case "switch25":
                        type = ShellyDeviceType.Shelly25;
                        break;
                    case "plus2pm":
                        type = ShellyDeviceType.ShellyPlus2Pm;
                        break;
                    case "plus1pm":
                        type = ShellyDeviceType.ShellyPlus1Pm;
                        break;
                    case "plusplugs":
                        type = ShellyDeviceType.ShellyPlugS;
                        break;
                    default:
                        continue;
                }

                ShellyGeneration generation;

                var httpService = shelly.Services.First(a => a.Key.EndsWith(service));

                if (httpService.Value.Properties.First().Any(a => a.Key == "gen"))
                {
                    var genType = httpService.Value.Properties.First().First(a => a.Key == "gen");
                    switch (genType.Value)
                    {
                        case "1":
                            generation = ShellyGeneration.Gen1;
                            break;
                        case "2":
                            generation = ShellyGeneration.Gen2;
                            break;
                        default:
                            generation = ShellyGeneration.Gen1;
                            break;
                    }
                }
                else
                {
                    generation = ShellyGeneration.Gen1;
                }
                
                ret.Add(new ShellyDevice(id, name, id.Split("-")[^1], type, shelly.IpAddress, generation));
            }

            return ret;
        }
    }
}
