using System;
using System.Net;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.Driver.Utility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.MBus.Config;
using P3.Driver.MBus.Frames;
using P3.Driver.MBus.Udp;

namespace P3.Driver.MBus.ConsoleTest
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            var mbusUdp = new MBusUdp(new MBusUdpConfig
            {
                IpAddress = IPAddress.Parse("192.168.8.32"),
                Port = 10030
            }, new EmptyTelegramMonitorInstance(), NullLogger.Instance);

            while (true)
            {
                var frame = await mbusUdp.ReadDevice(20, false, 5000);

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
