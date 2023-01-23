using Microsoft.Extensions.Logging;
using P3.Driver.Sonos.Upnp.Services;

namespace P3.Driver.Sonos
{
    public class SonosControllerFactory
    {
        public SonosController Create(string ipAddress, ILogger logger)
        {
            return new SonosController(new AvTransportService(ipAddress, logger),
                new RenderingControlService(ipAddress),
                new ContentDirectoryService(ipAddress));
        }
    }
}