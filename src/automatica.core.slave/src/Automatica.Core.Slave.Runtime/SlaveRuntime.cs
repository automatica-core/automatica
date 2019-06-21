using Automatica.Core.Slave.Abstraction;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Sockets;
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
        private readonly IDictionary<string, string> _runningImages = new Dictionary<string, string>();

        private string _slaveId;

        private readonly string _masterAddress;
        private readonly string _clientKey;
        private readonly ILogger _logger;

        public SlaveRuntime(IServiceProvider services, ILogger<SlaveRuntime> logger)
        {
            var config = services.GetRequiredService<IConfiguration>();

            _logger = logger;

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

                _logger.LogTrace(trace);
            };

            _mqttClient = new MqttFactory().CreateMqttClient();

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    _dockerClient = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    _dockerClient = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();
                }
                else
                {
                    throw new PlatformNotSupportedException();
                }
            }
            catch(PlatformNotSupportedException)
            {
                throw;
            }
            catch(Exception e)
            {
               _logger.LogError(e, $"Could not connect do docker daemon!");
            }

            
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _localImages = await _dockerClient.Images.ListImagesAsync(new ImagesListParameters());

                await _mqttClient.ConnectAsync(_options);

                var topic = $"slave/{_slaveId}/action";
                await _mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).WithExactlyOnceQoS().Build());


                _mqttClient.ApplicationMessageReceived += async (sender, e) =>
                {
                    if (MqttTopicFilterComparer.IsMatch(topic, e.ApplicationMessage.Topic))
                    {
                        var json = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                        var action = JsonConvert.DeserializeObject<ActionRequest>(json);
                        try
                        {
                            await ExecuteAction(action);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Could not execute request");
                        }
                    }
                };

            }
            catch (HttpRequestException e)
            {
                _logger.LogError(e, $"Error connecting to docker process...");
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error starting slave process...");
            }
        }

        private ImagesListResponse FindImage(string imageName, string imageTag)
        {
            if (_localImages != null)
            {
                foreach (var image in _localImages)
                {
                    if (image.RepoTags != null && image.RepoTags.Contains($"{imageName}:{imageTag}"))
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
                case SlaveAction.Stop:
                    await StopImage(action.ImageSource, action.ImageName, action.Tag);
                    break;
            }
        }

        private async Task StopImage(string imageSource, string imageName, string imageTag)
        {
            var imageFullName = $"{imageName}:{imageTag}";
            _logger.LogInformation($"Stop Image {imageFullName}");

            if(_runningImages.ContainsKey(imageFullName))
            {
                await _dockerClient.Containers.StopContainerAsync(_runningImages[imageFullName], new ContainerStopParameters());
                await _dockerClient.Images.DeleteImageAsync(imageFullName, new ImageDeleteParameters());
                _runningImages.Remove(imageFullName);
            }
        }

        private async Task StartImage(string imageSource, string imageName, string imageTag)
        {
            var imageFullName = $"{imageName}:{imageTag}";
            _logger.LogInformation($"Start Image {imageFullName}");
            var image = FindImage(imageName, $"{imageTag}");

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
                    Env = new[] { $"AUTOMATICA_SLAVE_MASTER=localhost", $"AUTOMATICA_SLAVE_USER={_slaveId}", $"AUTOMATICA_SLAVE_PASSWORD={_clientKey}" },
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


                if (_runningImages.ContainsKey(imageFullName))
                {
                    _runningImages.Remove(imageFullName);
                }
                _runningImages.Add(imageFullName, response.ID);
                await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters { });

            }
            catch (Exception e)
            {
                _logger.LogError(e, "error starting image...");
            }
         
        }


        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach(var id in _runningImages)
            {
                await _dockerClient.Containers.StopContainerAsync(id.Value, new ContainerStopParameters());
            }
        }
    }
}
