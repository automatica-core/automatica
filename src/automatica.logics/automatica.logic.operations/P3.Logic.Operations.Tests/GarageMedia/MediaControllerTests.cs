using System.Linq;
using Xunit;
using F = P3.Logic.Operations.MediaController.MediaControllerLogicFactory;

namespace P3.Logic.Operations.Tests.GarageMedia
{
    public class MediaControllerTests : BlockTest<F>
    {
        private int Mode => (int)Last(F.OutputMode);

        [Fact]
        public void Factory_DefaultModeNames()
        {
            Assert.Equal("TV", Interface(F.ParamModeNames[0]).Value);
            Assert.Equal("Radio", Interface(F.ParamModeNames[1]).Value);
            Assert.Equal("", Interface(F.ParamModeNames[2]).Value);
            Assert.Equal(3000L, Interface(F.ParamNumpadTimeout).ValueInteger);
        }

        [Fact]
        public void Start_IsOff()
        {
            Start();
            Assert.Equal(0, Mode);
            Assert.Equal(false, Last(F.OutputPower));
            Assert.All(F.OutputModeActives, a => Assert.Equal(false, Last(a)));
        }

        [Fact]
        public void ModeInputs_SwitchTheModeOutputs_OldOffBeforeNewOn()
        {
            Start();
            Input(F.InputModes[1], true);
            Assert.Equal(2, Mode);
            Assert.Equal(true, Last(F.OutputModeActives[1]));
            Assert.Equal(true, Last(F.OutputPower));

            ClearRecorded();
            Input(F.InputMode, 1);
            var order = Recorder.All().Where(a => a.Id == F.OutputModeActives[0] || a.Id == F.OutputModeActives[1]).ToList();
            Assert.Equal(F.OutputModeActives[1], order[0].Id);
            Assert.Equal(false, order[0].Value);
            Assert.Equal(F.OutputModeActives[0], order[1].Id);
            Assert.Equal(true, order[1].Value);

            Input(F.InputMode, 0);
            Assert.Equal(0, Mode);
            Assert.Equal(false, Last(F.OutputPower));
        }

        [Fact]
        public void PowerToggle_StartsTheLastMode_ElseTheFirstNamedMode()
        {
            Param(F.ParamModeNames[0], "");
            Param(F.ParamModeNames[1], "");
            Param(F.ParamModeNames[2], "Bluray");
            Start();

            Input(F.InputPowerToggle, true);
            Assert.Equal(3, Mode);
            Input(F.InputModes[4], true);
            Input(F.InputPowerToggle, true);
            Assert.Equal(0, Mode);
            Input(F.InputPowerOn, true);
            Assert.Equal(5, Mode);
            Input(F.InputPowerOff, true);
            Assert.Equal(0, Mode);
        }

        [Fact]
        public void VolumeUp_WhenOff_OnlySwitchesOn()
        {
            Start();
            Input(F.InputVolumeUp, true);
            Assert.Equal(1, Mode);
            Assert.Empty(Values(F.OutputVolumeUp));

            Input(F.InputVolumeUp, true);
            Input(F.InputVolumeUp, true);
            Assert.Equal(new object[] { true, true }, Values(F.OutputVolumeUp));
        }

        [Fact]
        public void Commands_AreSentOnlyWhileOn()
        {
            Start();
            Input(F.InputChannelUp, true);
            Input(F.InputNavOk, true);
            Assert.Empty(Values(F.OutputChannelUp));

            Input(F.InputModes[0], true);
            Input(F.InputChannelUp, true);
            Input(F.InputNavOk, true);
            Input(F.InputNavOk, false);
            Input(F.InputHome, true);
            Input(F.InputPlayPause, true);
            Assert.Equal(new object[] { true }, Values(F.OutputChannelUp));
            Assert.Equal(new object[] { true }, Values(F.OutputNavOk));
            Assert.Equal(new object[] { true }, Values(F.OutputHome));
            Assert.Equal(new object[] { true }, Values(F.OutputPlayPause));
        }

