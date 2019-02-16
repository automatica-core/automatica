using Microsoft.Extensions.Logging;
using P3.Driver.HueBridge.Api;
using Automatica.Core.Driver.Utility.Network;
using Rssdp;
using System;
using System.Threading.Tasks;

namespace P3.Driver.HueBridge
{
    public class HueDiscoveryService
    {
        public string AutomaticaUuid { get; }
        
        public const string DeviceTypeName = "basic";
        public const string DeviceTypeNamespace = "schemas-upnp-org";
        public static readonly string UrnSearch = $"urn:{DeviceTypeNamespace}:device:{DeviceTypeName}:1";
        public Uri Location { get; }

        private readonly Discovery _publisher;
        private readonly int _port;
        public SsdpRootDevice Device { get; private set; }

        public HueDiscoveryService(int huePort, ILogger logger)
        {
            try
            {
                _publisher = new Discovery(logger);
               
                _port = huePort;
                AutomaticaUuid = Guid.NewGuid().ToString();

               Location = new Uri($"http://{NetworkHelper.GetActiveIp()}:{_port}/api/discovery");
            }
            catch (Exception e)
            {
                logger.LogError($"Could not init SSDP Publisher {e}");
            }

        }

        public Task StartAsync()
        {
            PublishDevice();
            return Task.CompletedTask;
        }

        public int Port => _port;


        public void PublishDevice()
        {
            // As this is a sample, we are only setting the minimum required properties.
            Device = new SsdpRootDevice()
            {
                CacheLifetime = TimeSpan.FromMinutes(30),
                Location = Location,
                DeviceTypeNamespace = DeviceTypeNamespace,
                DeviceType = DeviceTypeName,
                FriendlyName = $"Philips hue (Automatica.Core)",
                Manufacturer = "Royal Philips Electronics",
                ModelName = "Philips hue bridge 2012",
                ModelDescription = "Philips hue Personal Wireless Lighting",
                ModelNumber = "BSB003",
                Uuid = AutomaticaUuid,
                SerialNumber = "FF27EBFFFED03BFF",
                PresentationUrl = new Uri("/", UriKind.Relative)
            };

            Device.UrlBase = new Uri($"http://{NetworkHelper.GetActiveIp()}:{_port}");

            Device.Icons.Add(new SsdpDeviceIcon()
            {
                MimeType = "image/png",
                Height = 48,
                Width = 48,
                ColorDepth = 24,
                Url = new Uri("hue_logo_0.png", UriKind.Relative)
            });

            Device.CustomProperties.Add(new SsdpDeviceProperty()
            {
                Name = "hue-bridgeid",
                Namespace = "hue-bridgeid",
                Value = $"93eadbeef13"
            });

            Device.CustomResponseHeaders.Add(new CustomHttpHeader("hue-bridgeid", $"FF27EBFFFED03BFF"));

            _publisher?.AddDevice(Device);
        }

    }
}
