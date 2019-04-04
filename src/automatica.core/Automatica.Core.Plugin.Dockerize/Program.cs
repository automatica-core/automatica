using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.Plugin.Dockerize.Factories;
using Microsoft.Extensions.Logging.Abstractions;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Threading.Tasks;

namespace Automatica.Core.Plugin.Dockerize
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Run plugin dockerized");

            var factories = await Dockerize.Init<DriverFactory>(args[0], NullLogger.Instance);

            foreach(var factory in factories)
            {
                var options = new MqttClientOptionsBuilder()
                    .WithClientId(factory.FactoryGuid.ToString())
                    .WithTcpServer("localhost")
                    .WithCleanSession()
                    .Build();

                var client = new MqttFactory().CreateMqttClient();
                await client.ConnectAsync(options);

                await Dockerize.InitDriverFactory(client, factory, new RemoteNodeTemplatesFactory());
            }

            Console.ReadLine();
        }
    }
}
