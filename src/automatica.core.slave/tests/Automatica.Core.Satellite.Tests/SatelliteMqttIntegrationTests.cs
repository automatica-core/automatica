using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Automatica.Core.Satellite.Abstraction;
using Automatica.Core.Satellite.Tests.Infrastructure;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using Xunit;

namespace Automatica.Core.Satellite.Tests
{
    /// <summary>
    /// The satellite runtime against a real in-process MQTTnet broker; the test plays the core
    /// </summary>
    public sealed class SatelliteMqttIntegrationTests : IAsyncLifetime
    {
        private const string Id = RuntimeHarness.SatelliteId;
        private RuntimeHarness _h;
        private IMqttClient _core;

        public async Task InitializeAsync()
        {
            _h = await RuntimeHarness.StartAsync();
            _core = await _h.Broker.ConnectClientAsync();
        }

        public async Task DisposeAsync()
        {
            await _core.DisconnectAsync();
            _core.Dispose();
            await _h.DisposeAsync();
        }

        private Task Publish(string topic, string payload) =>
            _core.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(Encoding.UTF8.GetBytes(payload))
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build(), CancellationTokens.Timeout());

        private static ActionRequest Start(string image = "automaticacore/automatica-plugin-zwave", Guid? id = null) => new()
        {
            Action = SlaveAction.Start,
            ImageName = image,
            Tag = "1.0",
            Id = id ?? Guid.NewGuid(),
            ClientKey = TestKeys.PluginKey
        };

        [Fact]
        public void Connect_UsesTheSatelliteIdAsClientIdAndUserAndTheKeyAsPassword()
        {
            // the other connect is the test client playing the core
            var connect = Assert.Single(_h.Broker.Connects, c => c.ClientId == Id);

            Assert.Equal(Id, connect.UserName);
            Assert.Equal(TestKeys.SatelliteKey, connect.Password);
        }

        [Fact]
        public void Subscribe_OnlyTheOwnSatelliteTopics()
        {
            var topics = _h.Broker.Subscriptions.Where(s => s.ClientId == Id).Select(s => s.Topic).OrderBy(t => t).ToArray();

            Assert.Equal(new[]
            {
                $"slave/{Id}/action",
                $"slave/{Id}/actions",
                $"slave/{Id}/reinit",
                $"watchdog/{Id}/#"
            }.OrderBy(t => t), topics);
        }

        [Fact]
        public async Task ActionTopic_StartRequest_StartsTheContainer()
        {
            var request = Start();

            await Publish($"slave/{Id}/action", JsonConvert.SerializeObject(request));

            await Eventually.Assert(() => _h.Docker.Started.Count == 1, "container was not started");
            var created = Assert.Single(_h.Docker.Created);
            Assert.Equal("automaticacore/automatica-plugin-zwave:1.0", created.Image);
            Assert.False(created.HostConfig.Privileged);
            Assert.True(_h.Runtime.RunningImages.ContainsKey(request.Id.ToString()));
        }

        [Fact]
        public async Task ActionsTopic_ArrayOfRequests_StartsAll()
        {
            await Publish($"slave/{Id}/actions", JsonConvert.SerializeObject(new[] { Start(), Start("automaticacore/automatica-plugin-knx") }));

            await Eventually.Assert(() => _h.Docker.Started.Count == 2, "both containers should be started");
            Assert.Equal(new[] { "automaticacore/automatica-plugin-knx:1.0", "automaticacore/automatica-plugin-zwave:1.0" },
                _h.Docker.Created.Select(c => c.Image).OrderBy(a => a));
        }

        [Fact]
        public async Task ActionTopic_DisallowedImage_IsRejectedAndTheSatelliteKeepsWorking()
        {
            await Publish($"slave/{Id}/action", JsonConvert.SerializeObject(Start("attacker/cryptominer")));
            await Publish($"slave/{Id}/action", JsonConvert.SerializeObject(Start()));

            await Eventually.Assert(() => _h.Docker.Started.Count == 1, "the allowed image should still be started");
            Assert.Equal("automaticacore/automatica-plugin-zwave:1.0", Assert.Single(_h.Docker.Created).Image);
            Assert.True(_h.Logs.Contains(LogLevel.Error, "Rejected start request"));
            Assert.True(_h.Runtime.Connected);
        }

