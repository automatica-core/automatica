using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Automatica.Core.Base.TelegramMonitor;
using Microsoft.Extensions.Logging;
using P3.Driver.ModBusDriver.Master.Tcp;
using Xunit;

namespace P3.Driver.ModBus.Tests
{
    /// <summary>
    /// Each modbus connection must log into the log of its own driver. The static ModBus.Logger is overwritten by every
    /// modbus driver that is created, so the errors of one device landed in the log of another one.
    /// </summary>
    public class ModBusMasterLoggerTests
    {
        private sealed class CaptureLogger : ILogger
        {
            public ConcurrentQueue<string> Messages { get; } = new();

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                Messages.Enqueue(formatter(state, exception));
            }
        }

        private static int ClosedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static ModBusMasterTcpDriver CreateMaster(int port, ILogger logger)
        {
            return new ModBusMasterTcpDriver(new ModBusMasterTcpConfig
            {
                IpAddress = IPAddress.Loopback,
                Port = (short)port,
                Timeout = 500
            }, new EmptyTelegramMonitorInstance(), logger);
        }

        [Fact]
        public void TwoConnections_LogIntoTheirOwnLogger()
        {
            var firstPort = ClosedPort();
            var secondPort = ClosedPort();
            var firstLogger = new CaptureLogger();
            var secondLogger = new CaptureLogger();

            var first = CreateMaster(firstPort, firstLogger);
            var second = CreateMaster(secondPort, secondLogger);

            Assert.False(first.Open());
            Assert.False(second.Open());

            // the config keeps the port as a short (ports above 32767 wrap), the connect error ends the address with a line break
            var firstAddress = $":{(short)firstPort}\n";
            var secondAddress = $":{(short)secondPort}\n";

            Assert.Contains(firstLogger.Messages, m => m.Contains(firstAddress));
            Assert.DoesNotContain(firstLogger.Messages, m => m.Contains(secondAddress));

            Assert.Contains(secondLogger.Messages, m => m.Contains(secondAddress));
            Assert.DoesNotContain(secondLogger.Messages, m => m.Contains(firstAddress));
        }

        [Fact]
        public void Connection_WithoutLogger_StillWorks()
        {
            var master = CreateMaster(ClosedPort(), null);

            Assert.False(master.Open());
        }
    }
}
