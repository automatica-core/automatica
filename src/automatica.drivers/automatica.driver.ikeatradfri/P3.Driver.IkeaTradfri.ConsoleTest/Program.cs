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


            }, DeviceType.ControlOutlet, 65539);

            while (true)
            {
               // await con.SwitchOff(65539);
               await Task.Delay(1000);

            }

            foreach (var dev in devices)
            {
                var deviceType = DeviceType.ControlOutlet;

                if (dev.DeviceType == DeviceType.ControlOutlet)
                {

                    await con.SwitchOn(dev.ID);
                    await con.SwitchOff(dev.ID);
                    await con.SwitchOn(dev.ID);
                    continue;
                }
                else if (dev.DeviceType == DeviceType.Remote)
                {
                    continue;
                }
                await con.RegisterChange(token =>
                {
                    Console.WriteLine($"Item {dev.Name} sent {token}");


                }, deviceType, dev.ID);

            }

            Console.ReadLine();
        }
    }
}
