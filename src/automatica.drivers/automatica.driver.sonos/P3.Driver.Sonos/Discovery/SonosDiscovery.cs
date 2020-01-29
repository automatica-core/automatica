using System.Collections.Generic;
using Rssdp;
using System.Threading.Tasks;

namespace P3.Driver.Sonos.Discovery
{
    public static class SonosDiscovery
    {

        public static async Task<List<SsdpRootDevice>> DiscoverSonos()
        {
            var ret = new List<SsdpRootDevice>();
            using (var deviceLocator = new SsdpDeviceLocator())
            {
                var foundDevices = await deviceLocator.SearchAsync("urn:schemas-upnp-org:device:ZonePlayer:1");

                foreach (var foundDevice in foundDevices)
                {
                    var fullDevice = await foundDevice.GetDeviceInfo();

                    if (fullDevice is SsdpRootDevice rootDevice && fullDevice.Manufacturer.ToLowerInvariant().Contains("sonos"))
                    {
                        ret.Add(rootDevice);
                    }
                }
            }

            return ret;
        }
    }
}
