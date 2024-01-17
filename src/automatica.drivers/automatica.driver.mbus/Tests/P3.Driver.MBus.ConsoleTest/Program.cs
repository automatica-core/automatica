using System;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.Driver.Utility;
using P3.Driver.MBus.Config;
using P3.Driver.MBus.Frames;
using P3.Driver.MBus.Serial;

namespace P3.Driver.MBus.ConsoleTest
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            var mbus = new MBusSerial(new MBusSerialConfig
            {
               Port = "COM10",
               Baudrate = 300,
               ResetBeforeRead = true
            }, new EmptyTelegramMonitorInstance(), new ConsoleLogger());


            while (true)
            {
                var frame = await mbus.ReadDevice(93, true, 15000);

                if (frame is VariableDataFrame vdf)
                    foreach (var data in vdf.DataBlocks)
                    {
                        
                        Console.WriteLine($"{data.DataInformationField.DataFieldType}: {data.Value} {data.ValueInformationField.Unit} ({Utils.ByteArrayToString(data.Data)})");
                    }


                await Task.Delay(1000);
            }

            var mbusTest = new MBusTest(args[0]);
            await mbusTest.Start();

            Console.ReadLine();


            await mbusTest.Stop();
        }

    }
}
