using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Console;
using P3.Driver.HomeKit.Hap.Model;

namespace P3.Driver.HomeKit.ConsoleTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            HomeKitServer.Init();

            var logger = new ConsoleLogger("mylogger", (s, level) => true, false);

            string ltpk = null;
            if (File.Exists("LTPK"))
            {
                ltpk = File.ReadAllText("LTPK");
            }

            string ltsk = null;
            if (File.Exists("LTSK"))
            {
                ltsk = File.ReadAllText("LTSK");
            }

            var homekit = new HomeKitServer(logger, 52765, "11automaticacore-1ad18", ltsk, ltpk, "BA:2F:FA:1A:CD:AA",
                "123-45-555", "AutomaticaCore123", "1AutomaticaCoreBridge");

            homekit.PairingCompleted += Homekit_PairingCompleted;

            var lightAccessory = AccessoryFactory.CreateLightBulbAccessory("Light1", "AutomaticaCore", "123456", false);
            var outletAccessory = AccessoryFactory.CreateOutletAccessory("Outlet", "AutomaticaCore", "123456", false);
            var switchAccessory = AccessoryFactory.CreateSwitchAccessory("Switch1", "AutomaticaCore", "123456", false);
            var contactSensorAcceessory = AccessoryFactory.CreateContactSensorAccessory("Contact1", "AutomaticaCore", "123456", 0);

            homekit.AddAccessory(lightAccessory);
            homekit.AddAccessory(outletAccessory);
            homekit.AddAccessory(switchAccessory);
            homekit.AddAccessory(contactSensorAcceessory);
            homekit.AddAccessory(AccessoryFactory.CreateTemperatureSensorAccessory("Temperature1", "AutomaticaCore", "asdfasdf", 22.9));

            homekit.ValueChanged += (sender, eventArgs) =>
                Console.WriteLine($"Value changed {eventArgs.Characteristic.Value}");

            await homekit.Start();

            Console.ReadLine();

            var onoff = lightAccessory.Services[1].Characteristics.SingleOrDefault(a => a.Id == 8);
            homekit.SetCharacteristicValue(onoff, true);

            Console.ReadLine();

            await homekit.Stop();
        }

        private static void Homekit_PairingCompleted(object sender, Hap.PairSetupCompleteEventArgs e)
        {
            File.WriteAllText("LTPK", e.Ltpk);
            File.WriteAllText("LTSK", e.Ltsk);
        }
    }
}
