using System;
using System.Threading.Tasks;
using P3.Driver.EBus.Config;
using P3.Driver.EBus.Interfaces;

namespace P3.Driver.EBus.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var ebus = new EBusTcp(new TcpConfig("192.168.8.7", 5000));
            await ebus.Connect();



            Console.ReadLine();

        }
    }
}
