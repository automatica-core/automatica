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


            var gateways = await IkeaTradfriDriver.Discover();

            var appName = $"45df1d511";
            
            var con = new IkeaTradfriDriver("192.168.8.105", appName, "xAWniaZm74vIhEdZ");
            con.Connect();

            Console.WriteLine("Conncted");

            var devices = con.LoadDevices();

            foreach (var dev in devices)
            {
                var deviceType = TradfriDeviceType.LightControl;

                if (dev.ApplicationType == DeviceType.PowerOutlet)
                {
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
