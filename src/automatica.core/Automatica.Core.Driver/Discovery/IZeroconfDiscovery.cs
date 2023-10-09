using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zeroconf;

namespace Automatica.Core.Driver.Discovery
{
    public class ZeroconfService
    {
        public ZeroconfService(string name, string serviceName, int port, int ttl, IReadOnlyList<IReadOnlyDictionary<string, string>> properties)
        {
            Name = name;
            ServiceName = serviceName;
            Port = port;
            Ttl = ttl;
            Properties = properties;
        }

        public string Name { get; }

        public string ServiceName { get; }

        public int Port { get; }

        public int Ttl { get; }

        public IReadOnlyList<IReadOnlyDictionary<string, string>> Properties { get; }
    }
    public class ZeroconfHost
    {
        public ZeroconfHost(string id, string displayName, string ipAddress, IReadOnlyList<string> ipAddresses, IReadOnlyDictionary<string, ZeroconfService> services)
        {
            Id = id;
            DisplayName = displayName;
            IpAddress = ipAddress;
            IPAddresses = ipAddresses;
            Services = services;
        }
        public string Id { get; }
        public string DisplayName { get;  }
        public string IpAddress { get; }
        public IReadOnlyList<string> IPAddresses { get; }


        public IReadOnlyDictionary<string, ZeroconfService> Services { get; }
    }

    public interface IZeroconfDiscovery
    {
        Task<IEnumerable<ZeroconfHost>> DiscoverAsync(string serviceType, TimeSpan timeout);
    }
}
