using Automatica.Core.Supervisor.Runtime.Data;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Core.Supervisor.Runtime
{
    public class Progress : IProgress<JSONMessage>
    {
        private readonly ILogger _logger;

        public Progress(ILogger logger)
        {
            this._logger = logger;
        }

        public void Report(JSONMessage value)
        {
            _logger.LogDebug($"{value.Status}:{value.ID}:{value.ProgressMessage}");
        }
    }

    public class RuntimeSupervisor : IHostedService
    {
        private readonly DockerClient _dockerClient;
        private readonly string _supervisorImage;
        private readonly string _supervisorImageTag;
        private readonly string _dockerRepositoryUrl;

        private readonly int _pollIntervalMinutes;
        private Timer _checkForNewImageTimer;

        private Timer _checkContainerStatusTimer;

        private readonly ILogger _logger;
        private string _runningContainer = null;
        private bool _isPullingImage = false;

        public RuntimeSupervisor(IConfiguration config, ILogger<RuntimeSupervisor> logger)
        {
            _logger = logger;

            var dockerSocket = config["DOCKER_SOCKET"];

            if (String.IsNullOrEmpty(dockerSocket))
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
            else
            {
                _logger.LogInformation($"Using provided docker socker {dockerSocket}");
                _dockerClient = new DockerClientConfiguration(new Uri(dockerSocket)).CreateClient();
            }

            _supervisorImage = config["image"];
            _supervisorImageTag = config["imageTag"];
            _dockerRepositoryUrl = config["dockerRegistry"];

            var pollInterval = config["pollIntervalMinutes"];

            if (String.IsNullOrEmpty(pollInterval))
            {
                _pollIntervalMinutes = 5;
            }
            else
            {
                _pollIntervalMinutes = Convert.ToInt32(pollInterval);
            }
        }

        public async Task Start()
        {
            try
            {
                var alreadyRunning = await CheckIfContainerAlreadyRunning();

                if (!alreadyRunning)
                {
                    await StartInternal();
                }

                _checkContainerStatusTimer = new Timer(new TimerCallback(async (a) =>
                {
                    await CheckContainerStatus();
                }), null, 0, (int)TimeSpan.FromSeconds(10).TotalMilliseconds);

                _checkForNewImageTimer = new Timer(new TimerCallback(async (a) =>
                {
                    try
                    {
                        if (await CheckForNewerImage())
                        {
                            await CreateAndStartContainer();
                        }
                    }
                    catch(Exception e)
                    {
                        _logger.LogError(e, "Could not check for container updates");
                    }

                }), null, 0, (int)TimeSpan.FromMinutes(_pollIntervalMinutes).TotalMilliseconds);

            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not start image");
            }
        }

        private async Task StartInternal()
        {
            await PullLatestImage();
            await CreateAndStartContainer();
        }

        private async Task CheckContainerStatus()
        {
            _logger.LogInformation($"Check container status");

            var containerInstance = await GetRunningContainerInstance();

            if(containerInstance == null)
            {
                _logger.LogError($"Container seems to be not running, starting it again");
                await StartInternal();
                return;
            }
            _logger.LogInformation($"Container state {containerInstance.State}");
        }

        private async Task CreateAndStartContainer()
        {
            _runningContainer = await CreateContainer();

            if (!string.IsNullOrEmpty(_runningContainer))
            {
                await StartContainer(_runningContainer);
            }
        }


        public async Task Stop()
        {
            _checkForNewImageTimer?.Dispose();
            _checkContainerStatusTimer?.Dispose();

            await StopContainer(_runningContainer);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Start();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Stop();
        }

        private async Task StartContainer(string containerId)
        {
            _logger.LogInformation($"Starting container  {containerId}");
            await _dockerClient.Containers.StartContainerAsync(containerId, new ContainerStartParameters { });
            
        }

        internal static HttpClient CreateClient()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");
            return httpClient;
        }

        private static async Task<T> GetImageInformation<T>(string url, string image, string tag, ILogger logger) where T : class
        {
            try
            {
                var client = CreateClient();

                var response = await client.GetAsync($"{url}/v2/repositories/{image}/tags/{tag}/");
                response.EnsureSuccessStatusCode();
                var payload = await response.Content.ReadAsStringAsync();


                return JsonConvert.DeserializeObject<T>(payload);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Could not get data");
            }
            return null;
        }

        private async Task<ContainerListResponse> GetRunningContainerInstance()
        {
            var fullImageName = $"{_supervisorImage}:{_supervisorImageTag}";
            var result = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters());

            var ret = result.SingleOrDefault(a => a.Image == fullImageName);

            return ret;
        }

        private async Task<bool> CheckIfContainerAlreadyRunning()
        {
            _logger.LogInformation($"Check if container already running");
            var ret = await GetRunningContainerInstance();

            if (ret != null)
            {
                _logger.LogInformation($"Container already running with id {ret.ID}");
                _runningContainer = ret.ID;
            }

            return ret != null;
        }

        private async Task StopContainer(string containerId)
        {
            if(string.IsNullOrEmpty(_runningContainer))
            {
                _logger.LogError("Cannot stop container because I have no clue which one");
                return;
            }
            _logger.LogInformation($"Stoping container {containerId}");
            await _dockerClient.Containers.StopContainerAsync(containerId, new ContainerStopParameters()
            {
                WaitBeforeKillSeconds = 60
            });
        }

        private async Task<string> CreateContainer()
        {
            if(_isPullingImage)
            {
                _logger.LogInformation($"Downloading newer image - do not start right now!");
                return null;
            }

            _logger.LogInformation($"Preparing container");


            try
            {

                var portBindings = new Dictionary<string, IList<PortBinding>>();
                portBindings.Add("5001/tcp", new List<PortBinding>()
                {
                    new PortBinding()
                    {
                        HostPort = "5001"
                    }
                });
                portBindings.Add("1883/tcp", new List<PortBinding>()
                {
                    new PortBinding()
                    {
                        HostPort = "1883"
                    }
                });

                var envVariables = new List<string>();

                foreach(DictionaryEntry env in Environment.GetEnvironmentVariables())
                {
                    var envVar = $"{env.Key}={env.Value}";
                    envVariables.Add(envVar);

                    _logger.LogDebug($"Using env variable: {envVar}");
                }

                var imgName = _supervisorImage.Split("/", StringSplitOptions.RemoveEmptyEntries)[1];

                if(imgName.Contains("."))
                {
                    var splitPoint = imgName.Split(".", StringSplitOptions.RemoveEmptyEntries);
                    imgName = splitPoint[splitPoint.Length - 1];
                }

                try
                {
                    await _dockerClient.Containers.RemoveContainerAsync($"{imgName}", new ContainerRemoveParameters() { Force = true });
                }
                catch(Exception e)
                {
                    _logger.LogWarning($"Could not delete old image - maybe no image has been created!");
                }

                var createContainerParams = new CreateContainerParameters()
                {
                    Name = imgName,
                    Hostname = imgName,
                    Image = $"{_supervisorImage}:{_supervisorImageTag}",
                    AttachStderr = false,
                    AttachStdin = false,
                    AttachStdout = false,
                    HostConfig = new HostConfig
                    {
                        PortBindings = portBindings
                    },
                    Env = envVariables
                };

                
                createContainerParams.HostConfig.Mounts = new List<Mount>();

               if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    createContainerParams.HostConfig.Mounts.Add(new Mount()
                    {
                        Source = $"/var/lib/{imgName}",
                        Target = $"/app/{imgName}/config",
                        Type = "bind"
                    });
                    createContainerParams.HostConfig.Mounts.Add(new Mount()
                    {
                        Source = $"/var/logs/{imgName}",
                        Target = $"/app/{imgName}/logs",
                        Type = "bind"
                    });
                }
                var networkMode = Environment.GetEnvironmentVariable($"NETWORK_MODE");

                if(!String.IsNullOrEmpty(networkMode))
                {
                    createContainerParams.HostConfig.NetworkMode = networkMode;
                }

                var response = await _dockerClient.Containers.CreateContainerAsync(createContainerParams);

                return response.ID;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not create image..");
            }
            return null;
        }

        private async Task<bool> CheckForNewerImage()
        {
            _logger.LogInformation($"Checking for new images.");

            var currentImage = await GetRunningContainerInstance();

            if(currentImage == null)
            {
                return true;
            }

            var image = await GetImageInformation<ImageInformation>(_dockerRepositoryUrl, _supervisorImage, _supervisorImageTag, _logger);

            //repository image is newer - pull it!
            if (currentImage != null && image.LastUpdated > currentImage.Created && image.Images.Any(a => IsArchitectureCompatible(a.Architecture)))
            {
                _logger.LogInformation($"New image found {image.Name} at {image.LastUpdated}\nCurrent local image {currentImage?.Labels.FirstOrDefault()} {currentImage?.Created}");
                await _dockerClient.Images.DeleteImageAsync($"{_supervisorImage}:{_supervisorImageTag}", new ImageDeleteParameters() { Force = true });
                return true;
            }
            return false;
        }

        private async Task PullLatestImage()
        {
            if (!_isPullingImage)
            {
                _isPullingImage = true;
                _logger.LogInformation($"Pull latest image.");


                _logger.LogInformation($"Pulling image");
                await _dockerClient.Images.CreateImageAsync(new ImagesCreateParameters()
                {
                    Tag = _supervisorImageTag,
                    FromImage = _supervisorImage
                }, null, new Progress(_logger));

                _isPullingImage = false;
            }
        }

        private bool IsArchitectureCompatible(string arch)
        {
            var curArch = NetStandardUtils.Platform.Arch;

            if (arch == "amd64" && curArch == "x64")
            {
                return true;
            }

            if (arch == "amd64" && curArch == "x86")
            {
                return true;
            }

            if (curArch == "arm" && arch == "arm")
            {
                return true;
            }

            if (curArch == "arm64" && arch == "arm64")
            {
                return true;
            }

            return false;
        }

    }
}
