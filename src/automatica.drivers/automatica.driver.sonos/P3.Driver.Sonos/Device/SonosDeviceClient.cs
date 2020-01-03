using System.Net.Http;
using System.Threading.Tasks;
using P3.Driver.Sonos.Upnp;

namespace P3.Driver.Sonos.Device
{
    internal class SonosDeviceClient
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public async Task<string> GetDeviceDescriptionXmlAsync(string ipAddress)
        {
            var uri = new SonosUri(ipAddress, "xml/device_description.xml").ToUri();

            var response = await HttpClient.GetAsync(uri);

            return await response.Content.ReadAsStringAsync();
        }
    }
}