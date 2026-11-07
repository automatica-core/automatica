using System;
using Knx.Falcon;
using Knx.Falcon.Configuration;
using Knx.Falcon.Logging;
using Knx.Falcon.Sdk;
using System.Net;
using System.Security;
using System.Threading;
using Knx.Falcon.KnxnetIp;
using Microsoft.Extensions.Logging;
using P3.Driver.Knx.DriverFactory.Logging;

namespace P3.Knx.Core.Console
{

    class ConsoleLogger : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            System.Console.WriteLine(formatter(state, exception));
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("Hello World!");

            var ipAddress = "dev.automaticaremote.com";
            var port = 1030;
            var useNatValue = true;

            Logger.Factory = new FalconLoggerFactory(new ConsoleLogger());

            var ip = new IpTunnelingConnectorParameters(ipAddress, ipPort: port, useNat: useNatValue, protocolType: IpProtocol.Tcp)
            {
                AutoReconnect = false
            };

            ip.IndividualAddress = IndividualAddress.Parse("1.3.252");
            ip.DeviceAuthenticationCodeHash =
                IpUnicastConnectorParameters.GetDeviceAuthenticationCodeHash(PasswordToSecureString(""));
            ip.UserPasswordHash =
                IpUnicastConnectorParameters.GetUserPasswordHash(PasswordToSecureString(""));
            ip.UserId = (byte)5;

            var tunneling = new KnxBus(ip);

            tunneling.Connect();
            while (true)
            {
                //connection.Stop();

                //connection = new KnxConnectionTunneling(new KnxEvents(), IPAddress.Parse("192.168.8.3"), 3671,
                //    IPAddress.Parse(NetworkHelper.GetActiveIp()));
                //connection.UseNat = false;

                //connection.Start();

                Thread.Sleep(500);
            }
        }


        private static SecureString PasswordToSecureString(string password)
        {
            var secureString = new SecureString();

            foreach (char c in password)
                secureString.AppendChar(c);
            return secureString;

        }

    }
}
