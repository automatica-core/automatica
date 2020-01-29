using P3.Driver.Sonos.Upnp.Services;

namespace P3.Driver.Sonos
{
    public class SonosControllerFactory
    {
        public SonosController Create(string ipAddress)
        {
            return new SonosController(new AvTransportService(ipAddress),
                new RenderingControlService(ipAddress),
                new ContentDirectoryService(ipAddress));
        }
    }
}