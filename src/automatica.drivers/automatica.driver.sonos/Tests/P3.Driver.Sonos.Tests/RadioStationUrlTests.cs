using Xunit;

namespace P3.Driver.Sonos.Tests
{
    public class RadioStationUrlTests
    {
        [Theory]
        [InlineData("s8007", "x-sonosapi-stream:s8007?sid=254&flags=32")]
        [InlineData(" 8007 ", "x-sonosapi-stream:s8007?sid=254&flags=32")]
        [InlineData("x-rincon-mp3radio://stream.example/live", "x-rincon-mp3radio://stream.example/live")]
        [InlineData("http://stream.example/live.mp3?x=1", "http://stream.example/live.mp3?x=1")]
        [InlineData("x-sonosapi-stream:s24896?sid=254&flags=8224", "x-sonosapi-stream:s24896?sid=254&flags=8224")]
        public void RadioStationUrl_TuneInIdOrUrl(string station, string expected)
        {
            Assert.Equal(expected, SonosController.RadioStationUrl(station));
        }
    }
}
