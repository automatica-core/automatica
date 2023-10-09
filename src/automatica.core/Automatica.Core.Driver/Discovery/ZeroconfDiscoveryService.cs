using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Zeroconf;

namespace Automatica.Core.Driver.Discovery
{
    internal class ZeroconfDiscoveryService : IZeroconfDiscovery
    {
        private readonly ILogger<ZeroconfDiscoveryService> _logger;

        public ZeroconfDiscoveryService(ILogger<ZeroconfDiscoveryService> logger)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<ZeroconfHost>> DiscoverAsync(string serviceType, TimeSpan timeout=default)
        {
            var ret = new List<ZeroconfHost>();
            _logger.LogDebug($"Search for {serviceType}");
            var responses = await ZeroconfResolver.ResolveAsync(serviceType, timeout);
            foreach (var resp in responses)
            {
                var services = new Dictionary<string, ZeroconfService>();
                _logger.LogDebug($"Found {resp.Id} {resp.DisplayName} on {resp.IPAddress}");
                foreach (var service in resp.Services)
                {
                    _logger.LogDebug($"Found {resp.Id} provides service {service.Value.Name}");
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
