using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Satellite.Abstraction;
using Automatica.Core.Satellite.Abstraction.Model;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MqttTopicFilterBuilder = MQTTnet.MqttTopicFilterBuilder;
using Timer = System.Timers.Timer;

namespace Automatica.Core.Satellite.Runtime
{
    public class SatelliteRuntime : IHostedService
    {
        private IMqttClientOptions _options;
        private IMqttClient _mqttClient;
        private readonly DockerClient _dockerClient;

        private readonly IDictionary<string, RunningContainerInfo> _runningImages = new Dictionary<string, RunningContainerInfo>();

        public string SlaveId => _slaveId;
        private string _slaveId;

        private string _masterAddress;
        private string _clientKey;

        private string _useDockerTag;
        private string _logLevel;

        private readonly ILogger _logger;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly SemaphoreSlim _startupSemaphore = new SemaphoreSlim(1);

        private readonly Timer _timer = new Timer(60000);
        private bool _connected;
        private bool _containerStarted;
        private readonly IConfiguration _config;
        private string _masterPort;

        public bool Connected => _connected;
        public bool ContainerStarted => _containerStarted;
        public IDictionary<string, RunningContainerInfo> RunningImages => _runningImages;

        public SatelliteRuntime(IServiceProvider services, ILogger<SatelliteRuntime> logger)
        {
            var config = services.GetRequiredService<IConfiguration>();
            _config = config;

            _logger = logger;
            
           

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
            await _startupSemaphore.WaitAsync();

            try
            {
                if (!_connected)
                {
                    await StopInternal("not connected");
                    await StartInternal();
                }
                else if (_containerStarted)
                {
                    _logger.LogInformation($"Check running containers...");
                    var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters()
                    {
                        Filters = new Dictionary<string, IDictionary<string, bool>>
                        {
                            {
                                "status", new Dictionary<string, bool>
                                {
                                    { "running", false }
                                }
                            }
                        }
                    });

                    foreach (var running in _runningImages)
                    {
                        if (containers.All(a => a.ID != running.Value.DockerId))
                        {
                            _logger.LogInformation($"Restart containers...");
                            await StopInternal("restart");
                            await StartInternal();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting salve...");
            }
            finally
            {
                _startupSemaphore.Release(1);
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

        private void Init()
        {
            _masterAddress = _config["server:master"];
            _masterPort = _config["server:masterPort"];

            _clientKey = _config["server:clientKey"];

            _slaveId = _config["server:clientId"];

            _useDockerTag = _config["server:dockerTag"];
            _logLevel = _config["Logging:LogLevel:Default"];

            

            _options = new MqttClientOptionsBuilder()
                .WithClientId(_slaveId)
                //   .WithWebSocketServer("localhost:5001/mqtt")
                .WithTcpServer(_masterAddress, String.IsNullOrEmpty(_masterPort) ? 1883 : Convert.ToInt32(_masterPort))
                .WithCredentials(_slaveId, _clientKey)
                .WithCleanSession()
                .Build();
            _mqttClient = new MqttFactory().CreateMqttClient();
            _mqttClient.ApplicationMessageReceivedHandler = new ApplicationMessageReceivedHandler(this, _logger);
        }

        private async Task StartInternal()
        {
            try
            {
                Init();

                _logger.LogInformation($"Try fetch docker images...");
                await _dockerClient.Images.ListImagesAsync(new ImagesListParameters());
                _logger.LogInformation($"Try fetch docker images...done");
                

                _logger.LogInformation($"Try connect to mqtt broker...");
                await _mqttClient.ConnectAsync(_options); 
                _logger.LogInformation($"Try connect to mqtt broker...done");

                var topic = $"slave/{_slaveId}/action";
                var topics = $"slave/{_slaveId}/actions";
                var reinit = $"slave/{_slaveId}/reinit";
                var watchdog = $"watchdog/{_slaveId}/#";

                _logger.LogInformation($"Try subscribe to mqtt topics...");
                await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).WithExactlyOnceQoS().Build());
                await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topics).WithExactlyOnceQoS().Build());
                await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(reinit).WithExactlyOnceQoS().Build());
                await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(watchdog).WithExactlyOnceQoS().Build());

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
                    if (!await StartImage(action))
                    {
                        await StopInternal("start");
                        await StartInternal();
                    }
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

            try
            {
                await _dockerClient.Containers.StopContainerAsync(request.Id.ToString(),
                    new ContainerStopParameters { WaitBeforeKillSeconds = 120 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Could not stop container");
            }

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

        private async Task<bool> StartImage(ActionRequest request)
        {
            await _semaphore.WaitAsync();

            var imageFullName = $"{request.ImageName}:{_useDockerTag ?? request.Tag}";

            try
            {
                if (_runningImages.ContainsKey(request.Id.ToString()))
                {
                    _logger.LogWarning($"Image id {request.Id.ToString()} with image {imageFullName} already running, ignore now!");
                    return true;
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

                try
                {
                    _logger.LogInformation($"Remove old container with name {request.Id}");
                    await _dockerClient.Containers.RemoveContainerAsync(request.Id.ToString(),
                        new ContainerRemoveParameters
                        {
                            Force = true
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Could not delete container...{ex}");
                }

                await _dockerClient.Images.CreateImageAsync(imageCreateParams, new AuthConfig(),
                    new ImageProgress(_logger));

                var createContainerParams = new CreateContainerParameters()
                {
                    Image = imageFullName,
                    AttachStderr = false,
                    Name = $"{request.Id}",
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
                        $"AUTOMATICA_SLAVE_PASSWORD={_clientKey}", $"AUTOMATICA_NODE_ID={request.Id}", 
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

                _runningImages.Add(request.Id.ToString(), new()
                {
                    CreateContainerParameters = createContainerParams,
                    DockerId = response.ID,
                    Image = createContainerParams.Image,
                    RequestId = request.Id.ToString()
                });
                await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());

                _containerStarted = true;
                return true;
            }
            catch (DockerApiException ex)
            {
                if (ex.Message.Contains("already in use by container"))
                {
                    await _dockerClient.Containers.RemoveContainerAsync(request.Id.ToString(),
                        new ContainerRemoveParameters
                        {
                            Force = true
                        });
                }
                else
                {
                    _logger.LogError(ex, $"Could not delete container...{ex}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "error starting image...");
            }
            finally
            {
                _semaphore.Release(1);
            }

            return false;
        }

        internal async Task ReInit()
        {
            await StopAllContainers("re-init");
        }

        public async Task Restart()
        {
            await StopInternal("restart");
            await StartAsync();
        }

        private async Task StopAllContainers(string reason)
        {
            foreach (var id in _runningImages)
            {
                _logger.LogInformation($"Stopping container instance {id.Value}: Reason {reason}");
                try
                {
                    await _dockerClient.Containers.StopContainerAsync(id.Value.DockerId, new ContainerStopParameters());

                    await _dockerClient.Containers.RemoveContainerAsync(id.Value.DockerId, new ContainerRemoveParameters
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

        private async Task StopInternal(string reason)
        {
            try
            {
                await StopAllContainers(reason);

                await _mqttClient.DisconnectAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error stopping connection...{e}");
            }
            finally
            {
                _connected = false;
            }
        }


        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await StopInternal("stop service");


            _timer.Elapsed -= _timer_Elapsed;
        }

    }
}
