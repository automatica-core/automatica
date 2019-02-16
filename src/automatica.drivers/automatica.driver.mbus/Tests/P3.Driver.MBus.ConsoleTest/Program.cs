using System;
using Microsoft.Extensions.Logging;

namespace P3.Driver.MBus.ConsoleTest
{
    static class Program
    {
        static void Main(string[] args)
        {
            var mbusTest = new MBusTest();
            mbusTest.Start();

            Console.ReadLine();

            mbusTest.Stop();
        }

    }
}
