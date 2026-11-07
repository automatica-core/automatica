using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Tomidix.NetStandard.Tradfri.Models;

namespace P3.Driver.IkeaTradfri.ConsoleTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");


//            var gateways = await IkeaTradfriDriver.Discover();

            var appName = Guid.NewGuid().ToString();

            var key = IkeaTradfriDriver.GeneratePsk("192.168.8.103", appName, "Ej3Ta2AzrePZ9jcJ");
            
            var con = new IkeaTradfriDriver("192.168.8.103", appName, key.PSK);
            await con.Connect();

            Console.WriteLine("Conncted");

            var devices = await con.LoadDevices();

            await con.RegisterChange(token =>
            {
                Console.WriteLine($"Item {token.Name} sent {token} {token.Control[0].State}");


            }, 65539);

            while (true)
            {
               // await con.SwitchOff(65539);
               await Task.Delay(1000);

            }
        }
    }
}
