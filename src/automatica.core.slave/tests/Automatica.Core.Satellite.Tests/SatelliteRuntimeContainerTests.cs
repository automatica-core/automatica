using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Automatica.Core.Satellite.Abstraction;
using Automatica.Core.Satellite.Runtime;
using Automatica.Core.Satellite.Tests.Infrastructure;
using Docker.DotNet;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Automatica.Core.Satellite.Tests
{
    /// <summary>
    /// What the satellite asks docker to do for start/stop requests (docker is a fake, the broker is real)
    /// </summary>
    public class SatelliteRuntimeContainerTests
    {
        private static ActionRequest Start(string image = "automaticacore/automatica-plugin-knx", string tag = "1.0", Guid? id = null, string clientKey = TestKeys.PluginKey) => new()
        {
            Action = SlaveAction.Start,
            ImageName = image,
            Tag = tag,
            ImageSource = "https://hub.docker.com",
            Id = id ?? Guid.NewGuid(),
            ClientKey = clientKey
        };

        private static ActionRequest Stop(ActionRequest start) => new()
        {
            Action = SlaveAction.Stop,
            ImageName = start.ImageName,
            Tag = start.Tag,
            Id = start.Id
        };

        private static bool IsUnixLike => OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();

        [Fact]
        public async Task StartAsync_Configured_ConnectsToTheBroker()
        {
            await using var h = await RuntimeHarness.StartAsync();

            Assert.True(h.Runtime.Connected);
            Assert.False(h.Runtime.ContainerStarted);
            Assert.Equal(RuntimeHarness.SatelliteId, h.Runtime.SlaveId);
        }

        [Fact]
        public async Task StartImage_AllowedImage_CreatesAnUnprivilegedManagedContainer()
        {
            await using var h = await RuntimeHarness.StartAsync();
            var request = Start();

            await h.Runtime.ExecuteAction(request);

            var created = Assert.Single(h.Docker.Created);
            Assert.Equal("automaticacore/automatica-plugin-knx:1.0", created.Image);
            Assert.Equal(request.Id.ToString(), created.Name);
            Assert.False(created.HostConfig.Privileged);
            Assert.Equal("host", created.HostConfig.NetworkMode);
            Assert.Contains("no-new-privileges", created.HostConfig.SecurityOpt);
            Assert.Empty(created.HostConfig.Mounts);
            Assert.True(created.HostConfig.Binds == null || created.HostConfig.Binds.Count == 0);
            Assert.Equal("true", created.Labels[ContainerPolicy.ManagedLabel]);
            Assert.Equal(RuntimeHarness.SatelliteId, created.Labels[ContainerPolicy.SatelliteLabel]);
            Assert.Equal(request.Id.ToString(), created.Labels[ContainerPolicy.NodeLabel]);

            var pulled = Assert.Single(h.Docker.Pulled);
            Assert.Equal("automaticacore/automatica-plugin-knx", pulled.FromImage);
            Assert.Equal("1.0", pulled.Tag);

            Assert.Equal(new[] { "docker-1" }, h.Docker.Started);
            Assert.True(h.Runtime.ContainerStarted);
            Assert.True(h.Runtime.RunningImages.ContainsKey(request.Id.ToString()));
            Assert.Equal("docker-1", h.Runtime.RunningImages[request.Id.ToString()].DockerId);
        }

        [Fact]
        public async Task StartImage_PassesThePluginCredentialsAndNotTheSatelliteKey()
        {
            await using var h = await RuntimeHarness.StartAsync();
            var request = Start();

            await h.Runtime.ExecuteAction(request);

            var env = Assert.Single(h.Docker.Created).Env;
            Assert.Contains("AUTOMATICA_SLAVE_MASTER=127.0.0.1", env);
            Assert.Contains($"AUTOMATICA_SLAVE_USER={RuntimeHarness.SatelliteId}", env);
            Assert.Contains($"AUTOMATICA_SLAVE_PASSWORD={TestKeys.PluginKey}", env);
            Assert.Contains($"AUTOMATICA_NODE_ID={request.Id}", env);
            Assert.Contains("LOG_LEVEL=Warning", env);
            Assert.DoesNotContain(env, e => e.Contains(TestKeys.SatelliteKey));
            // plain connection: no TLS settings for the plugin
            Assert.DoesNotContain(env, e => e.StartsWith("AUTOMATICA_SLAVE_TLS_"));
        }

        [Fact]
        public async Task StartImage_OldCoreWithoutPluginKey_FallsBackToTheSatelliteKeyAndWarns()
        {
            await using var h = await RuntimeHarness.StartAsync();

            await h.Runtime.ExecuteAction(Start(clientKey: null));

            Assert.Contains($"AUTOMATICA_SLAVE_PASSWORD={TestKeys.SatelliteKey}", Assert.Single(h.Docker.Created).Env);
            Assert.True(h.Logs.Contains(LogLevel.Warning, "No plugin credentials"));
        }

        [Theory]
        [InlineData("evil/miner")]
        [InlineData("ubuntu")]
        [InlineData("ghcr.io/automaticacore/x")]
        [InlineData("automaticacore/../evil")]
        public async Task StartImage_DisallowedImage_TouchesNothingInDocker(string image)
        {
            await using var h = await RuntimeHarness.StartAsync();

            await h.Runtime.ExecuteAction(Start(image));

            Assert.Empty(h.Docker.Pulled);
            Assert.Empty(h.Docker.Created);
            Assert.Empty(h.Docker.Started);
            Assert.Empty(h.Runtime.RunningImages);
            Assert.False(h.Runtime.ContainerStarted);
            Assert.True(h.Logs.Contains(LogLevel.Error, "Rejected start request"));
        }

        [Fact]
        public async Task StartImage_ImageSourceUrl_IsRejected()
        {
            await using var h = await RuntimeHarness.StartAsync();
            var request = Start();
            request.ImageSource = "http://attacker.example/rootfs.tar";

            await h.Runtime.ExecuteAction(request);

            Assert.Empty(h.Docker.Pulled);
            Assert.Empty(h.Docker.Created);
        }

        [Fact]
        public async Task StartImage_ConfiguredDockerTag_OverridesTheRequestTag()
        {
            await using var h = await RuntimeHarness.StartAsync(new Dictionary<string, string> { { "server:dockerTag", "latest-develop" } });

            await h.Runtime.ExecuteAction(Start(tag: "1.0"));

            Assert.Equal("automaticacore/automatica-plugin-knx:latest-develop", Assert.Single(h.Docker.Created).Image);
            Assert.Equal("latest-develop", Assert.Single(h.Docker.Pulled).Tag);
        }

        [Fact]
        public async Task StartImage_PinnedDigest_PullsAndRunsByDigest()
        {
            await using var h = await RuntimeHarness.StartAsync(configureDocker: d => d.RepoDigests = new List<string> { $"automaticacore/automatica-plugin-knx@{TestKeys.Digest}" });
            var request = Start();
            request.ImageDigest = TestKeys.Digest;

            await h.Runtime.ExecuteAction(request);

            Assert.Equal(TestKeys.Digest, Assert.Single(h.Docker.Pulled).Tag);
            Assert.Equal($"automaticacore/automatica-plugin-knx@{TestKeys.Digest}", Assert.Single(h.Docker.Created).Image);
        }

        [Fact]
        public async Task StartImage_PinnedDigestWithDockerIoPrefix_IsAccepted()
        {
            await using var h = await RuntimeHarness.StartAsync(configureDocker: d => d.RepoDigests = new List<string> { $"docker.io/automaticacore/automatica-plugin-knx@{TestKeys.Digest}" });
            var request = Start();
            request.ImageDigest = TestKeys.Digest;

            await h.Runtime.ExecuteAction(request);

            Assert.Single(h.Docker.Created);
        }

        [Theory]
        [InlineData("automaticacore/automatica-plugin-knx@sha256:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc")]
        [InlineData("evil/automatica-plugin-knx@sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb")]
        [InlineData("automaticacore/automatica-plugin-knx-other@sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb")]
        public async Task StartImage_PulledImageWithoutTheRequestedDigest_IsNotStarted(string repoDigest)
        {
            await using var h = await RuntimeHarness.StartAsync(configureDocker: d => d.RepoDigests = new List<string> { repoDigest });
            var request = Start();
            request.ImageDigest = TestKeys.Digest;

            await h.Runtime.ExecuteAction(request);

            Assert.Single(h.Docker.Pulled);
            Assert.Empty(h.Docker.Created);
            Assert.True(h.Logs.Contains(LogLevel.Error, "does not have the requested digest"));
        }

        [Fact]
        public async Task StartImage_SameIdTwice_StartsOnlyOnce()
        {
            await using var h = await RuntimeHarness.StartAsync();
            var request = Start();

            await h.Runtime.ExecuteAction(request);
            await h.Runtime.ExecuteAction(request);

            Assert.Single(h.Docker.Created);
            Assert.True(h.Logs.Contains(LogLevel.Warning, "already running"));
        }

        [Fact]
        public async Task StartImage_ForeignContainerWithTheSameName_IsNotReplaced()
        {
            await using var h = await RuntimeHarness.StartAsync();
            var request = Start();
            h.Docker.AddForeignContainer(request.Id.ToString(), "foreign-1");

            await h.Runtime.ExecuteAction(request);

            Assert.Empty(h.Docker.Removed);
            Assert.Empty(h.Docker.Created);
            Assert.True(h.Logs.Contains(LogLevel.Error, "was not created by the satellite"));
        }

        [Fact]
        public async Task StartImage_OldManagedContainer_IsRemovedFirst()
        {
            await using var h = await RuntimeHarness.StartAsync();
            var request = Start();
            h.Docker.AddManagedContainer(request.Id.ToString(), "old-1");

            await h.Runtime.ExecuteAction(request);

            Assert.Equal(new[] { "old-1" }, h.Docker.Removed);
            Assert.Single(h.Docker.Created);
        }

        [Fact]
        public async Task StartImage_RequestedDevicesAndCapabilities_OnlyFromTheAllowList()
        {
            await using var h = await RuntimeHarness.StartAsync();
            var request = Start();
            request.Devices = new[] { "/dev/mem", "/dev/sda", "/dev/../dev/kmsg" };
            request.CapAdd = new[] { "SYS_ADMIN", "NET_ADMIN", "ALL" };

            await h.Runtime.ExecuteAction(request);

            var host = Assert.Single(h.Docker.Created).HostConfig;
            Assert.False(host.Privileged);
            Assert.True(host.Devices == null || host.Devices.Count == 0);
            Assert.True(host.CapAdd == null || host.CapAdd.Count == 0);
        }

        [Fact]
        public async Task StartImage_AllowedCapability_IsAddedOnLinux()
        {
            await using var h = await RuntimeHarness.StartAsync(new Dictionary<string, string> { { "docker:allowedCapabilities", "NET_ADMIN" } });
            var request = Start();
            request.CapAdd = new[] { "NET_ADMIN", "SYS_ADMIN" };

            await h.Runtime.ExecuteAction(request);

            var host = Assert.Single(h.Docker.Created).HostConfig;
            if (IsUnixLike)
            {
                Assert.Equal(new[] { "NET_ADMIN" }, host.CapAdd);
            }
            else
            {
                // devices and capabilities are only set on a Linux/macOS docker host
                Assert.Null(host.CapAdd);
            }
        }

        [Fact]
        public async Task StartImage_PrivilegedOnlyWithTheExplicitSatelliteSetting()
        {
            await using var h = await RuntimeHarness.StartAsync(new Dictionary<string, string> { { "docker:privileged", "true" } });

            await h.Runtime.ExecuteAction(Start());

            Assert.Equal(IsUnixLike, Assert.Single(h.Docker.Created).HostConfig.Privileged);
            if (IsUnixLike)
            {
                Assert.True(h.Logs.Contains(LogLevel.Warning, "docker:privileged is set"));
            }
        }

        [Fact]
        public async Task StartImage_DockerApiError_IsNotRecordedAsRunningAndReconnects()
        {
            await using var h = await RuntimeHarness.StartAsync(configureDocker: d => d.CreateContainerError = new DockerApiException(System.Net.HttpStatusCode.InternalServerError, "boom"));

            await h.Runtime.ExecuteAction(Start());

            Assert.Empty(h.Runtime.RunningImages);
            Assert.False(h.Runtime.ContainerStarted);
            // the satellite restarts its connection after a docker error
            await Eventually.Assert(() => h.Broker.Connects.Count >= 2, "expected a reconnect after the docker error");
            await Eventually.Assert(() => h.Runtime.Connected, "expected the satellite to be connected again");
        }

        [Theory]
        [InlineData(SlaveAction.Restart)]
        [InlineData(SlaveAction.Kill)]
        public async Task ExecuteAction_UnsupportedActions_AreIgnored(SlaveAction action)
        {
            await using var h = await RuntimeHarness.StartAsync();
            var request = Start();
            request.Action = action;

            await h.Runtime.ExecuteAction(request);

            Assert.Empty(h.Docker.Pulled);
            Assert.Empty(h.Docker.Created);
            Assert.Empty(h.Docker.Stopped);
        }

        [Fact]
        public async Task StopImage_RunningContainer_IsStoppedRemovedAndItsImageDeleted()
        {
            await using var h = await RuntimeHarness.StartAsync();
            var request = Start();
            await h.Runtime.ExecuteAction(request);

            await h.Runtime.ExecuteAction(Stop(request));

            Assert.Equal(new[] { "docker-1" }, h.Docker.Stopped);
            Assert.Equal(new[] { "docker-1" }, h.Docker.Removed);
            Assert.Equal(new[] { "automaticacore/automatica-plugin-knx:1.0" }, h.Docker.DeletedImages);
            Assert.Empty(h.Runtime.RunningImages);
        }

        [Fact]
        public async Task StopImage_ForeignContainer_IsLeftAlone()
        {
            await using var h = await RuntimeHarness.StartAsync();
            var request = Start();
            h.Docker.AddForeignContainer(request.Id.ToString(), "foreign-1");

            await h.Runtime.ExecuteAction(Stop(request));

            Assert.Empty(h.Docker.Stopped);
            Assert.Empty(h.Docker.Removed);
            Assert.Empty(h.Docker.DeletedImages);
            Assert.True(h.Logs.Contains(LogLevel.Error, "was not created by the satellite"));
        }

        [Fact]
        public async Task StopImage_DisallowedImage_DoesNotDeleteAnyImage()
        {
            await using var h = await RuntimeHarness.StartAsync();
            var request = Stop(Start("library/postgres"));

            await h.Runtime.ExecuteAction(request);

            Assert.Empty(h.Docker.DeletedImages);
        }

        [Fact]
        public async Task StopImage_NoContainer_StillDeletesTheAllowedImage()
        {
            await using var h = await RuntimeHarness.StartAsync();

            await h.Runtime.ExecuteAction(Stop(Start()));

            Assert.Empty(h.Docker.Stopped);
            Assert.Equal(new[] { "automaticacore/automatica-plugin-knx:1.0" }, h.Docker.DeletedImages);
        }

        [Fact]
        public async Task ReInit_StopsAndRemovesAllContainers()
        {
            await using var h = await RuntimeHarness.StartAsync();
            await h.Runtime.ExecuteAction(Start());
            await h.Runtime.ExecuteAction(Start());

            await h.Runtime.ReInit();

            Assert.Equal(new[] { "docker-1", "docker-2" }, h.Docker.Stopped.OrderBy(a => a));
            Assert.Equal(new[] { "docker-1", "docker-2" }, h.Docker.Removed.OrderBy(a => a));
            Assert.Empty(h.Runtime.RunningImages);
        }

        [Fact]
        public async Task RestartPlugin_RunningNode_RecreatesItsContainer()
        {
            await using var h = await RuntimeHarness.StartAsync();
            var request = Start();
            await h.Runtime.ExecuteAction(request);

            await h.Runtime.RestartPlugin(request.Id.ToString());

            Assert.Equal(new[] { "docker-1" }, h.Docker.Removed);
            Assert.Equal(2, h.Docker.Created.Count);
            Assert.Equal("docker-2", h.Runtime.RunningImages[request.Id.ToString()].DockerId);
        }

        [Fact]
        public async Task RestartPlugin_UnknownNode_DoesNothing()
        {
            await using var h = await RuntimeHarness.StartAsync();

            await h.Runtime.RestartPlugin(Guid.NewGuid().ToString());

            Assert.Empty(h.Docker.Removed);
            Assert.Empty(h.Docker.Created);
        }

        [Fact]
        public async Task StartAsync_NotConfigured_DoesNotConnectOrTouchDocker()
        {
            await using var h = await RuntimeHarness.StartAsync(new Dictionary<string, string> { { "server:clientKey", "" } });

            Assert.False(h.Runtime.Connected);
            Assert.Empty(h.Broker.Connects);
            h.Docker.Images.Verify(i => i.ListImagesAsync(Moq.It.IsAny<Docker.DotNet.Models.ImagesListParameters>(), Moq.It.IsAny<System.Threading.CancellationToken>()), Moq.Times.Never);
            Assert.True(h.Logs.Contains(LogLevel.Warning, "not configured"));
        }

        [Theory]
        [InlineData("server:master")]
        [InlineData("server:clientId")]
        public async Task StartAsync_MissingServerOrId_DoesNotConnect(string key)
        {
            await using var h = await RuntimeHarness.StartAsync(new Dictionary<string, string> { { key, "" } });

            Assert.False(h.Runtime.Connected);
            Assert.Empty(h.Broker.Connects);
        }

        [Fact]
        public async Task StartAsync_DockerUnavailable_DoesNotConnect()
        {
            await using var h = await RuntimeHarness.StartAsync(configureDocker: d => d.ListImagesError = new HttpRequestException("no docker"));

            Assert.False(h.Runtime.Connected);
            Assert.Empty(h.Broker.Connects);
            Assert.True(h.Logs.Contains(LogLevel.Error, "Error connecting to docker"));
        }

        [Fact]
        public async Task StopAsync_DisconnectsAndStopsTheContainers()
        {
            var h = await RuntimeHarness.StartAsync();
            try
            {
                await h.Runtime.ExecuteAction(Start());

                await h.Runtime.StopAsync(System.Threading.CancellationToken.None);

                Assert.False(h.Runtime.Connected);
                Assert.Equal(new[] { "docker-1" }, h.Docker.Stopped);
                await Eventually.Assert(async () => await h.Broker.ConnectedClientCountAsync() == 0, "the satellite should have disconnected");
                // a disconnect the satellite caused itself does not trigger a reconnect
                await Task.Delay(200);
                Assert.Single(h.Broker.Connects);
            }
            finally
            {
                await h.Broker.DisposeAsync();
            }
        }
    }

}
