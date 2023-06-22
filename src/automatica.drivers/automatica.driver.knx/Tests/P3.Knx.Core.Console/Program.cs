using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver.Utility.Network;
using Microsoft.Extensions.Logging;
using P3.Knx.Core.Driver;
using P3.Knx.Core.Driver.IpSecure;
using P3.Knx.Core.Driver.Tunneling;

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
            System.Console.WriteLine($"Datagram on {datagram.DestinationAddress}");
            return Task.CompletedTask;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("Hello World!");

            KnxHelper.Logger = new ConsoleLogger();//NullLogger.Instance;

            var connection = new KnxConnectionTunnelingSecure(new KnxEvents(), IPAddress.Parse("192.168.8.151"), 3671,
                IPAddress.Parse(NetworkHelper.GetActiveIp()), "F,;A@B%w", "MWvpeP6?");
           // var connection = new KnxConnectionTunneling(new KnxEvents(), IPAddress.Parse("192.168.8.3"), 3671, IPAddress.Parse(NetworkHelper.GetActiveIp()));
            connection.UseNat = false;

            connection.IpSecureErrorOccured += (sender, eventArgs) =>
            {
                System.Console.WriteLine($"{eventArgs.ErrorType}: {eventArgs.Text}");
            };

            connection.Start();
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
