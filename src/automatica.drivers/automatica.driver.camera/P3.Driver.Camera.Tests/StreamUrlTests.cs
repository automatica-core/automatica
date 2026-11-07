using P3.Driver.Camera.Driver;
using Xunit;

namespace P3.Driver.Camera.Tests
{
    public class StreamUrlTests
    {
        [Fact]
        public void WithCredentials_AddsEscapedUserAndPassword()
        {
            Assert.Equal("rtsp://admin:p%40ss%3Aw%2Frd@192.168.1.20:554/Streaming/Channels/101?transportmode=unicast&profile=Profile_1",
                StreamUrl.WithCredentials("rtsp://192.168.1.20:554/Streaming/Channels/101?transportmode=unicast&profile=Profile_1", "admin", "p@ss:w/rd"));
        }

        [Fact]
        public void WithCredentials_KeepsCredentialsOfTheUrl()
        {
            Assert.Equal("rtsp://other:x@cam/stream", StreamUrl.WithCredentials("rtsp://other:x@cam/stream", "admin", "secret"));
        }

        [Fact]
        public void WithCredentials_NoUser_LeavesTheUrl()
        {
            Assert.Equal("rtsp://cam/stream", StreamUrl.WithCredentials(" rtsp://cam/stream ", "", "secret"));
            Assert.Null(StreamUrl.WithCredentials("  ", "admin", "secret"));
        }

        [Fact]
        public void WithCredentials_UserWithoutPassword()
        {
            Assert.Equal("http://viewer@cam/snap.jpg", StreamUrl.WithCredentials("http://cam/snap.jpg", "viewer", null));
        }

        [Fact]
        public void WithCredentials_DoesNotReescapeThePath()
        {
            // Uri would turn %2F into / or escape the braces, cameras need them verbatim
            Assert.Equal("rtsp://a:b@cam/live%2Fch0{1}", StreamUrl.WithCredentials("rtsp://cam/live%2Fch0{1}", "a", "b"));
        }

        [Fact]
        public void ToGo2RtcSource_RtspWithoutOptions_DisablesTheBackchannel()
        {
            Assert.Equal("rtsp://cam/stream#backchannel=0", StreamUrl.ToGo2RtcSource("rtsp://cam/stream"));
            Assert.Equal("rtsps://cam/stream#backchannel=0", StreamUrl.ToGo2RtcSource("rtsps://cam/stream"));
        }

        [Fact]
        public void ToGo2RtcSource_KeepsOwnOptionsAndOtherSchemes()
        {
            Assert.Equal("rtsp://cam/stream#timeout=30", StreamUrl.ToGo2RtcSource("rtsp://cam/stream#timeout=30"));
            Assert.Equal("http://cam/video.mjpg", StreamUrl.ToGo2RtcSource("http://cam/video.mjpg"));
            Assert.Null(StreamUrl.ToGo2RtcSource(null));
        }

        [Fact]
        public void Redact_HidesTheCredentials()
        {
            Assert.Equal("rtsp://***@cam:554/s", StreamUrl.Redact("rtsp://admin:secret@cam:554/s"));
            Assert.Equal("rtsp://cam/s", StreamUrl.Redact("rtsp://cam/s"));
        }

        [Theory]
        [InlineData("rtsp://admin:x@192.168.1.20:8554/s", "192.168.1.20", 8554)]
        [InlineData("rtsp://192.168.1.20/s", "192.168.1.20", 554)]
        [InlineData("rtsps://cam.local/s", "cam.local", 322)]
        [InlineData("http://cam/snap.jpg", "cam", 80)]
        [InlineData("rtsp://[fe80::1]:555/s", "fe80::1", 555)]
        [InlineData("rtsp://cam?x=1", "cam", 554)]
        public void TryGetEndpoint_ParsesHostAndPort(string url, string host, int port)
        {
            Assert.True(StreamUrl.TryGetEndpoint(url, 1, out var h, out var p));
            Assert.Equal(host, h);
            Assert.Equal(port, p);
        }

        [Fact]
        public void TryGetEndpoint_Garbage_False()
        {
            Assert.False(StreamUrl.TryGetEndpoint("not a url", 554, out _, out _));
            Assert.False(StreamUrl.TryGetEndpoint("rtsp:///path", 554, out _, out _));
        }
    }
}
