using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using P3.Driver.IkeaTradfri.Models;

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
            con.Connect();

            Console.WriteLine("Conncted");

            var devices = con.LoadDevices();

            while (true)
            {
                await con.SwitchOff(65539);
                await Task.Delay(1000);

            }

            foreach (var dev in devices)
            {
                var deviceType = TradfriDeviceType.LightControl;

                if (dev.ApplicationType == DeviceType.PowerOutlet)
                {

                    await con.SwitchOn(dev.Id);
                    await con.SwitchOff(dev.Id);
                    await con.SwitchOn(dev.Id);
                    continue;
                }
                else if (dev.ApplicationType == DeviceType.Remote || dev.ApplicationType == DeviceType.Unknown)
                {
                    continue;
                }
                con.RegisterChange(token =>
                {
                    Console.WriteLine($"Item {dev.Name} sent {token}");


                    if (token is JArray array)
                    {
                        var valueProp = ((int)TradfriConstAttribute.LightColorHex).ToString();

                        var strValue = array.First()[valueProp].ToString();

                        
                    }
                }, deviceType, dev.Id);

            }

            Console.ReadLine();
        }
    }
}
