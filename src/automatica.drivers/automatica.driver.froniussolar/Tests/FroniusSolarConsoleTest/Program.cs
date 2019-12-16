using System;
using System.Threading.Tasks;
using P3.Driver.FroniusSolar;

namespace FroniusSolarConsoleTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var solar = new SolarApi("192.168.8.5");
            var response = await solar.GetInverterInfo();


            Console.ReadLine();
        }
    }
}
