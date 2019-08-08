using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Console;
using P3.Driver.EnOcean.Data.Packets;
using Serilog.Events;

namespace P3.Driver.EnOcean.Console
{
    static class Program
    {
        static void Main(string[] args)
        {
            Task.Factory.StartNew(Run);

            System.Console.ReadLine();
        }

        static async void Run()
        {
            var logger = new ConsoleLogger("EnOnean", (s, level) => true, false);
            Logger.Logger.Instance = logger;

            var driver = new Driver("COM4");
            await driver.Open();
             
             driver.StartTeachInMode();

            var telegram = new RadioErp1Packet(Rorg.Rps, new ReadOnlyMemory<byte>(new byte[] { 0 }));
            var rec = await driver.SendTelegram(telegram);

            driver.TelegramReceived += Driver_TelegramReceived;
            driver.TeachInReceived += Driver_TeachInReceived;
            System.Console.WriteLine("rec...");
        }

        private static void Driver_TeachInReceived(object sender, Data.PacketReceivedEventArgs e)
        {
            
        }

        private static void Driver_TelegramReceived(object sender, Data.PacketReceivedEventArgs e)
        {
           // System.Console.WriteLine(e.Telegram.ToPacket().ToString());
        }
    }

}
