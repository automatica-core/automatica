using Automatica.Core.Driver.Utility.Network;
using P3.Knx.Core.Driver;
using P3.Knx.Core.Driver.Tunneling;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Knx.Core.DPT;

namespace P3.Knx.Core
{
    class ConsoleLogger : ILogger {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Console.WriteLine(formatter.Invoke(state, exception));
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

    internal class KnxEvents : IKnxEvents
    {
        public Task Connected()
        {
            return Task.CompletedTask;
        }

        public Task Disconnected()
        {
            return Task.CompletedTask;
        }

        public Task OnDatagram(KnxDatagram datagram)
        {
            return Task.CompletedTask;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            KnxHelper.Logger = new ConsoleLogger();//NullLogger.Instance;

            var connection = new KnxConnectionTunneling(new KnxEvents(), IPAddress.Parse("192.168.8.3"), 3671, IPAddress.Parse(NetworkHelper.GetActiveIp()));
            connection.UseNat = false;

            connection.Start();
            while (true)
            {
                connection.Stop();

                connection = new KnxConnectionTunneling(new KnxEvents(), IPAddress.Parse("192.168.8.3"), 3671,
                    IPAddress.Parse(NetworkHelper.GetActiveIp()));
                connection.UseNat = false;

                connection.Start();

                Thread.Sleep(500);
            }

           

            Console.ReadLine();
        }
    }
}
