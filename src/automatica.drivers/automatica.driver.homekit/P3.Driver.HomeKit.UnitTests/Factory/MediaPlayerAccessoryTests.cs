using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Control.Base;
using P3.Driver.HomeKit.Hap.Model;
using P3.Driver.HomeKitFactory;
using P3.Driver.HomeKitFactory.Accessories;
using Xunit;

// stands in for the core interface the plugin finds by name (the plugin builds against a core NuGet without it)
namespace Automatica.Core.Control.Base
{
    public enum TestPlaybackState { Unknown = -1, Stopped = 0, Playing = 1, Paused = 2 }

    public interface IMediaPlayer : IControl, IControlValueCallback
    {
        TestPlaybackState PlaybackState { get; }
        int Volume { get; }
        bool IsMuted { get; }
        Task PlayAsync(CancellationToken token = default);
        Task PauseAsync(CancellationToken token = default);
        Task SetVolumeAsync(int volume, CancellationToken token = default);
        Task SetMuteAsync(bool muted, CancellationToken token = default);
    }
}

namespace P3.Driver.HomeKit.UnitTests.Factory
{
    public class MediaPlayerAccessoryTests
    {
        private sealed class FakePlayer : IMediaPlayer
        {
            public readonly List<string> Calls = new();
            private Action _callback;

            public Guid Id { get; } = Guid.NewGuid();
            public string Name => "Kitchen";
            public TestPlaybackState PlaybackState { get; set; } = TestPlaybackState.Paused;
            public int Volume { get; set; } = 35;
            public bool IsMuted { get; set; }

            public void Changed() => _callback?.Invoke();

            public Task PlayAsync(CancellationToken token = default) => Log("play");
            public Task PauseAsync(CancellationToken token = default) => Log("pause");
            public Task SetVolumeAsync(int volume, CancellationToken token = default) => Log($"volume {volume}");
            public Task SetMuteAsync(bool muted, CancellationToken token = default) => Log(muted ? "mute" : "unmute");

            public Guid RegisterValueCallback(Action callback)
            {
                _callback = callback;
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

        private readonly FakePlayer _fake = new();
        private readonly List<(Characteristic Characteristic, object Value)> _written = new();

        private MediaPlayerAccessory Bind() =>
            MediaPlayerAccessory.Bind(MediaPlayerAccessory.CreateAccessory(5, "Kitchen", "1"), MediaPlayerProxy.TryCreate(_fake), c => _written.Add((c, c.Value)));

        [Fact]
        public void Is_a_switch_with_a_speaker_service()
        {
            var accessory = MediaPlayerAccessory.CreateAccessory(5, "Kitchen", "1");

            Assert.Equal(AccessoryFactory.SwitchType, accessory.Specific.Type);
            Assert.Equal(new[] { CharacteristicBase.OnType }, accessory.Specific.Characteristics.Select(c => c.Type));

            var speaker = Assert.Single(accessory.Services, s => s.Type == AccessoryFactory.SpeakerType);
            Assert.Equal(new[] { CharacteristicBase.MuteType, CharacteristicBase.VolumeType }, speaker.Characteristics.Select(c => c.Type));
            var volume = speaker.Characteristics[1];
            Assert.Equal(CharacteristicFormat.UInt8, volume.Format);
            Assert.Contains("pw", volume.Permissions);

            // services and characteristics share the instance id space of the accessory
            var ids = accessory.Services.Select(s => s.Id).Concat(accessory.Services.SelectMany(s => s.Characteristics).Select(c => c.Id)).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }

        [Fact]
        public void Takes_the_player_state_and_notifies_only_changes()
        {
            _fake.PlaybackState = TestPlaybackState.Playing;
            var binding = Bind();

            Assert.Equal(true, binding.Playing.Value);
            Assert.Equal(false, binding.Mute.Value);
            Assert.Equal(35, binding.Volume.Value);
            Assert.Empty(_written);

            _fake.Volume = 140;
            _fake.PlaybackState = TestPlaybackState.Stopped;
            _fake.Changed();

            Assert.Equal(new (Characteristic, object)[] { (binding.Playing, false), (binding.Volume, 100) }, _written);
        }

        [Fact]
        public async Task Writes_play_pause_mute_and_set_the_volume()
        {
            var binding = Bind();

            await binding.HandleWriteAsync(binding.Playing, true);
            await binding.HandleWriteAsync(binding.Playing, 0L);
            await binding.HandleWriteAsync(binding.Mute, 1);
            await binding.HandleWriteAsync(binding.Mute, false);
            await binding.HandleWriteAsync(binding.Volume, 42.0);

            Assert.Equal(new[] { "play", "pause", "mute", "unmute", "volume 42" }, _fake.Calls);
        }

        [Fact]
        public void Other_controls_are_no_media_players()
        {
            Assert.Null(MediaPlayerProxy.TryCreate(new PlainControl()));
            Assert.NotNull(MediaPlayerProxy.TryCreate(_fake));
        }
    }
}
