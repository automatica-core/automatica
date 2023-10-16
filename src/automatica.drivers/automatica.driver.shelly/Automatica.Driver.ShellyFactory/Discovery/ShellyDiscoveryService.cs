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
            var devices = await _discovery.DiscoverAsync("_http._tcp.local.", default);

            var shellyList= devices.Where(a => a.DisplayName.Contains("shelly"));
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
                    default:
                        continue;
                }

                ShellyGeneration generation;

                var httpService = shelly.Services.First(a => a.Key.EndsWith("_http._tcp.local."));

                if (httpService.Value.Properties.First().Any(a => a.Value == "gen"))
                {
                    var genType = httpService.Value.Properties.First().First(a => a.Key == "gen");
                    switch (genType.Value)
                    {
                        case "gen1":
                            generation = ShellyGeneration.Gen1;
                            break;
                        case "gen2":
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

                ret.Add(new ShellyDevice(id, name,type, shelly.IpAddress, generation));
            }

            return ret;
        }
    }
}
