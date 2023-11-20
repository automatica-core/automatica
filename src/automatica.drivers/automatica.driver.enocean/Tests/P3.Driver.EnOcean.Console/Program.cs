using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using P3.Driver.EnOcean.Data.Packets;
using P3.Driver.EnOcean.Serial;

namespace P3.Driver.EnOcean.Console
{
    public class ConsoleLogger : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            try
            {
                var msg = formatter.Invoke(state, exception);
                System.Console.WriteLine(msg);
            }
            catch
            {
                //can be ignored
            }
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
    static class Program
    {
        static void Main(string[] args)
        {
            Task.Factory.StartNew(Run);

            System.Console.ReadLine();
        }

        static async void Run()
        {
            var logger =  new ConsoleLogger();
            Logger.Logger.Instance = logger;

            var driver = new Driver(new SerialStream("COM13"));
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
            System.Console.WriteLine(e.Telegram.ToPacket().ToString());
        }
    }

}
