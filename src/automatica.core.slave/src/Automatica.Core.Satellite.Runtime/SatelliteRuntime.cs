using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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
using Timer = System.Timers.Timer;

namespace Automatica.Core.Satellite.Runtime
{
    public class SatelliteRuntime : IHostedService
    {
        private MqttClientOptions _options;
        private IMqttClient _mqttClient;
        private readonly DockerClient _dockerClient;

        private readonly IDictionary<string, RunningContainerInfo> _runningImages = new Dictionary<string, RunningContainerInfo>();

        public string SlaveId => _slaveId;
        private string _slaveId;

        private string _masterAddress;
        private string _clientKey;

        private string _useDockerTag;
        private string _logLevel;

        private bool _configured;
        private bool _useTls;
        private int _tlsPort;
        private string _tlsFingerprint;
        private IMqttClient _stoppedClient;

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
                                    { "running", true }
                                }
                            },
                            {
                                "label", new Dictionary<string, bool>
                                {
                                    { $"{ContainerPolicy.ManagedLabel}=true", true }
                                }
                            }
                        }
                    });

                    if (_runningImages.Values.Any(running => containers.All(a => a.ID != running.DockerId)))
                    {
                        _logger.LogInformation($"Restart containers...");
                        await StopInternal("restart");
                        await StartInternal();
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

        private ContainerPolicy CreatePolicy() => new ContainerPolicy(_config);

        private void Init()
        {
            _masterAddress = _config["server:master"];
            _masterPort = _config["server:masterPort"];

            _clientKey = _config["server:clientKey"];

            _slaveId = _config["server:clientId"];

            _useDockerTag = _config["server:dockerTag"];
            _logLevel = _config["Logging:LogLevel:Default"];

            _useTls = String.Equals(_config["server:useTls"], "true", StringComparison.OrdinalIgnoreCase);
            _tlsPort = Int32.TryParse(_config["server:tlsPort"], out var tlsPort) && tlsPort > 0 ? tlsPort : 8883;
            _tlsFingerprint = NormalizeFingerprint(_config["server:tlsFingerprint"]);

            _configured = !String.IsNullOrEmpty(_masterAddress) && !String.IsNullOrEmpty(_slaveId) && !String.IsNullOrEmpty(_clientKey);

            var builder = new MqttClientOptionsBuilder()
                .WithClientId(_slaveId)
                .WithCredentials(_slaveId, _clientKey)
                .WithCleanSession();

            if (_useTls)
            {
                builder = builder
                    .WithTcpServer(_masterAddress, _tlsPort)
                    .WithTlsOptions(o => o
                        .UseTls()
                        .WithTargetHost(_masterAddress)
                        .WithCertificateValidationHandler(ValidateBrokerCertificate));
            }
            else
            {
                builder = builder.WithTcpServer(_masterAddress, String.IsNullOrEmpty(_masterPort) ? 1883 : Convert.ToInt32(_masterPort));
            }

            _options = builder.Build();
            _mqttClient = new MqttClientFactory().CreateMqttClient();
            _mqttClient.ApplicationMessageReceivedAsync += new ApplicationMessageReceivedHandler(this, _logger).HandleApplicationMessageReceivedAsync;
        }

        public static string NormalizeFingerprint(string fingerprint)
        {
            if (String.IsNullOrWhiteSpace(fingerprint))
            {
                return null;
            }
            var hex = new string(fingerprint.Where(Uri.IsHexDigit).ToArray()).ToLowerInvariant();
            return hex.Length == 64 ? hex : null;
        }

        /// <summary>
        /// The broker certificate is pinned by its SHA-256 fingerprint (the core's certificate is self-signed). Without a
        /// configured fingerprint the first certificate seen is pinned (trust on first use) and stored.
        /// </summary>
        private bool ValidateBrokerCertificate(MqttClientCertificateValidationEventArgs args)
        {
            if (args.Certificate == null)
            {
                return false;
            }

            var fingerprints = new List<string> { Convert.ToHexString(SHA256.HashData(args.Certificate.GetRawCertData())).ToLowerInvariant() };
            if (args.Chain != null)
            {
                fingerprints.AddRange(args.Chain.ChainElements.Select(e => Convert.ToHexString(SHA256.HashData(e.Certificate.RawData)).ToLowerInvariant()));
            }

            if (_tlsFingerprint == null)
            {
                _tlsFingerprint = fingerprints[0];
                _config["server:tlsFingerprint"] = _tlsFingerprint;
                _logger.LogWarning($"No broker certificate fingerprint configured, pinned the certificate of {_masterAddress}: {_tlsFingerprint}");
                return true;
            }

            if (fingerprints.Any(f => UserAuthData.FixedTimeEquals(f, _tlsFingerprint)))
            {
                return true;
            }

            _logger.LogError($"Broker certificate of {_masterAddress} ({fingerprints[0]}) does not match the pinned fingerprint {_tlsFingerprint}, not connecting");
            return false;
        }

        private async Task StartInternal()
        {
            try
            {
                Init();

                if (!_configured)
                {
                    _logger.LogWarning("Satellite is not configured (server, client id and key), not connecting");
                    return;
                }

                _logger.LogInformation($"Try fetch docker images...");
                await _dockerClient.Images.ListImagesAsync(new ImagesListParameters());
                _logger.LogInformation($"Try fetch docker images...done");


                _logger.LogInformation($"Try connect to mqtt broker {_masterAddress} (tls: {_useTls})...");
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

                // Init() creates a fresh client on every (re)start, so this is attached once per client;
                // disconnects we cause ourselves (StopInternal) don't trigger a restart
                var client = _mqttClient;
                client.DisconnectedAsync += e => client == _stoppedClient || client != _mqttClient || !e.ClientWasConnected ? Task.CompletedTask : Restart();

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
                    if (UpgradeToTls(action))
                    {
                        return;
                    }
                    if (!await StartImage(action))
                    {
                        // not inline: this runs in the message handler of the client that is restarted
                        _ = Task.Run(Restart);
                    }
                    break;
                case SlaveAction.Stop:
                    await StopImage(action);
                    break;
            }
        }

        /// <summary>
        /// The core offers a TLS listener: switch to it (unless TLS was configured explicitly) and pin the fingerprint
        /// it sent. Returns true if the connection is restarted.
        /// </summary>
        private bool UpgradeToTls(ActionRequest action)
        {
            if (_useTls || !String.IsNullOrEmpty(_config["server:useTls"]) || action.BrokerTlsPort <= 0)
            {
                return false;
            }

            var fingerprint = NormalizeFingerprint(action.BrokerFingerprint);
            if (fingerprint == null)
            {
                return false;
            }

            _logger.LogInformation($"Core offers a TLS broker on port {action.BrokerTlsPort}, switching to TLS (fingerprint {fingerprint})");
            _config["server:tlsPort"] = action.BrokerTlsPort.ToString();
            _config["server:tlsFingerprint"] = fingerprint;
            _config["server:useTls"] = "true";

            _ = Task.Run(Restart);
            return true;
        }

        private async Task<ContainerInspectResponse> GetContainer(string name)
        {
            try
            {
                return await _dockerClient.Containers.InspectContainerAsync(name);
            }
            catch (DockerContainerNotFoundException)
            {
                return null;
            }
        }

        private static bool IsManaged(ContainerInspectResponse container)
        {
            return container?.Config?.Labels != null &&
                   container.Config.Labels.TryGetValue(ContainerPolicy.ManagedLabel, out var managed) && managed == "true";
        }

        private async Task StopImage(ActionRequest request)
        {
            var policy = CreatePolicy();
            _logger.LogInformation($"Stop container {request.Id}");

            var container = await GetContainer(request.Id.ToString());
            if (container != null && !IsManaged(container))
            {
                _logger.LogError($"Container {request.Id} was not created by the satellite, not stopping it");
                return;
            }

            if (container != null)
            {
                try
                {
                    await _dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters { WaitBeforeKillSeconds = 120 });
                    await _dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters { Force = true });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Could not stop container");
                }
            }

            // the image is removed so the next start pulls the current one; only allowed images, and not while in use
            if (policy.TryGetImage(request, _useDockerTag, out _, out _, out var imageReference, out _))
            {
                try
                {
                    await _dockerClient.Images.DeleteImageAsync(imageReference, new ImageDeleteParameters { Force = false });
                }
                catch (Exception e)
                {
                    _logger.LogWarning($"Could not delete image {imageReference}: {e.Message}");
                }
            }

            _runningImages.Remove(request.Id.ToString());
        }

        /// <summary>
        /// Returns false if the satellite should reconnect (docker errors), true if the request was handled (also when it was rejected)
        /// </summary>
        private async Task<bool> StartImage(ActionRequest request)
        {
            var policy = CreatePolicy();

            if (!policy.TryGetImage(request, _useDockerTag, out var repository, out var tag, out var imageReference, out var error))
            {
                _logger.LogError($"Rejected start request for {request.Id}: {error}");
                return true;
            }

            await _semaphore.WaitAsync();

            try
            {
                if (_runningImages.ContainsKey(request.Id.ToString()))
                {
                    _logger.LogWarning($"Image id {request.Id} with image {imageReference} already running, ignore now!");
                    return true;
                }

                _logger.LogInformation($"Start Image {imageReference}");

                var existing = await GetContainer(request.Id.ToString());
                if (existing != null)
                {
                    if (!IsManaged(existing))
                    {
                        _logger.LogError($"A container named {request.Id} exists that was not created by the satellite, not replacing it");
                        return true;
                    }

                    _logger.LogInformation($"Remove old container with name {request.Id}");
                    await _dockerClient.Containers.RemoveContainerAsync(existing.ID, new ContainerRemoveParameters { Force = true });
                }

                await _dockerClient.Images.CreateImageAsync(new ImagesCreateParameters
                {
                    FromImage = repository,
                    // the docker api takes a tag or a digest here
                    Tag = String.IsNullOrEmpty(request.ImageDigest) ? tag : request.ImageDigest
                }, new AuthConfig(), new ImageProgress(_logger));

                if (!String.IsNullOrEmpty(request.ImageDigest) && !await HasDigest(imageReference, repository, request.ImageDigest))
                {
                    _logger.LogError($"Image {imageReference} does not have the requested digest, not starting it");
                    return true;
                }

                var password = request.ClientKey;
                if (String.IsNullOrEmpty(password))
                {
                    // cores before the broker hardening don't send per-plugin credentials
                    _logger.LogWarning($"No plugin credentials for {request.Id}, passing the satellite key (update the core)");
                    password = _clientKey;
                }

                var env = new List<string>
                {
                    $"AUTOMATICA_SLAVE_MASTER={_masterAddress}", $"AUTOMATICA_SLAVE_USER={_slaveId}",
                    $"AUTOMATICA_SLAVE_PASSWORD={password}", $"AUTOMATICA_NODE_ID={request.Id}",
                    $"LOG_LEVEL={_logLevel}"
                };
                if (_useTls && _tlsFingerprint != null)
                {
                    env.Add($"AUTOMATICA_SLAVE_TLS_PORT={_tlsPort}");
                    env.Add($"AUTOMATICA_SLAVE_TLS_FINGERPRINT={_tlsFingerprint}");
                }

                var createContainerParams = new CreateContainerParameters()
                {
                    Image = imageReference,
                    AttachStderr = false,
                    Name = $"{request.Id}",
                    AttachStdin = false,
                    AttachStdout = false,
                    Labels = new Dictionary<string, string>
                    {
                        { ContainerPolicy.ManagedLabel, "true" },
                        { ContainerPolicy.SatelliteLabel, _slaveId },
                        { ContainerPolicy.NodeLabel, request.Id.ToString() }
                    },
                    HostConfig = new HostConfig
                    {
                        // host network: the drivers talk to devices on the LAN (multicast, broadcast, unknown ports)
                        NetworkMode = "host",
                        Mounts = new List<Mount>(),
                        SecurityOpt = new List<string> { "no-new-privileges" }
                    },
                    Env = env
                };

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    if (policy.Privileged)
                    {
                        _logger.LogWarning($"docker:privileged is set, starting {request.Id} privileged");
                        createContainerParams.HostConfig.Privileged = true;
                    }

                    var devices = policy.GetDevices(request, d => _logger.LogWarning($"Device {d} requested by {request.Id} is not allowed"));
                    createContainerParams.HostConfig.Devices = devices.Select(d => new DeviceMapping
                    {
                        PathOnHost = d,
                        PathInContainer = d,
                        CgroupPermissions = "rwm"
                    }).ToList();

                    var caps = policy.GetCapabilities(request, c => _logger.LogWarning($"Capability {c} requested by {request.Id} is not allowed"));
                    if (caps.Count > 0)
                    {
                        createContainerParams.HostConfig.CapAdd = caps;
                    }

                    _logger.LogInformation($"Container {request.Id}: devices [{String.Join(", ", devices)}], capabilities [{String.Join(", ", caps)}]");
                }

                var response = await _dockerClient.Containers.CreateContainerAsync(createContainerParams);

                _runningImages.Add(request.Id.ToString(), new()
                {
                    CreateContainerParameters = createContainerParams,
                    DockerId = response.ID,
                    Image = createContainerParams.Image,
                    RequestId = request.Id.ToString(),
                    Request = request
                });
                await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());

                _containerStarted = true;
                return true;
            }
            catch (DockerApiException ex)
            {
                _logger.LogError(ex, $"Could not start container {request.Id}");
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

        private async Task<bool> HasDigest(string imageReference, string repository, string digest)
        {
            try
            {
                var image = await _dockerClient.Images.InspectImageAsync(imageReference);
                return image.RepoDigests != null && image.RepoDigests.Any(d =>
                    d.EndsWith("@" + digest, StringComparison.Ordinal) &&
                    (d.StartsWith(repository + "@", StringComparison.Ordinal) || d.StartsWith("docker.io/" + repository + "@", StringComparison.Ordinal)));
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not inspect image {imageReference}");
                return false;
            }
        }

        internal async Task ReInit()
        {
            await StopAllContainers("re-init");
        }

        /// <summary>
        /// A plugin lost its broker connection (its will message): restart its container
        /// </summary>
        internal async Task RestartPlugin(string nodeId)
        {
            if (!_runningImages.TryGetValue(nodeId, out var info) || info.Request == null)
            {
                return;
            }

            _logger.LogWarning($"Plugin {nodeId} disconnected unexpectedly, restarting its container");
            try
            {
                await _dockerClient.Containers.RemoveContainerAsync(info.DockerId, new ContainerRemoveParameters { Force = true });
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not remove container {info.DockerId}");
            }
            _runningImages.Remove(nodeId);

            await StartImage(info.Request);
        }

        public async Task Restart()
        {
            await _startupSemaphore.WaitAsync();
            try
            {
                await StopInternal("restart");
                await StartInternal();
                _timer.Start();
            }
            finally
            {
                _startupSemaphore.Release();
            }
        }

        private async Task StopAllContainers(string reason)
        {
            foreach (var id in _runningImages)
            {
                _logger.LogInformation($"Stopping container instance {id.Key}: Reason {reason}");
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
            _stoppedClient = _mqttClient;
            try
            {
                await StopAllContainers(reason);

                if (_mqttClient != null)
                {
                    await _mqttClient.DisconnectAsync();
                }
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
            _timer.Stop();
            await StopInternal("stop service");


            _timer.Elapsed -= _timer_Elapsed;
        }

    }
}