        [Fact]
        public void Volume_And_Channel_AreSentAsValues()
        {
            Start();
            Input(F.InputVolume, 135);
            Input(F.InputChannel, 12);
            Assert.Equal(100.0, Last(F.OutputVolume));
            Assert.Equal(12, Last(F.OutputChannel));
        }

        [Fact]
        public void Digits_AreSentAsOneChannelAfterTheTimeout()
        {
            Param(F.ParamNumpadTimeout, 2000);
            Start();
            Input(F.InputModes[0], true);
            Input(F.InputDigit, 1);
            Input(F.InputDigit, 0);
            Input(F.InputDigit, 5);
            Assert.Empty(Values(F.OutputChannel));
            Assert.True(Timer().Enabled);
            Assert.Equal(2000, Timer().Interval);

            Fire();
            Assert.Equal(new object[] { 105 }, Values(F.OutputChannel));
        }

        [Fact]
        public void Digits_WithoutTimeout_AreSentRightAway()
        {
            Param(F.ParamNumpadTimeout, 0);
            Start();
            Input(F.InputModes[0], true);
            Input(F.InputDigit, 7);
            Assert.Equal(new object[] { 7 }, Values(F.OutputChannel));
        }

        [Fact]
        public void Lock_SwitchesOffAndBlocks()
        {
            Start();
            Input(F.InputModes[0], true);
            Input(F.InputLock, true);
            Assert.Equal(0, Mode);
            VisuInput(F.InputModes[0], true);
            Assert.Equal(0, Mode);
            Input(F.InputLock, false);
            VisuInput(F.InputModes[0], true);
            Assert.Equal(1, Mode);
        }

        [Fact]
        public void DisablePeriphery_OnlyTheVisuControls()
        {
            Start();
            Input(F.InputDisablePeriphery, true);
            Input(F.InputModes[0], true);
            Assert.Equal(0, Mode);
            VisuInput(F.InputModes[0], true);
            Assert.Equal(1, Mode);
        }

        [Fact]
        public void VisuData_HasModeNamesAndState()
        {
            Start();
            Input(F.InputModes[1], true);
            Input(F.InputVolume, 30);
            var data = VisuData;
            Assert.Equal(2, data["mode"]);
            Assert.Equal(true, data["power"]);
            Assert.Equal(30.0, data["volume"]);
            var names = (string[])data["modeNames"];
            Assert.Equal(8, names.Length);
            Assert.Equal("Radio", names[1]);
        }

        [Fact]
        public void CommandOutput_PowerPerMode_OldOffBeforeNewOn()
        {
            Start();
            Input(F.InputModes[0], true);
            Input(F.InputModes[1], true);
            Input(F.InputPowerOff, true);

            Assert.Equal(new object[] { "1:power:1", "1:power:0", "2:power:1", "2:power:0" }, Values(F.OutputCommand));
        }

        [Fact]
        public void CommandOutput_CommandsOfTheActiveMode_RepeatedPressesRepeat()
        {
            Param(F.ParamNumpadTimeout, 0);
            Start();
            Input(F.InputModes[2], true);
            ClearRecorded();

            Input(F.InputVolumeUp, true);
            Input(F.InputVolumeUp, true);
            Input(F.InputVolume, 42.5);
            Input(F.InputMute, true);
            Input(F.InputDigit, 7);
            Input(F.InputNavOk, true);

            Assert.Equal(new object[] { "3:volumeUp", "3:volumeUp", "3:volume:42.5", "3:mute", "3:channel:7", "3:navOk" },
                Values(F.OutputCommand));
            // the shared outputs stay as they were
            Assert.Equal(2, Values(F.OutputVolumeUp).Count);
            Assert.Equal(7, Last(F.OutputChannel));
        }

        [Fact]
        public void CommandOutput_NothingWhileOff()
        {
            Start();
            Input(F.InputVolume, 20);
            Input(F.InputChannel, 5);
            Assert.Empty(Values(F.OutputCommand));
        }
    }
}
