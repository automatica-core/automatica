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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client.Options;
using Timer = System.Timers.Timer;
using System.Net;
using MQTTnet.Protocol;

namespace Automatica.Core.Slave.Runtime
{
    public class SlaveRuntime : IHostedService
    {
        private readonly IMqttClientOptions _options;
        private readonly IMqttClient _mqttClient;
        private readonly DockerClient _dockerClient;

        private readonly IDictionary<string, string> _runningImages = new Dictionary<string, string>();

        public string SlaveId => _slaveId;
        private readonly string _slaveId;

        private readonly string _masterAddress;
        private readonly string _clientKey;

        private readonly string _useDockerTag;
        private readonly string _logLevel;

        private readonly ILogger _logger;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        private readonly Timer _timer = new Timer(5000);
        private bool _connected;

        public SlaveRuntime(IServiceProvider services, ILogger<SlaveRuntime> logger)
        {
            var config = services.GetRequiredService<IConfiguration>();

            _logger = logger;

            _masterAddress = config["server:master"];
            _clientKey = config["server:clientKey"];

            _slaveId = config["server:clientId"];

            _useDockerTag = config["server:dockerTag"];
            _logLevel = config["Logging:LogLevel:Default"];
            


            _options = new MqttClientOptionsBuilder()
                   .WithClientId(_slaveId)
                   //   .WithWebSocketServer("localhost:5001/mqtt")
                   .WithTcpServer(_masterAddress, 1883)
                   .WithCredentials(_slaveId, _clientKey)
                   .WithCleanSession()
                   .Build();

            if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("MQTT_LOG_VERBOSE")))
            {
                //MqttNetGlobalLogger.LogMessagePublished += (s, e) =>
                //{
                //    var trace =
                //        $">> [{e.TraceMessage.Timestamp:O}] [{e.TraceMessage.ThreadId}] [{e.TraceMessage.Source}] [{e.TraceMessage.Level}]: {e.TraceMessage.Message}";
                //    if (e.TraceMessage.Exception != null)
                //    {
                //        trace += Environment.NewLine + e.TraceMessage.Exception.ToString();
                //    }

                //    _logger.LogTrace(trace);
                //};
            }

            _mqttClient = new MqttFactory().CreateMqttClient();
            _mqttClient.ApplicationMessageReceivedHandler = new ApplicationMessageReceivedHandler(this, _logger);

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
            catch (PlatformNotSupportedException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not connect do docker daemon!");
            }


