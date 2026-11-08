using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Automatica.Core.Control.Base;
using P3.Driver.Matter.Crypto;
using P3.Driver.Matter.DataModel.Clusters;
using P3.Driver.Matter.Driver;
using P3.Driver.Matter.InteractionModel;
using P3.Driver.Matter.Storage;
using P3.Driver.Matter.Tlv;

// stands in for the core interface the plugin finds by name (the plugin builds against a core NuGet without it)
namespace Automatica.Core.Control.Base
{
    public enum TestPlaybackState { Unknown = -1, Stopped = 0, Playing = 1, Paused = 2 }

    public enum TestBridgeType { OnOffVolume = 0, Speaker = 1 }

    public interface IMediaPlayer : IControl, IControlValueCallback
    {
        TestPlaybackState PlaybackState { get; }
        int Volume { get; }
        bool IsMuted { get; }
        string Title { get; }
        string Artist { get; }
        TestBridgeType BridgeType { get; }
        Task PlayAsync(CancellationToken token = default);
        Task PauseAsync(CancellationToken token = default);
        Task StopAsync(CancellationToken token = default);
        Task NextAsync(CancellationToken token = default);
        Task PreviousAsync(CancellationToken token = default);
        Task SetVolumeAsync(int volume, CancellationToken token = default);
        Task SetMuteAsync(bool muted, CancellationToken token = default);
    }
}

namespace P3.Driver.Matter.Tests
{
    public sealed class MediaPlayerTests : IAsyncLifetime
    {
        private readonly MatterBridge _bridge = new(new MatterNodeOptions { Port = 0, EnableMdns = false }, new InMemoryMatterStorage());
        private readonly List<string> _calls = new();
        private BridgedMediaPlayer _player;
        private BridgedMediaPlayer _speaker;
        private MiniController _controller;
        private TestCertificateAuthority _ca;

        public async Task InitializeAsync()
        {
            await _bridge.InitializeAsync();
            _player = _bridge.Add(new BridgedMediaPlayer("player", "Kitchen music")
            {
                SwitchRequested = on => Record(on ? "play" : "pause"),
                VolumeRequested = v => Record($"volume {v}")
            });
            _speaker = _bridge.Add(new BridgedMediaPlayer("speaker", "Living room", MediaPlayerDeviceKind.Speaker)
            {
                SwitchRequested = on => Record(on ? "unmute" : "mute"),
                VolumeRequested = v => Record($"volume {v}")
            });
            await _bridge.StartAsync();
            _controller = new MiniController(_bridge.Node.Port);
            _ca = new TestCertificateAuthority(0x4321);
            await CommissionAsync();
        }

        public async Task DisposeAsync()
        {
            _controller.Dispose();
            _ca.Dispose();
            await _bridge.DisposeAsync();
        }

        private Task Record(string call)
        {
            lock (_calls) _calls.Add(call);
            return Task.CompletedTask;
        }

        private string[] Calls()
        {
            lock (_calls) return _calls.ToArray();
        }

        [Fact]
        public async Task Player_is_a_dimmable_plug_and_the_speaker_a_matter_speaker()
        {
            foreach (var (device, type, levelFeatures) in new[] { (_player, 0x010Bu, 0x03ul), (_speaker, 0x0022u, 0x01ul) })
            {
                var reports = await _controller.ReadAsync((device.EndpointId, null, null));
                Assert.DoesNotContain(reports, r => r.Get(0) != null);
                var types = reports.Data(device.EndpointId, DescriptorCluster.Id, DescriptorCluster.AttributeDeviceTypeList).Children.Select(c => c.GetUInt(0));
                Assert.Contains((ulong)type, types);
                Assert.Equal(levelFeatures, reports.Data(device.EndpointId, LevelControlCluster.Id, 0xFFFC).AsUInt());
            }
        }

        [Fact]
        public async Task On_plays_off_pauses_and_the_level_is_the_volume()
        {
            Assert.Equal(StatusCode.Success, MiniController.InvokeStatus(await _controller.InvokeAsync(_player.EndpointId, OnOffCluster.Id, 0x01)));
            Assert.Equal(StatusCode.Success, MiniController.InvokeStatus(await _controller.InvokeAsync(_player.EndpointId, OnOffCluster.Id, 0x00)));
            Assert.Equal(new[] { "play", "pause" }, Calls());

            // "set the kitchen music to 50 %" while paused: volume, then play
            Assert.Equal(StatusCode.Success, MiniController.InvokeStatus(await MoveToLevelWithOnOff(_player.EndpointId, 127)));
            Assert.Equal(new[] { "play", "pause", "volume 50", "play" }, Calls());
            Assert.True(_player.IsOn);
        }

        [Fact]
        public async Task Speaker_on_off_is_mute()
        {
            _speaker.SetState(true, 30);
            Assert.Equal(StatusCode.Success, MiniController.InvokeStatus(await _controller.InvokeAsync(_speaker.EndpointId, OnOffCluster.Id, 0x00)));
            Assert.Equal(StatusCode.Success, MiniController.InvokeStatus(await MoveToLevelWithOnOff(_speaker.EndpointId, 254)));
            Assert.Equal(new[] { "mute", "volume 100", "unmute" }, Calls());
        }

        [Fact]
        public async Task State_from_the_player_reaches_the_controller()
        {
            _player.SetState(true, 40);
            var reports = await _controller.ReadAsync((_player.EndpointId, OnOffCluster.Id, 0x0000), (_player.EndpointId, LevelControlCluster.Id, 0x0000));
            Assert.True(reports.Data(_player.EndpointId, OnOffCluster.Id, 0x0000).AsBool());
            Assert.Equal((ulong)LevelControlCluster.PercentToLevel(40), reports.Data(_player.EndpointId, LevelControlCluster.Id, 0x0000).AsUInt());
        }

