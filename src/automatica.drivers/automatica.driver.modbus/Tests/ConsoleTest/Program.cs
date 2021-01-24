using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.ModBusDriver.Slave.Tcp;

namespace ConsoleTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var slave = new ModBusSlaveTcpDriver(new ModBusSlaveTcpConfig
            {
                DeviceId = 1, DeviceIds = new List<int>() {1}, IgnoreDeviceId = false, Port = 502
            }, new EmptyTelegramMonitorInstance(), NullLogger.Instance);
            slave.Open();

            slave.SetInputRegister(1, 0, 255);


            Console.ReadLine();
        }
    }
}
