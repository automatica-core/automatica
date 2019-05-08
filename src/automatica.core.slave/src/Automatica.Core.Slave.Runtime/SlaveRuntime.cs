using Automatica.Core.Slave.Abstraction;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Core.Slave.Runtime
{
    public class SlaveRuntime : IHostedService
    {
        private readonly IMqttClientOptions _options;
        private IMqttClient _mqttClient;
        private readonly DockerClient _dockerClient;

        private IList<ImagesListResponse> _localImages;

        private string _slaveId;

        private readonly string _masterAddress;
        private readonly string _clientKey;

        public SlaveRuntime(IServiceProvider services)
        {
            var config = services.GetRequiredService<IConfiguration>();

            _masterAddress = config["server:master"];
            _clientKey = config["server:clientKey"];

            _slaveId = config["server:clientId"];
            _options = new MqttClientOptionsBuilder()
                   .WithClientId(_slaveId)
                //   .WithWebSocketServer("localhost:5001/mqtt")
                   .WithTcpServer(_masterAddress, 1883)
                   .WithCredentials(_slaveId, _clientKey)
                   .WithCleanSession()
                   .Build();

            MqttNetGlobalLogger.LogMessagePublished += (s, e) =>
            {
                var trace = $">> [{e.TraceMessage.Timestamp:O}] [{e.TraceMessage.ThreadId}] [{e.TraceMessage.Source}] [{e.TraceMessage.Level}]: {e.TraceMessage.Message}";
                if (e.TraceMessage.Exception != null)
                {
                    trace += Environment.NewLine + e.TraceMessage.Exception.ToString();
                }

                Console.WriteLine(trace);
            };

            _mqttClient = new MqttFactory().CreateMqttClient();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _dockerClient = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();
            }
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _dockerClient = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
                
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _localImages = await _dockerClient.Images.ListImagesAsync(new ImagesListParameters());

            await _mqttClient.ConnectAsync(_options);

            var topic = $"slave/{_slaveId}/action";
            await _mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).WithExactlyOnceQoS().Build());


            _mqttClient.ApplicationMessageReceived += async (sender, e) =>
            {
                if(MqttTopicFilterComparer.IsMatch(topic, e.ApplicationMessage.Topic))
                {
                    var json = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    var action = JsonConvert.DeserializeObject<ActionRequest>(json);

                    await ExecuteAction(action);
                }
            };


        }

        private ImagesListResponse FindImage(string imageName, string imageTag)
        {
            if (_localImages != null)
            {
                foreach (var image in _localImages)
                {
                    if (image.RepoTags.Contains($"{imageName}:{imageTag}"))
                    {
                        return image;
                    }
                }
            }

            return null;
        }

        private async Task ExecuteAction(ActionRequest action)
        {
            switch (action.Action)
            {
                case SlaveAction.Start:
                    await StartImage(action.ImageSource, action.ImageName, action.Tag);
                    break;
            }
        }

        public static string Arch
        {
            get
            {
                var arch = "";

                switch (RuntimeInformation.OSArchitecture)
                {
                    case Architecture.Arm:
                        arch += "arm";
                        break;
                    case Architecture.Arm64:
                        arch += "arm64";
                        break;
                    case Architecture.X64:
                        arch += "x64";
                        break;
                    case Architecture.X86:
                        arch += "x86";
                        break;
                }

                return arch;
            }
        }

        private async Task StartImage(string imageSource, string imageName, string imageTag)
        {
            var imageFullName = $"{imageName}:{imageTag}-{Arch}";
            var image = FindImage(imageName, $"{imageTag}-{Arch}");

            if (image == null)
            {
                var imageCreateParams = new ImagesCreateParameters()
                {
                    FromImage = imageName,
                    Tag = imageTag
                };

                if (!String.IsNullOrEmpty(imageSource))
                {
                    imageCreateParams.FromSrc = imageSource;
                }

                await _dockerClient.Images.CreateImageAsync(imageCreateParams, new AuthConfig(), new ImageProgress());


                image = FindImage(imageName, imageTag);

                if (image == null)
                {
                    throw new OperationCanceledException();
                }
            }

            try
            {
                var response = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters()
                {
                    Image = $"{imageFullName}",
                    AttachStderr = false,
                    AttachStdin = false,
                    AttachStdout = false,
                    Env = new[] { $"AUTOMATICA_SLAVE_MASTER=172.20.0.2", $"AUTOMATICA_SLAVE_USER={_slaveId}", $"AUTOMATICA_SLAVE_PASSWORD={_clientKey}" },
                    HostConfig = new HostConfig
                    {
                        PortBindings = new Dictionary<string, IList<PortBinding>> {
                            {
                                "1833", new List<PortBinding> {
                                    new PortBinding { HostPort = "1833" }
                                }
                            }
                        },
                        NetworkMode = "host"
                    }
                });


                await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters { });

            }
            catch (Exception e)
            {

            }
         
        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