            _timer.Elapsed += _timer_Elapsed;

        }

        private async void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!_connected)
            {
                await StopInternal();
                await StartInternal();
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await StartAsync();
        }

        public async Task StartAsync()
        {
            await StartInternal();
            _timer.Start();
        }


        private async Task StartInternal()
        {
            try
            {
                _logger.LogInformation($"Try fetch docker images...");
                await _dockerClient.Images.ListImagesAsync(new ImagesListParameters());
                _logger.LogInformation($"Try fetch docker images...done");


                _logger.LogInformation($"Try connect to mqtt broker...");
                await _mqttClient.ConnectAsync(_options); 
                _logger.LogInformation($"Try connect to mqtt broker...done");

                var topic = $"slave/{_slaveId}/action";
                var topics = $"slave/{_slaveId}/actions";
                var reinit = $"slave/{_slaveId}/reinit";

                _logger.LogInformation($"Try subscribe to mqtt topics...");
                await _mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).WithExactlyOnceQoS().Build());
                await _mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(topics).WithExactlyOnceQoS().Build());
                await _mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(reinit).WithExactlyOnceQoS().Build());

                _mqttClient.DisconnectedHandler = new MqttDisconnectedHandler(this);

                _logger.LogInformation($"Try subscribe to mqtt topics...done");
                _connected = true;
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

        internal async Task ExecuteAction(ActionRequest action)
        {
            switch (action.Action)
            {
                case SlaveAction.Start:
                    await StartImage(action);
                    break;
                case SlaveAction.Stop:
                    await StopImage(action);
                    break;
            }
        }

        private async Task StopImage(ActionRequest request)
        {
            var imageFullName = $"{request.ImageName}:{_useDockerTag ?? request.Tag}";
            _logger.LogInformation($"Stop Image {imageFullName}");

            if (_runningImages.ContainsKey(request.Id.ToString()))
            {
                await _dockerClient.Containers.StopContainerAsync(_runningImages[request.Id.ToString()], new ContainerStopParameters());
                try
                {
                    await _dockerClient.Images.DeleteImageAsync(imageFullName, new ImageDeleteParameters
                    {
                        Force = true
                    });
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Could not delete image");
                }

                _runningImages.Remove(request.Id.ToString());
            }
            else
            {
                _logger.LogError($"Could not stop image, image {imageFullName} not found");
            }
        }

        private async Task StartImage(ActionRequest request)
        {
            await _semaphore.WaitAsync();

            var imageFullName = $"{request.ImageName}:{_useDockerTag ?? request.Tag}";

            try
            {
                if (_runningImages.ContainsKey(request.Id.ToString()))
                {
                    _logger.LogWarning($"Image id {request.Id.ToString()} with image {imageFullName} already running, ignore now!");
                    return;
                }

                _logger.LogInformation($"Start Image {imageFullName}");

                var imageCreateParams = new ImagesCreateParameters()
                {
                    FromImage = request.ImageName,
                    Tag = _useDockerTag ?? request.Tag
                };

                if (!String.IsNullOrEmpty(request.ImageSource))
                {
                    imageCreateParams.FromSrc = request.ImageSource;
                }

                await _dockerClient.Images.CreateImageAsync(imageCreateParams, new AuthConfig(),
                    new ImageProgress(_logger));

                var createContainerParams = new CreateContainerParameters()
                {
                    Image = imageFullName,
                    AttachStderr = false,
                    AttachStdin = false,
                    AttachStdout = false,
                    HostConfig = new HostConfig
                    {
                        DNS = new[] { "8.8.8.8", "8.8.4.4" },
                        NetworkMode = "host",
                        Mounts = new List<Mount>()
                    },
                    Env = new[]
                    {
                        $"AUTOMATICA_SLAVE_MASTER={_masterAddress}", $"AUTOMATICA_SLAVE_USER={_slaveId}",
                        $"AUTOMATICA_SLAVE_PASSWORD={_clientKey}", $"AUTOMATICA_NODE_ID={request.Id.ToString()}", 
                        $"MQTT_LOG_VERBOSE=asdf",
                        $"LOG_LEVEL={_logLevel}"
                    },
                };

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    createContainerParams.HostConfig.Privileged = true;

                    createContainerParams.HostConfig.Mounts.Add(new Mount
                    {
                        Source = "/dev",
                        Target = "/dev",
                        Type = "bind"
                    });

                    createContainerParams.HostConfig.Mounts.Add(new Mount
                    {
                        Source = "/tmp",
                        Target = "/tmp",
                        Type = "bind"
                    });
                }

                var response = await _dockerClient.Containers.CreateContainerAsync(createContainerParams);

                _runningImages.Add(request.Id.ToString(), response.ID);
                await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());

            }
            catch (Exception e)
            {
                _logger.LogError(e, "error starting image...");
            }
            finally
            {
                _semaphore.Release(1);
            }

        }

        internal async Task ReInit()
        {
            await StopAllContainers();
        }
        internal async Task Restart()
        {
            await StopInternal();
            await StartAsync();
        }

        private async Task StopAllContainers()
        {
            foreach (var id in _runningImages)
            {
                _logger.LogInformation($"Stopping container instance {id.Value}");
                try
                {
                    await _dockerClient.Containers.StopContainerAsync(id.Value, new ContainerStopParameters());

                    await _dockerClient.Containers.RemoveContainerAsync(id.Value, new ContainerRemoveParameters()
                    {
                        Force = true
                    });
                }
                catch(Exception e)
                {
                    _logger.LogError(e, $"Could not stop/delete container {e}");
                }
            }
            _runningImages.Clear();
        }

        private async Task StopInternal()
        {
            try
            {
                await StopAllContainers();

                await _mqttClient.DisconnectAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error stopping connection...");
            }
        }


        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await StopInternal();


            _timer.Elapsed -= _timer_Elapsed;
        }

    }
}
