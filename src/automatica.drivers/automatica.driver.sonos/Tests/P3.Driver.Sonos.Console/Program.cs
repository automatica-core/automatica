using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.Sonos.Device;
using P3.Driver.Sonos.Discovery;

namespace P3.Driver.Sonos.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var discover = await SonosDiscovery.DiscoverSonos();
            var device = await new SonosDeviceService().GetDeviceAsync("192.168.8.133");


            var controller = new SonosControllerFactory().Create("192.168.8.133", NullLogger.Instance);

            var data = await controller.GetMediaInfoAsync();
            var data1 = await controller.GetMediaInfoAsync();
            var datax = await controller.GetPositionInfoAsync();

            var isPlaying = await controller.GetIsPlayingAsync();
        }
    }
}
