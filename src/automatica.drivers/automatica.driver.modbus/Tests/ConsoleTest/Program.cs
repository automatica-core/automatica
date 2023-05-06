using System;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using P3.Driver.ModBusDriver;
using P3.Driver.ModBusDriver.Master.Rtu;
using RJCP.IO.Ports;

namespace ConsoleTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine(JsonConvert.SerializeObject(args));
            ModBus.Logger = new ConsoleLogger();
            var serialTest = new SerialPortStream(args[0], 9600);

            serialTest.Open();

            serialTest.DataReceived += (sender, eventArgs) =>
            {
                var bytes = new byte[serialTest.BytesToRead];

                var data = serialTest.Read(bytes);
                Console.WriteLine(Convert.ToHexString(bytes, 0, data));
            };


            //var slave = new ModBusSlaveTcpDriver(new ModBusSlaveTcpConfig
            //{
            //    DeviceId = 1, DeviceIds = new List<int>() {1}, IgnoreDeviceId = false, Port = 502
            //}, new EmptyTelegramMonitorInstance(), NullLogger.Instance);
            //slave.Open();

            //slave.SetInputRegister(1, 0, 255);

            //var master = new ModBusMasterTcpDriver(new ModBusMasterTcpConfig
            //{
            //    IpAddress = IPAddress.Parse("127.0.0.1"),
            //    Port = 502,
            //    Timeout = 5000
            //}, new EmptyTelegramMonitorInstance());
            //master.Open();

            //var value = await master.ReadRegisters(0, 0, 10);

            //var solarmanDriver = new SolarmanConnection(new SolarmanConfig
            //{
            //    IpAddress = IPAddress.Parse("192.168.8.122"),
            //    Port = 8899,
            //    SolarmanSerialNumber = 2701730339,
            //    Timeout = 5000
            //}, new EmptyTelegramMonitorInstance());

            //solarmanDriver.Open();
            //var data = await solarmanDriver.ReadRegisters(1, 0x0268, 1);

            Console.ReadLine();
            return;
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
                var response = await modBusDriver.ReadInputRegisters(23, 8260, 2);

                //var res = await modBusDriver.WriteHoldingRegister(23, 1, 701);

                await Task.Delay(1000);
            }

            Console.ReadLine();
        }
    }
}
