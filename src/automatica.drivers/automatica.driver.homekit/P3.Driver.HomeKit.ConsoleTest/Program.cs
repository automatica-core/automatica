using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Microsoft.Extensions.Logging;
using P3.Driver.HomeKit.Hap.Model;

namespace P3.Driver.HomeKit.ConsoleTest
{
    class ConsoleLogger : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var exceptionMessage = exception != null ? $"Exception occured: {exception}.{Environment.NewLine}" : String.Empty;
             Console.WriteLine($"{DateTime.Now:o}: [Th: {Thread.CurrentThread.ManagedThreadId}] {exceptionMessage}{formatter.Invoke(state, exception)}");
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
            var logger = new ConsoleLogger();
            HomeKitServer.Init(logger);

            
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

            var objId = new Guid("e00b1712-a2bf-4420-b3e5-c9b9a0c4a5f2").ToString().Replace("-", "");
            string code = $"{CreateRandom(100, 999)}-{CreateRandom(10, 99)}-{CreateRandom(100, 999)}";
            code = "111-23-222";

            Console.WriteLine($"Code is {code}");

            var homekitId =
                $"{objId[0]}{objId[1]}:{objId[2]}{objId[3]}:{objId[4]}{objId[5]}:{objId[6]}{objId[7]}:{objId[8]}{objId[9]}:{objId[10]}{objId[11]}";

            var homekit = new HomeKitServer(logger, 54321, "AAAAA", ltsk, ltpk, homekitId,
                code, "demo", homekitId, 8, "0.0.1");

            homekit.PairingCompleted += Homekit_PairingCompleted;

            var lightAccessory = AccessoryFactory.CreateLightBulbAccessory(2, "Light1", "AutomaticaCore", "123456", true, 100);
            var lightAccessory2 = AccessoryFactory.CreateLightBulbAccessory(3, "Light2", "AutomaticaCore", "12345", false, 0);
            var lightAccessory3 = AccessoryFactory.CreateLightBulbAccessory(4, "Light3", "AutomaticaCore", "1234567", false, 0);
            var outletAccessory = AccessoryFactory.CreateOutletAccessory(5, "Outlet", "AutomaticaCore", "123", false);
            var switchAccessory = AccessoryFactory.CreateSwitchAccessory(6, "Switch1", "AutomaticaCore", "1456", false);
            var contactSensorAcceessory = AccessoryFactory.CreateContactSensorAccessory(7, "Contact1", "AutomaticaCore", "d123456", 0);
            var contactSensorAcceessory2 = AccessoryFactory.CreateContactSensorAccessory(8, "Contact2", "AutomaticaCore", "1b23456", 0);
            var contactSensorAcceessory3 = AccessoryFactory.CreateContactSensorAccessory(9, "Contact3", "AutomaticaCore", "1b23456d", 0);
            var lightAccessory4 = AccessoryFactory.CreateLightBulbAccessory(10, "Light4", "AutomaticaCore", "asdf1234567", false, 0);

            homekit.AddAccessory(lightAccessory);
            homekit.AddAccessory(lightAccessory2);
            homekit.AddAccessory(lightAccessory3);
            homekit.AddAccessory(lightAccessory4);
            homekit.AddAccessory(outletAccessory);
            homekit.AddAccessory(switchAccessory);
            homekit.AddAccessory(contactSensorAcceessory);
            homekit.AddAccessory(contactSensorAcceessory2);
            homekit.AddAccessory(contactSensorAcceessory3);
            homekit.AddAccessory(AccessoryFactory.CreateTemperatureSensorAccessory(11, "Temperature1", "AutomaticaCore", "asdfasdf", 22.9));

            homekit.ValueChanged += (sender, eventArgs) =>
                Console.WriteLine($"Value changed {eventArgs.Characteristic.Value}");

            homekit.PairingCompleted += (sender, eventArgs) =>
            {
                Console.WriteLine($"Paring completed...");
            };

            await homekit.Start();


            ThreadPool.QueueUserWorkItem(async state =>
            {
                var value = true;
                while (true)
                {
                    var onoff = contactSensorAcceessory.Services[1].Characteristics.SingleOrDefault(a => a.Id == 8);
                    homekit.SetCharacteristicValue(onoff, value);

                    value = !value;
                    await Task.Delay(1000);
                }
            });

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
