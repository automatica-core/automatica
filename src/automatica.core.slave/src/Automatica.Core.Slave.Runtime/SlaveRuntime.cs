using Automatica.Core.Slave.Abstraction;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Client;
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

        public SlaveRuntime(IServiceProvider services)
        {
            var config = services.GetRequiredService<IConfiguration>();

            _slaveId = config["server:clientId"];
            _options = new MqttClientOptionsBuilder()
                   .WithClientId(_slaveId)
                   .WithTcpServer(config["server:master"])
                   .WithCredentials(_slaveId, config["server:clientKey"])
                   .WithCleanSession()
                   .Build();

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
            await _mqttClient.ConnectAsync(_options);

            var topic = $"slave/{_slaveId}/action/#";
            await _mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).WithExactlyOnceQoS().Build());


            _mqttClient.ApplicationMessageReceived += (sender, e) =>
            {
                if(MqttTopicFilterComparer.IsMatch(topic, e.ApplicationMessage.Topic))
                {
                    var json = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    var action = JsonConvert.DeserializeObject<ActionRequest>(json);

                }
            };

            _localImages = await _dockerClient.Images.ListImagesAsync(new ImagesListParameters());

        }

        private ImagesListResponse FindImage(string imageName, string imageTag)
        {
            foreach(var image in _localImages)
            {
                if(image.RepoTags.Contains($"{imageName}:{imageTag}"))
                {
                    return image;
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

        private async Task StartImage(string imageSource, string imageName, string imageTag)
        {

            var image = FindImage(imageName, imageTag);

            if(image == null)
            {
                var imageCreateParams = new ImagesCreateParameters()
                {
                    FromImage = imageName,
                    Tag = imageTag
                };

                if(!String.IsNullOrEmpty(imageSource))
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

            await _dockerClient.Containers.StartWithConfigContainerExecAsync(image.ID, new ContainerExecStartParameters()
            {
                
            });
         
        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
