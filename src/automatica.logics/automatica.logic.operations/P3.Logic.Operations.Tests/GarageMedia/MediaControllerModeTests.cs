using P3.Logic.Operations.MediaController;
using Xunit;
using M = P3.Logic.Operations.MediaController.MediaControllerModeLogicFactory;

namespace P3.Logic.Operations.Tests.GarageMedia
{
    public class MediaControllerModeTests : BlockTest<M>
    {
        [Fact]
        public void Factory_DefaultModeIsOne()
        {
            Assert.Equal(1L, Interface(M.ParamMode).ValueInteger);
        }

        [Fact]
        public void Power_OnlyOfItsMode()
        {
            Param(M.ParamMode, 2);
            Start();

            Input(M.InputCommand, "1:power:1");
            Assert.Empty(Values(M.OutputPower));

            Input(M.InputCommand, "2:power:1");
            Assert.Equal(true, Last(M.OutputPower));
            Input(M.InputCommand, "2:power:0");
            Assert.Equal(false, Last(M.OutputPower));
        }

        [Fact]
        public void Commands_OfItsMode_ToTheTypedOutputs()
        {
            Param(M.ParamMode, 3);
            Start();

            Input(M.InputCommand, "3:volumeUp");
            Input(M.InputCommand, "3:volumeUp");
            Input(M.InputCommand, "3:volume:42.5");
            Input(M.InputCommand, "3:channel:12");
            Input(M.InputCommand, "3:navOk");
            Input(M.InputCommand, "1:home");

            Assert.Equal(2, Values(M.OutputVolumeUp).Count);
            Assert.Equal(42.5, Last(M.OutputVolume));
            Assert.Equal(12, Last(M.OutputChannel));
            Assert.Equal(true, Last(M.OutputNavOk));
            Assert.Empty(Values(M.OutputHome));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("volumeUp")]
        [InlineData("x:volumeUp")]
        [InlineData("9:volumeUp")]
        [InlineData("1:")]
        [InlineData("1:unknown")]
        [InlineData("1:volume:abc")]
        public void InvalidCommands_AreIgnored(string command)
        {
            Start();
            Input(M.InputCommand, command);
            Assert.Empty(Recorder.All());
        }

        [Fact]
        public void ModeChange_SwitchesThePowerOff()
        {
            Start();
            Input(M.InputCommand, "1:power:1");
            ChangeParam(M.ParamMode, 2);
            Assert.Equal(false, Last(M.OutputPower));
            Assert.Equal(2, VisuData["mode"]);
        }

        [Fact]
        public void Format_And_TryParse_RoundTrip()
        {
            Assert.True(MediaControllerCommand.TryParse(MediaControllerCommand.Format(4, "channel", "105"), out var mode, out var command, out var value));
            Assert.Equal(4, mode);
            Assert.Equal("channel", command);
            Assert.Equal("105", value);
        }
    }
}
