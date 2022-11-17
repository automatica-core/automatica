using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.Driver.Monitor;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.ModBusDriver;
using P3.Driver.ModBusDriver.Master.Rtu;
using P3.Driver.ModBusDriver.Slave.Tcp;

namespace ConsoleTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            //var slave = new ModBusSlaveTcpDriver(new ModBusSlaveTcpConfig
            //{
            //    DeviceId = 1, DeviceIds = new List<int>() {1}, IgnoreDeviceId = false, Port = 502
            //}, new EmptyTelegramMonitorInstance(), NullLogger.Instance);
            //slave.Open();

            //slave.SetInputRegister(1, 0, 255);

            var baud = 9600;
            var port = "COM10";
            var dataBits = 8;
            var stopBits = 1;
            var parity = "NoParity";

            ModBus.Logger = new ConsoleLogger();

            var modBusDriver = new ModBusMasterRtuDriver(new ModBusMasterRtuConfig()
            {
                Port = port,
                Baud = baud,
                DataBits = dataBits,
                Parity = parity,
                StopBits = stopBits,
                Timeout = 5000
            }, new EmptyTelegramMonitorInstance());

            modBusDriver.Open();

            while (true)
            {
              //  var response = await modBusDriver.ReadInputRegisters(23, 8260, 2);

                var res = await modBusDriver.WriteHoldingRegister(23, 1, 701);

                await Task.Delay(1000);
            }

            Console.ReadLine();
        }
    }
}
