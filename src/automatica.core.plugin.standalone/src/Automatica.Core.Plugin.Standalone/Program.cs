using System;
using System.Text;
using System.Threading.Tasks;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Mqtt;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.Plugin.Standalone.Dispatcher;
using Automatica.Core.Plugin.Standalone.Factories;
using Microsoft.Extensions.Logging.Abstractions;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;
using Newtonsoft.Json;
using NetStandardUtils;

namespace Automatica.Core.Plugin.Standalone
{
    class Program
    {
        static async Task Main(string[] args)
        {

            while (true)
            {
                try
                {
                    Console.WriteLine($"Run plugin dockerized ({NetStandardUtils.Version.GetAssemblyVersion()})");
                    Console.WriteLine(string.Join(Environment.NewLine, Environment.GetEnvironmentVariables()));

                    var localization = new LocalizationProvider(NullLogger.Instance);
                    var factories = await Dockerize.Init<DriverFactory>(args[0], NullLogger.Instance, localization);

                    await Task.Delay(TimeSpan.FromSeconds(10));

                    var masterAddress = "localhost";

                    var masterEnv = Environment.GetEnvironmentVariable("AUTOMATICA_SLAVE_MASTER");
                    if (!string.IsNullOrEmpty(masterEnv))
                    {
                        masterAddress = masterEnv;
                    }

                    var user = Environment.GetEnvironmentVariable("AUTOMATICA_SLAVE_USER");
                    var password = Environment.GetEnvironmentVariable("AUTOMATICA_SLAVE_PASSWORD");

                    Console.WriteLine($"Trying to connect to {masterAddress} with user {user}");

                    foreach (var factory in factories)
                    {
                        var ndFactory = new RemoteNodeTemplatesFactory();
                        var options = new MqttClientOptionsBuilder()
                            .WithClientId(factory.FactoryGuid.ToString())
                            .WithTcpServer(masterAddress)
                            .WithCredentials(user, password)
                            .WithCleanSession()
                            .Build();

                        var client = new MqttFactory().CreateMqttClient();
                        await client.ConnectAsync(options);
                        await client.SubscribeAsync(new TopicFilterBuilder().WithTopic($"{MqttTopicConstants.CONFIG_TOPIC}/{factory.DriverGuid}").WithExactlyOnceQoS().Build(),
                            new TopicFilterBuilder().WithTopic($"{MqttTopicConstants.DISPATCHER_TOPIC}/#").WithAtLeastOnceQoS().Build());

                        var dispatcher = new MqttDispatcher(client);

                        client.ApplicationMessageReceived += async (sender, e) =>
                        {
                            Console.WriteLine($"received topic {e.ApplicationMessage.Topic}...");

                            if (e.ApplicationMessage.Topic == $"{MqttTopicConstants.CONFIG_TOPIC}/{factory.DriverGuid}")
                            {
                                var json = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                                var dto = JsonConvert.DeserializeObject<NodeInstance>(json);

                                var context = new DriverContext(dto, dispatcher, ndFactory, null, null, NullLogger.Instance, null, null, null, false);
                                var driver = factory.CreateDriver(context);

                                if (driver.BeforeInit())
                                {
                                    driver.Configure();
                                    await driver.Start();
                                }
                             
                            }
                            else if (MqttTopicFilterComparer.IsMatch(e.ApplicationMessage.Topic, $"{MqttTopicConstants.DISPATCHER_TOPIC}/#"))
                            {
                                dispatcher.MqttDispatch(e.ApplicationMessage.Topic, e.ApplicationMessage.Payload);
                            }
                        };
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Error occured, retry in 10 sec\n{e}");
                    await Task.Delay(10000);
                }
            }
        }
    }
}
