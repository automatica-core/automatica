using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace P3.Knx.Core.Console
{
    class ConsoleLogger : ILogger {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            System.Console.WriteLine(formatter.Invoke(state, exception));
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

   

    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("Hello World!");

           while (true)
            {
                //connection.Stop();

                //connection = new KnxConnectionTunneling(new KnxEvents(), IPAddress.Parse("192.168.8.3"), 3671,
                //    IPAddress.Parse(NetworkHelper.GetActiveIp()));
                //connection.UseNat = false;

                //connection.Start();

                Thread.Sleep(500);
            }

           

            System.Console.ReadLine();
        }
    }
}
