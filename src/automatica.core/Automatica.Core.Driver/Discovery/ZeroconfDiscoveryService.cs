using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zeroconf;

namespace Automatica.Core.Driver.Discovery
{
    internal class ZeroconfDiscoveryService : IZeroconfDiscovery
    {
        public async Task<IEnumerable<ZeroconfHost>> DiscoverAsync(string serviceType, TimeSpan timeout)
        {
            var ret = new List<ZeroconfHost>();
            var responses = await ZeroconfResolver.ResolveAsync("_coap._udp.local.");
            foreach (var resp in responses)
            {
               var services = new Dictionary<string, ZeroconfService>();
                foreach (var service in resp.Services)
                {
                    var autoService = new ZeroconfService(service.Value.Name, service.Value.ServiceName,
                        service.Value.Port, service.Value.Ttl, service.Value.Properties);
                    services.Add(service.Key, autoService);
                }

                ret.Add(new ZeroconfHost(resp.Id, resp.DisplayName, resp.IPAddress, resp.IPAddresses, services));
            }

            return ret;
        }
    }
}
