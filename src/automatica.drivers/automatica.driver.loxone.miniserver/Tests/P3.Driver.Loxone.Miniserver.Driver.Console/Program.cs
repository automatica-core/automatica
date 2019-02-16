using System;
using System.Net;

namespace P3.Driver.Loxone.Miniserver.Driver.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("Hello World!");

            var lox = new LoxoneMiniserverConnection("demominiserver.loxone.com", 7779, "web", "web");
            lox.Connect();

            System.Console.ReadLine();
        }
    }
}
