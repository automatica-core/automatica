﻿using Automatica.Core.Driver.Discovery;
using Zeroconf;

namespace Automatica.Driver.ShellyConsole
{
    internal class ZeroconfDiscovery : IZeroconfDiscovery
    {
        public async Task<IEnumerable<ZeroconfHost>> DiscoverAsync(string serviceType, TimeSpan timeout)
        {
            var ret = new List<ZeroconfHost>();
            var responses = await ZeroconfResolver.ResolveAsync(serviceType, timeout);
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
