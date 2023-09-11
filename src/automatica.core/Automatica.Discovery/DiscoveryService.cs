using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Automatica.Core.Driver.Utility.Network;
using Rssdp;

namespace Automatica.Discovery
{
    public class DiscoveryService : IHostedService
    {
        private readonly ILogger<DiscoveryService> _logger;
        public string AutomaticaUuid { get;}
        private readonly SsdpDeviceLocator _deviceLocator;

        public const string DeviceTypeName = "Automatica.Core.Server";
        public const string DeviceTypeNamespace = "Automatica-Core";
        public static readonly string UrnSearch = $"urn:{DeviceTypeNamespace}:device:{DeviceTypeName}:1";
        public Uri Location { get; }

        private readonly SsdpDevicePublisher _publisher;
        private readonly int _port;
        public SsdpRootDevice Device { get; private set; }

        public DiscoveryService(ILogger<DiscoveryService> logger)
        {
            _logger = logger;
            try
            {
                _publisher = new SsdpDevicePublisher();
                _deviceLocator = new SsdpDeviceLocator { NotificationFilter = UrnSearch };

                _deviceLocator.StartListeningForNotifications();

                _port = Convert.ToInt32(ServerInfo.WebPort);

                AutomaticaUuid = ServerInfo.ServerUid.ToString();

                Location = new Uri($"http://{NetworkHelper.GetActiveIp()}:{_port}/webapi/discovery");
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not init SSDP Publisher {e}");
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
             PublishDevice();
            return Task.CompletedTask;
        }

        public int Port => _port;

        public Task<IEnumerable<DiscoveredSsdpDevice>> Scan()
        {
            return _deviceLocator.SearchAsync();
        }

        public void PublishDevice()
        {
            // As this is a sample, we are only setting the minimum required properties.
            Device = new SsdpRootDevice()
            {
                CacheLifetime = TimeSpan.FromMinutes(30),
                Location = Location, 
                DeviceTypeNamespace = DeviceTypeNamespace,
                DeviceType = DeviceTypeName,
                FriendlyName = $"Automatica.Core Server ({ServerInfo.GetServerVersion()})",
                Manufacturer = "p3root",
                ModelName = "Automatica.Core Server",
                Uuid = AutomaticaUuid
            };

            Device.CustomProperties.Add(new SsdpDeviceProperty()
            {
                Name = "URL",
                Namespace = "server-url",
                Value = $"http://{NetworkHelper.GetActiveIp()}:{_port}"
            });

            Device.CustomResponseHeaders.Add(new CustomHttpHeader("server-url", $"http://{NetworkHelper.GetActiveIp()}:{_port}"));


            Device.CustomProperties.Add(new SsdpDeviceProperty()
            {
                Name = "Version",
                Namespace = "server-version",
                Value = ServerInfo.GetServerVersion()
            });

            Device.CustomResponseHeaders.Add(new CustomHttpHeader("server-version", ServerInfo.GetServerVersion()));

            Device.CustomProperties.Add(new SsdpDeviceProperty()
            {
                Name = "Name",
                Namespace = "server-name",
                Value = $"Automatica.Core XYZ"
            });

            Device.CustomResponseHeaders.Add(new CustomHttpHeader("server-name", $"Automatica.Core XYZ"));

            _publisher?.AddDevice(Device);
            
        }
       
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
