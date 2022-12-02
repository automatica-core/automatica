using System;
using System.Net;
using System.Threading.Tasks;

namespace P3.Driver.Loxone.Miniserver.Driver.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            System.Console.WriteLine("Hello World!");

            var lox = new LoxoneMiniserverConnection("demominiserver.loxone.com", 7779, "web", "web", new ConsoleLogger();
            await lox.Connect();

            System.Console.ReadLine();
        }
    }
}
