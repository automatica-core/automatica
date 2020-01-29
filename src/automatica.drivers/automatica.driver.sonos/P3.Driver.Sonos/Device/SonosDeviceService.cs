using System.Net.Http;
using System.Threading.Tasks;
using P3.Driver.Sonos.Models;
using P3.Driver.Sonos.Upnp;

namespace P3.Driver.Sonos.Device
{
    public class SonosDeviceService
    {
        private readonly SonosDeviceFactory _sonosDeviceFactory;
        private readonly SonosDeviceClient _sonosDeviceClient;

        public SonosDeviceService()
        {
            _sonosDeviceFactory = new SonosDeviceFactory();
            _sonosDeviceClient = new SonosDeviceClient();
        }

        public async Task<HttpResponseMessage> RebootAsync(string ipAddress)
        {
            using (var client = new HttpClient())
            {
                var uri = new SonosUri(ipAddress, "reboot").ToUri();

                return await client.GetAsync(uri);
            }
        }

        public async Task<SonosDevice> GetDeviceAsync(string ipAddress)
        {
            var xml = await _sonosDeviceClient.GetDeviceDescriptionXmlAsync(ipAddress);

            return _sonosDeviceFactory.CreateFor(ipAddress, xml);
        }
    }
}
