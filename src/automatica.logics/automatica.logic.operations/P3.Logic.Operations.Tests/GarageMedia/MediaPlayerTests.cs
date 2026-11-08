using System.Threading.Tasks;
using Automatica.Core.Control.Base;
using Xunit;
using F = P3.Logic.Operations.MediaPlayer.MediaPlayerLogicFactory;

namespace P3.Logic.Operations.Tests.GarageMedia
{
    public class MediaPlayerTests : BlockTest<F>
    {
        private IMediaPlayer Player => (IMediaPlayer)Sut;

        [Fact]
        public void Is_a_media_player_control_with_the_on_off_volume_default()
        {
            Start();
            Assert.Equal(MediaPlaybackState.Unknown, Player.PlaybackState);
            Assert.Equal(MediaPlayerBridgeType.OnOffVolume, Player.BridgeType);
            Assert.Equal(RuleInstance.ObjId, Player.Id);
        }

        [Fact]
        public void Bridge_type_parameter_selects_the_speaker()
        {
            Param(F.ParamBridgeType, 1);
            Start();
            ChangeParam(F.ParamBridgeType, 1);
            Assert.Equal(MediaPlayerBridgeType.Speaker, Player.BridgeType);
            ChangeParam(F.ParamBridgeType, 7);
            Assert.Equal(MediaPlayerBridgeType.OnOffVolume, Player.BridgeType);
        }

        [Fact]
        public void Inputs_take_the_device_state_and_send_nothing()
        {
            Start();
            var notified = 0;
            Player.RegisterValueCallback(() => notified++);

            Input(F.InputPlaying, true);
            Input(F.InputVolume, 140);
            Input(F.InputMute, 1);
            Input(F.InputTitle, " Song ");
            Input(F.InputArtist, "");

            Assert.Equal(MediaPlaybackState.Playing, Player.PlaybackState);
            Assert.Equal(100, Player.Volume);
            Assert.True(Player.IsMuted);
            Assert.Equal("Song", Player.Title);
            Assert.Null(Player.Artist);
            Assert.Equal(5, notified);
            Assert.Empty(Recorder.All());

            Input(F.InputPlaying, false);
            Assert.Equal(MediaPlaybackState.Paused, Player.PlaybackState);
        }

        [Fact]
        public async Task Commands_go_out_on_the_outputs()
        {
            Start();
            await Player.PlayAsync();
            Assert.Equal(true, Last(F.OutputPlay));
            Assert.Equal(MediaPlaybackState.Playing, Player.PlaybackState);

            await Player.PauseAsync();
            Assert.Equal(false, Last(F.OutputPlay));
            Assert.Equal(MediaPlaybackState.Paused, Player.PlaybackState);

            await Player.StopAsync();
            await Player.NextAsync();
            await Player.NextAsync();
            await Player.PreviousAsync();
            Assert.Equal(new object[] { true }, Values(F.OutputStop));
            Assert.Equal(new object[] { true, true }, Values(F.OutputNext));
            Assert.Equal(new object[] { true }, Values(F.OutputPrevious));
            Assert.Equal(MediaPlaybackState.Stopped, Player.PlaybackState);

            await Player.SetVolumeAsync(-5);
            await Player.SetVolumeAsync(42);
            Assert.Equal(new object[] { 0, 42 }, Values(F.OutputVolume));
            Assert.Equal(42, Player.Volume);

            await Player.SetMuteAsync(true);
            Assert.Equal(true, Last(F.OutputMute));
            Assert.True(Player.IsMuted);
        }

        [Fact]
        public async Task Device_state_overrides_the_commanded_state()
        {
            Start();
            await Player.PlayAsync();
            Input(F.InputPlaying, false);
            Assert.Equal(MediaPlaybackState.Paused, Player.PlaybackState);
        }
    }
}
