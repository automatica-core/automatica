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

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            KnxHelper.Logger = new ConsoleLogger();//NullLogger.Instance;

            var connection = new KnxConnectionTunneling(IPAddress.Parse("192.168.8.3"), 3671, IPAddress.Parse(NetworkHelper.GetActiveIp()));
            connection.UseNat = false;

            connection.Start();

            var cancellationTokenSource = new CancellationTokenSource();
            var task = CreateNewTask(cancellationTokenSource, connection);

            connection.OnConnected += (s, e) => task.Start();
            connection.OnDatagramReceived += OnDatagramReceived;
            connection.OnDisconnected += (s, e) =>
            {
                connection.Stop();
                cancellationTokenSource.Cancel();

                Thread.Sleep(1000);
                cancellationTokenSource = new CancellationTokenSource();
                task = CreateNewTask(cancellationTokenSource, connection);

                Console.WriteLine("KNX disconnected...");
                connection.Start();
            };

            Console.ReadLine();
        }

        private static void OnDatagramReceived(object sender, KnxDatgramEventArgs e)
        {
            if (e.Datagram.DestinationAddress == "7/7/7")
            {
                var data = DptTranslator.Instance.FromDataPoint("10.001", e.Datagram.Data);
            }
        }

        private static Task CreateNewTask(CancellationTokenSource cts, KnxConnection connection)
        {
            return new Task(async () =>

            {
                try
                {
                    while (true)
                    {
                        connection.Write("7/2/0",
                            DptTranslator.Instance.ToDataPoint("16.001", "7141.61$"));
                        await Task.Delay(1000);
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }, cts.Token); ;
        }
    }
}
