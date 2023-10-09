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
                var id = shelly.Services.First().Value.Properties.First()["id"];

                _logger.LogDebug($"Found device with id {id} and ip {shelly.IpAddress}");

                var deviceAndId = id.Replace("shelly", "");
                var split = deviceAndId.Split("-");
                var deviceType = split[0];
                var name = split[1];

                ShellyDeviceType type;

                switch (deviceType)
                {
                    case "1":
                        type = ShellyDeviceType.Shelly1Pm;
                        break;
                    default:
                        continue;
                }

                ret.Add(new ShellyDevice(id, name,type, shelly.IpAddress));
            }

            return ret;
        }
    }
}
