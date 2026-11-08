using Xunit;
using F = P3.Logic.Sonos.AudioCentral.AudioCentralLogicFactory;

namespace P3.Logic.Sonos.Tests.AudioCentral
{
    public class AudioCentralTests : BlockTest<F>
    {
        [Fact]
        public void Factory_HasEightPlayers()
        {
            Assert.Equal(8, F.InputPlayings.Length);
            Assert.Equal(8, F.InputPlayerVolumes.Length);
            Assert.Equal(8, F.OutputPlayerVolumes.Length);
            Assert.Equal("at-oe3;at-fm4;at-oe1", Interface(F.ParamFavorites).Value);
            Assert.Equal(Automatica.Core.EF.Models.RuleInterfaceParameterDataType.RadioStations, Interface(F.ParamFavorites).This2RuleInterfaceTemplateNavigation.ParameterDataType);
        }

        [Fact]
        public void OnOffPlayPause_SendThePlayState()
        {
            Start();
            Input(F.InputOn, true);
            Input(F.InputOff, true);
            Input(F.InputPlay, true);
            Input(F.InputPause, true);
            Input(F.InputPause, false);
            Assert.Equal(new object[] { true, false, true, false }, Values(F.OutputPlay));
        }

        [Fact]
        public void Toggle_OffWhenOnePlays_OnWhenNonePlays()
        {
            Start();
            Input(F.InputToggle, true);
            Assert.Equal(true, Last(F.OutputPlay));

            Input(F.InputPlayings[2], true);
            Input(F.InputToggle, true);
            Assert.Equal(false, Last(F.OutputPlay));
        }

        [Fact]
        public void ActiveCount_FromThePlayFeedback()
        {
            Start();
            Assert.Equal(0, Last(F.OutputActiveCount));
            Input(F.InputPlayings[0], true);
            Input(F.InputPlayings[4], true);
            Input(F.InputPlayings[5], false);
            Assert.Equal(2, Last(F.OutputActiveCount));
            Input(F.InputPlayings[0], false);
            Assert.Equal(1, Last(F.OutputActiveCount));
        }

        [Fact]
        public void Volume_KeepsTheRatioBetweenThePlayers()
        {
            Start();
            Input(F.InputPlayerVolumes[0], 40);
            Input(F.InputPlayerVolumes[1], 20);
            Input(F.InputVolume, 60);

            Assert.Equal(60.0, Last(F.OutputVolume));
            Assert.Equal(60.0, Last(F.OutputPlayerVolumes[0]));
            Assert.Equal(30.0, Last(F.OutputPlayerVolumes[1]));
            // players without feedback get the value itself
            Assert.Equal(60.0, Last(F.OutputPlayerVolumes[2]));

            Input(F.InputVolume, 30);
            Assert.Equal(30.0, Last(F.OutputPlayerVolumes[0]));
            Assert.Equal(15.0, Last(F.OutputPlayerVolumes[1]));
        }

        [Fact]
        public void Volume_AllSilent_GetTheValue()
        {
            Start();
            Input(F.InputPlayerVolumes[0], 0);
            Input(F.InputVolume, 25);
            Assert.Equal(25.0, Last(F.OutputPlayerVolumes[0]));
        }

        [Fact]
        public void VolumeButtons_AndTracks_AreSentEveryTime()
        {
            Start();
            Input(F.InputVolumeUp, true);
            Input(F.InputVolumeUp, true);
            Input(F.InputVolumeDown, true);
            Input(F.InputNext, true);
            Input(F.InputPrevious, true);
            Assert.Equal(new object[] { true, true }, Values(F.OutputVolumeUp));
            Assert.Equal(new object[] { true }, Values(F.OutputVolumeDown));
            Assert.Equal(new object[] { true }, Values(F.OutputNext));
            Assert.Equal(new object[] { true }, Values(F.OutputPrevious));
        }

        [Fact]
        public void NextFavorite_CyclesTheStationsAndPlays()
        {
            Param(F.ParamFavorites, "a; b");
            Start();
            Input(F.InputNextFavorite, true);
            Input(F.InputNextFavorite, true);
            Input(F.InputNextFavorite, true);
            Assert.Equal(new object[] { "a", "b", "a" }, Values(F.OutputRadioStation));
            Assert.Equal(true, Last(F.OutputPlay));
        }

        [Fact]
        public void BellAndAlarm_SendTheirUrlAndTrigger()
        {
            Param(F.ParamBellUrl, "http://x/bell.mp3");
            Start();
            Input(F.InputBell, true);
            Input(F.InputAlarm, true);
            Assert.Equal(new object[] { "http://x/bell.mp3" }, Values(F.OutputSoundUrl));
            Assert.Equal(new object[] { true }, Values(F.OutputSoundTrigger));
        }

        [Fact]
        public void Lock_PausesAndBlocks()
        {
            Start();
            Input(F.InputLock, true);
            Assert.Equal(false, Last(F.OutputPlay));
            Input(F.InputOn, true);
            VisuInput(F.InputOn, true);
            Assert.Equal(false, Last(F.OutputPlay));
            Input(F.InputLock, false);
            Input(F.InputOn, true);
            Assert.Equal(true, Last(F.OutputPlay));
        }

        [Fact]
        public void DisablePeriphery_OnlyTheVisu()
        {
            Start();
            Input(F.InputDisablePeriphery, true);
            Input(F.InputOn, true);
            Assert.Empty(Values(F.OutputPlay));
            VisuInput(F.InputOn, true);
            Assert.Equal(true, Last(F.OutputPlay));
            // feedback still counts
            Input(F.InputPlayings[0], true);
            Assert.Equal(1, Last(F.OutputActiveCount));
        }
    }
}
