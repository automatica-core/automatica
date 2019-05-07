using Automatica.Core.Base.Localization;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Mqtt;
using Automatica.Core.Plugin.Dockerize.Dispatcher;
using Automatica.Core.Plugin.Dockerize.Factories;
using Microsoft.Extensions.Logging.Abstractions;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Automatica.Core.Plugin.Dockerize
{
    class Program
    {
        static async Task Main(string[] args)
        {

            while (true)
            {
                try
                {
                    Console.WriteLine("Run plugin dockerized");
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
                                // _driverInstances.Add(driver);
                                // nodeInstance.State = NodeInstanceState.Initialized;
                                    driver.Configure();
                                    await driver.Start();
                                }
                                else
                                {
                                //  nodeInstance.State = NodeInstanceState.UnknownError;
                            }
                            }
                            else if (MqttTopicFilterComparer.IsMatch(e.ApplicationMessage.Topic, $"{MqttTopicConstants.DISPATCHER_TOPIC}/#"))
                            {
                                dispatcher.MqttDispatch(e.ApplicationMessage.Topic, e.ApplicationMessage.Payload);
                            }
                        };

                        await Dockerize.InitDriverFactory(client, factory, ndFactory);
                        await client.PublishAsync(new MqttApplicationMessage()
                        {
                            Topic = $"{MqttTopicConstants.LOCALIZATIN_TOPIC}/{factory.FactoryGuid}",
                            QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce,
                            Payload = Encoding.UTF8.GetBytes(localization.ToJson())
                        });


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