        [Fact]
        public async Task ActionTopic_PayloadAskingForPrivilegedMode_StartsUnprivileged()
        {
            var json = $@"{{""Action"":0,""ImageName"":""automaticacore/x"",""Tag"":""1.0"",""Id"":""{Guid.NewGuid()}"",
                ""Privileged"":true,""HostConfig"":{{""Privileged"":true,""Binds"":[""/var/run/docker.sock:/var/run/docker.sock""]}},
                ""Devices"":[""/dev/mem""],""CapAdd"":[""SYS_ADMIN""]}}";

            await Publish($"slave/{Id}/action", json);

            await Eventually.Assert(() => _h.Docker.Started.Count == 1, "container was not started");
            var host = Assert.Single(_h.Docker.Created).HostConfig;
            Assert.False(host.Privileged);
            Assert.Empty(host.Mounts);
            Assert.True(host.Binds == null || host.Binds.Count == 0);
            Assert.True(host.Devices == null || host.Devices.All(d => d.PathOnHost != "/dev/mem"));
            Assert.True(host.CapAdd == null || !host.CapAdd.Contains("SYS_ADMIN"));
        }

        [Theory]
        [InlineData("action", "{ not json")]
        [InlineData("action", "")]
        [InlineData("action", "null")]
        [InlineData("action", "[1,2,3]")]
        [InlineData("actions", "{\"Action\":0}")]
        [InlineData("actions", "null")]
        [InlineData("actions", "")]
        public async Task BadPayload_IsIgnoredAndTheNextRequestIsHandled(string suffix, string payload)
        {
            await Publish($"slave/{Id}/{suffix}", payload);
            await Publish($"slave/{Id}/action", JsonConvert.SerializeObject(Start()));

            await Eventually.Assert(() => _h.Docker.Started.Count == 1, "the valid request after the bad one should be handled");
            Assert.Single(_h.Docker.Created);
            Assert.True(_h.Runtime.Connected);
        }

        [Fact]
        public async Task OtherSatellitesTopics_AreNotReceived()
        {
            await Publish("slave/other-satellite/action", JsonConvert.SerializeObject(Start("automaticacore/other")));
            await Publish("slave/other-satellite/reinit", "{}");
            // sentinel: the own topic, published after the foreign ones
            await Publish($"slave/{Id}/action", JsonConvert.SerializeObject(Start()));

            await Eventually.Assert(() => _h.Docker.Started.Count == 1, "sentinel request was not handled");
            Assert.Equal("automaticacore/automatica-plugin-zwave:1.0", Assert.Single(_h.Docker.Created).Image);
        }

        [Fact]
        public async Task StopRequest_RemovesTheContainer()
        {
            var request = Start();
            await Publish($"slave/{Id}/action", JsonConvert.SerializeObject(request));
            await Eventually.Assert(() => _h.Docker.Started.Count == 1, "container was not started");

            request.Action = SlaveAction.Stop;
            await Publish($"slave/{Id}/action", JsonConvert.SerializeObject(request));

            await Eventually.Assert(() => _h.Docker.Removed.Count == 1, "container was not removed");
            Assert.Equal(new[] { "docker-1" }, _h.Docker.Stopped);
            await Eventually.Assert(() => _h.Runtime.RunningImages.Count == 0, "container still listed as running");
        }

        [Fact]
        public async Task ReinitTopic_StopsAllContainers()
        {
            await Publish($"slave/{Id}/actions", JsonConvert.SerializeObject(new[] { Start(), Start() }));
            await Eventually.Assert(() => _h.Docker.Started.Count == 2, "containers were not started");

            await Publish($"slave/{Id}/reinit", "");

            await Eventually.Assert(() => _h.Docker.Removed.Count == 2, "containers were not removed on re-init");
            Assert.Empty(_h.Runtime.RunningImages);
        }

        [Fact]
        public async Task WatchdogTopic_PluginWillMessage_RestartsThatPlugin()
        {
            var request = Start();
            await Publish($"slave/{Id}/action", JsonConvert.SerializeObject(request));
            await Eventually.Assert(() => _h.Docker.Started.Count == 1, "container was not started");

            await Publish($"watchdog/{Id}/{request.Id}", "");

            await Eventually.Assert(() => _h.Docker.Started.Count == 2, "plugin container was not restarted");
            Assert.Equal(new[] { "docker-1" }, _h.Docker.Removed);
            Assert.Equal("docker-2", _h.Runtime.RunningImages[request.Id.ToString()].DockerId);
        }

        [Fact]
        public async Task BrokerDropsTheConnection_SatelliteReconnectsAndResubscribes()
        {
            await _h.Broker.DisconnectClientAsync(Id);

            await Eventually.Assert(() => _h.Broker.Connects.Count(c => c.ClientId == Id) == 2, "satellite did not reconnect");
            await Eventually.Assert(() => _h.Broker.Subscriptions.Count(s => s.ClientId == Id && s.Topic == $"slave/{Id}/action") == 2, "satellite did not subscribe again");
            await Eventually.Assert(() => _h.Runtime.Connected, "satellite is not connected");

            // and it handles requests on the new connection
            await Publish($"slave/{Id}/action", JsonConvert.SerializeObject(Start()));
            await Eventually.Assert(() => _h.Docker.Started.Count == 1, "request after the reconnect was not handled");
        }

        [Fact]
        public async Task WatchdogTopic_UnknownOrNestedNode_IsIgnored()
        {
            await Publish($"watchdog/{Id}/{Guid.NewGuid()}", "");
            await Publish($"watchdog/{Id}/a/b", "");
            await Publish($"slave/{Id}/action", JsonConvert.SerializeObject(Start()));

            await Eventually.Assert(() => _h.Docker.Started.Count == 1, "sentinel request was not handled");
            Assert.Empty(_h.Docker.Removed);
        }
    }
}
