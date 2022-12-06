using System;
using System.Threading.Tasks;
using FroniusSolarClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace FroniusSolarConsoleTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            


            var client = new SolarClient("192.168.8.5", 1, NullLogger.Instance);

            var commonInverterData = client.GetCommonInverterData();
            var p3 = client.GetP3InverterData();
            var powerFlow = client.GetPowerFlowRealtimeData();

            Console.ReadLine();
        }
    }
}