        private Task<TlvElement> MoveToLevelWithOnOff(ushort endpoint, byte level) =>
            _controller.InvokeAsync(endpoint, LevelControlCluster.Id, 0x04, w =>
            {
                w.PutUInt(0, level);
                w.PutUInt(1, 0);
                w.PutUInt(2, 0);
                w.PutUInt(3, 0);
            });

        private async Task CommissionAsync()
        {
            const ulong controllerNodeId = 0x1111;
            const ulong deviceNodeId = 0x2222;
            Assert.NotNull(await _controller.EstablishPaseAsync(_bridge.Node.State.Passcode));
            await _controller.InvokeWithResponseAsync(0, GeneralCommissioningCluster.Id, 0x00, w =>
            {
                w.PutUInt(0, 60);
                w.PutUInt(1, 1);
            });
            var nocsr = TlvReader.Decode((await _controller.InvokeWithResponseAsync(0, OperationalCredentialsCluster.Id, 0x04,
                w => w.PutBytes(0, MatterCrypto.RandomBytes(32)))).GetBytes(0));
            var request = CertificateRequest.LoadSigningRequest(nocsr.GetBytes(1), HashAlgorithmName.SHA256);
            using var csrKey = request.PublicKey.GetECDsaPublicKey()!;
            await _controller.InvokeAsync(0, OperationalCredentialsCluster.Id, 0x0B, w => w.PutBytes(0, _ca.RootCert));
            var ipk = MatterCrypto.RandomBytes(16);
            var noc = await _controller.InvokeWithResponseAsync(0, OperationalCredentialsCluster.Id, 0x06, w =>
            {
                w.PutBytes(0, _ca.IssueNoc(MatterCrypto.ExportPublicKey(csrKey), deviceNodeId));
                w.PutBytes(2, ipk);
                w.PutUInt(3, controllerNodeId);
                w.PutUInt(4, 0xFFF1);
            });
            Assert.Equal(0ul, noc.GetUInt(0));
            using var key = MatterCrypto.CreateSigningKey();
            Assert.NotNull(await _controller.EstablishCaseAsync(_ca, ipk, controllerNodeId, key, _ca.IssueNoc(MatterCrypto.ExportPublicKey(key), controllerNodeId),
                deviceNodeId));
            Assert.Equal(0ul, (await _controller.InvokeWithResponseAsync(0, GeneralCommissioningCluster.Id, 0x04)).GetUInt(0));
        }
    }

    /// <summary>The driver reaches a core media player by the interface name.</summary>
    public sealed class MediaPlayerProxyTests
    {
        private sealed class FakePlayer : IMediaPlayer
        {
            public readonly List<string> Calls = new();
            public Guid Id { get; } = Guid.NewGuid();
            public string Name => "Kitchen";
            public TestPlaybackState PlaybackState { get; set; } = TestPlaybackState.Paused;
            public int Volume { get; set; } = 35;
            public bool IsMuted { get; set; }
            public string Title => null;
            public string Artist => null;
            public TestBridgeType BridgeType { get; set; }
            public Action Callback;

            public Task PlayAsync(CancellationToken token = default) => Log("play");
            public Task PauseAsync(CancellationToken token = default) => Log("pause");
            public Task StopAsync(CancellationToken token = default) => Log("stop");
            public Task NextAsync(CancellationToken token = default) => Log("next");
            public Task PreviousAsync(CancellationToken token = default) => Log("previous");
            public Task SetVolumeAsync(int volume, CancellationToken token = default) => Log($"volume {volume}");
            public Task SetMuteAsync(bool muted, CancellationToken token = default) => Log(muted ? "mute" : "unmute");

            public Guid RegisterValueCallback(Action callback)
            {
                Callback = callback;
                return Guid.NewGuid();
            }

            public void UnregisterValueCallback(Guid id)
            {
            }

            private Task Log(string call)
            {
                Calls.Add(call);
                return Task.CompletedTask;
            }
        }

        private sealed class PlainControl : IControl
        {
            public Guid Id { get; } = Guid.NewGuid();
            public string Name => "Other";
        }

        [Fact]
        public async Task Reads_state_and_calls_commands_by_name()
        {
            var fake = new FakePlayer();
            var proxy = MediaPlayerProxy.TryCreate(fake);
            Assert.NotNull(proxy);
            Assert.False(proxy.IsPlaying);
            Assert.Equal(35, proxy.Volume);
            Assert.False(proxy.IsSpeaker);
            Assert.Equal((false, 35), MatterDriver.MediaState(proxy));

            fake.PlaybackState = TestPlaybackState.Playing;
            Assert.Equal((true, 35), MatterDriver.MediaState(proxy));

            await proxy.PlayAsync();
            await proxy.PauseAsync();
            await proxy.SetVolumeAsync(60);
            await proxy.SetMuteAsync(true);
            Assert.Equal(new[] { "play", "pause", "volume 60", "mute" }, fake.Calls);

            var called = false;
            proxy.RegisterValueCallback(() => called = true);
            fake.Callback();
            Assert.True(called);
        }

        [Fact]
        public void Speaker_state_is_unmuted()
        {
            var fake = new FakePlayer { BridgeType = TestBridgeType.Speaker, IsMuted = true, PlaybackState = TestPlaybackState.Playing };
            var proxy = MediaPlayerProxy.TryCreate(fake);
            Assert.True(proxy.IsSpeaker);
            Assert.Equal((false, 35), MatterDriver.MediaState(proxy));
        }

        [Fact]
        public void Other_controls_are_no_media_players()
        {
            Assert.Null(MediaPlayerProxy.TryCreate(new PlainControl()));
        }
    }
}
