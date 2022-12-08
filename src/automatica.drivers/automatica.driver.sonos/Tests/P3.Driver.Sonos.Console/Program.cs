using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.Sonos.Discovery;

namespace P3.Driver.Sonos.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var discover = await SonosDiscovery.DiscoverSonos();

            var controller = new SonosControllerFactory().Create("192.168.8.105", NullLogger.Instance);

            var data = await controller.GetMediaInfoAsync();
        }
    }
}
