using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using P3.Driver.HomeKit.Hap.Model;

namespace P3.Driver.HomeKit.ConsoleTest
{
    class ConsoleLogger : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Console.WriteLine(formatter.Invoke(state, exception));
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            HomeKitServer.Init();

            var logger = new ConsoleLogger();

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

            ServerInfo.ServerUid = Guid.NewGuid();

            var objId = Guid.NewGuid().ToString().Replace("-", "");
            string code = $"{CreateRandom(100, 999)}-{CreateRandom(10, 99)}-{CreateRandom(100, 999)}";

            Console.WriteLine($"Code is {code}");

            var homekitId =
                $"{objId[0]}{objId[1]}:{objId[2]}{objId[3]}:{objId[4]}{objId[5]}:{objId[6]}{objId[7]}:{objId[8]}{objId[9]}:{objId[10]}{objId[11]}";

            var homekit = new HomeKitServer(logger, 52634, "HomeKitName", ltsk, ltpk, homekitId,
                code, "demo", "demo"+homekitId);

            homekit.SetConfigVersion(5);

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

            homekit.PairingCompleted += (sender, eventArgs) =>
            {
                Console.WriteLine($"Paring completed...");
            };

            await homekit.Start();

            Console.ReadLine();

            var onoff = lightAccessory.Services[1].Characteristics.SingleOrDefault(a => a.Id == 8);
            homekit.SetCharacteristicValue(onoff, true);

            Console.ReadLine();

            await homekit.Stop();
        }

        private static int CreateRandom(int from, int to)
        {
            Random randomNumber = new Random();
            return randomNumber.Next(from, to);
        }

        private static void Homekit_PairingCompleted(object sender, Hap.PairSetupCompleteEventArgs e)
        {
            File.WriteAllText("LTPK", e.Ltpk);
            File.WriteAllText("LTSK", e.Ltsk);
        }
    }
}
