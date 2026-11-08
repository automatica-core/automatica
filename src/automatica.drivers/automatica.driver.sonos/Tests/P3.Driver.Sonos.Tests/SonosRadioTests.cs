using Xunit;

namespace P3.Driver.Sonos.Tests
{
    public class SonosRadioTests
    {
        [Theory]
        [InlineData("s8007")]
        [InlineData("S8007")]
        [InlineData(" 8007 ")]
        public void TuneInId_PlaysTuneInWithTheStationName(string tuneInId)
        {
            var media = SonosRadio.ForStation(tuneInId, "http://ignored.example/live", "Ö3");

            Assert.Equal("x-sonosapi-stream:s8007?sid=254&flags=32", media.Uri);
            Assert.Contains("<item id=\"F00092020s8007\"", media.Metadata);
            Assert.Contains("<dc:title>Ö3</dc:title>", media.Metadata);
            Assert.Contains("SA_RINCON65031_", media.Metadata);
        }

        [Fact]
        public void HttpStream_PlaysAsSonosRadio()
        {
            var media = SonosRadio.ForStation(null, "http://orf-live.ors-shoutcast.at/oe3-q2a", "Hitradio Ö3");

            Assert.Equal("x-rincon-mp3radio://orf-live.ors-shoutcast.at/oe3-q2a", media.Uri);
            Assert.Contains("<dc:title>Hitradio Ö3</dc:title>", media.Metadata);
            Assert.Contains("<upnp:class>object.item.audioItem.audioBroadcast</upnp:class>", media.Metadata);
        }

        [Theory]
        [InlineData("https://stream.example/live.mp3")]
        [InlineData("x-rincon-mp3radio://stream.example/live")]
        public void OtherUrls_ArePlayedAsTheyAre(string url)
        {
            Assert.Equal(url, SonosRadio.ForStation("", url, "Radio").Uri);
        }

        [Fact]
        public void TuneInUri_KeepsItsUriAndGetsTuneInMetadata()
        {
            var media = SonosRadio.ForStation(null, "x-sonosapi-stream:s24896?sid=254&flags=8224", null);

            Assert.Equal("x-sonosapi-stream:s24896?sid=254&flags=8224", media.Uri);
            Assert.Contains("F00092020s24896", media.Metadata);
            Assert.Contains("<dc:title>tunein</dc:title>", media.Metadata);
        }

        [Fact]
        public void Title_IsXmlEscaped_AndDefaultsToTheHost()
        {
            Assert.Contains("<dc:title>Rock &amp; Roll &lt;1&gt;</dc:title>", SonosRadio.ForStation(null, "https://a.example/x", "Rock & Roll <1>").Metadata);
            Assert.Contains("<dc:title>a.example</dc:title>", SonosRadio.ForStation(null, "https://a.example/x", " ").Metadata);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void NoStation_IsNull(string url)
        {
            Assert.Null(SonosRadio.ForStation(null, url, "x"));
            Assert.Null(SonosRadio.ForStation("not-an-id", url, "x"));
        }
    }
}
