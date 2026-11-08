using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using P3.Driver.Http.Driver.Security;
using Xunit;

namespace P3.Driver.Http.Tests
{
    public class RequestGuardTests
    {
        [Theory]
        [InlineData("http://192.168.1.20/status")]
        [InlineData("https://api.example.com/v1?key=1")]
        [InlineData(" http://device.local:8080 ")]
        public void ValidateUrl_Accepts(string url)
        {
            Assert.NotNull(RequestGuard.ValidateUrl(url));
        }

        [Theory]
        [InlineData(null, "no URL")]
        [InlineData("", "no URL")]
        [InlineData("192.168.1.20/status", "not valid")]
        [InlineData("file:///etc/passwd", "only http")]
        [InlineData("ftp://host/file", "only http")]
        [InlineData("gopher://host:70/", "only http")]
        [InlineData("http://admin:secret@192.168.1.20/", "credentials")]
        public void ValidateUrl_Rejects(string url, string reason)
        {
            var ex = Assert.Throws<RequestBlockedException>(() => RequestGuard.ValidateUrl(url));
            Assert.Contains(reason, ex.Message);
            Assert.DoesNotContain("secret", ex.Message);
        }

        [Theory]
        [InlineData("http://10.0.0.5", "/relay/0?turn=on", "http://10.0.0.5/relay/0?turn=on")]
        [InlineData("http://10.0.0.5/", "relay/0", "http://10.0.0.5/relay/0")]
        [InlineData("http://10.0.0.5/api", "", "http://10.0.0.5/api")]
        [InlineData("http://10.0.0.5", "https://other.example.com/x", "https://other.example.com/x")]
        public void Combine(string address, string command, string expected)
        {
            Assert.Equal(new Uri(expected), RequestGuard.Combine(address, command));
        }

        [Theory]
        [InlineData("192.168.1.20", false, true)]
        [InlineData("10.1.2.3", false, true)]
        [InlineData("8.8.8.8", false, true)]
        [InlineData("fd00::1", false, true)]
        [InlineData("2001:db8::1", false, true)]
        [InlineData("127.0.0.1", false, false)]
        [InlineData("127.0.0.1", true, true)]
        [InlineData("127.8.9.10", false, false)]
        [InlineData("::1", false, false)]
        [InlineData("::1", true, true)]
        [InlineData("::ffff:127.0.0.1", false, false)]
        [InlineData("::ffff:169.254.169.254", true, false)]
        [InlineData("169.254.169.254", true, false)]
        [InlineData("0.0.0.0", true, false)]
        [InlineData("255.255.255.255", false, false)]
        [InlineData("224.0.0.251", false, false)]
        [InlineData("fe80::1", false, false)]
        [InlineData("ff02::1", false, false)]
        [InlineData("::", true, false)]
        [InlineData("64:ff9b::a9fe:a9fe", false, false)]
        [InlineData("64:ff9b::c0a8:0114", false, true)]
        public void IsAllowed(string address, bool allowLoopback, bool expected)
        {
            Assert.Equal(expected, RequestGuard.IsAllowed(IPAddress.Parse(address), allowLoopback));
        }

        [Fact]
        public async Task ResolveAllowed_HostResolvingToMetadataAddress_IsBlocked()
        {
            var ex = await Assert.ThrowsAsync<RequestBlockedException>(() => RequestGuard.ResolveAllowedAsync("rebind.example.com", true,
                (_, _) => Task.FromResult(new[] { IPAddress.Parse("169.254.169.254") }), CancellationToken.None));
            Assert.Contains("blocked address", ex.Message);
        }

        [Fact]
        public async Task ResolveAllowed_SkipsBlockedAddresses()
        {
            var allowed = await RequestGuard.ResolveAllowedAsync("dual.example.com", false,
                (_, _) => Task.FromResult(new[] { IPAddress.Loopback, IPAddress.Parse("192.168.1.9") }), CancellationToken.None);
            Assert.Equal(IPAddress.Parse("192.168.1.9"), Assert.Single(allowed));
        }

        [Fact]
        public async Task Handler_BlocksLoopback_UnlessAllowed()
        {
            await using var server = await LoopbackServer.StartAsync("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");

            using (var blocked = new HttpClient(RequestGuard.CreateHandler(() => false)))
            {
                var ex = await Assert.ThrowsAsync<HttpRequestException>(() => blocked.GetStringAsync(server.Url));
                Assert.IsType<RequestBlockedException>(ex.InnerException);
                Assert.Equal(0, server.Requests);
            }

            using (var allowed = new HttpClient(RequestGuard.CreateHandler(() => true)))
            {
                Assert.Equal("ok", await allowed.GetStringAsync(server.Url));
                Assert.Equal(1, server.Requests);
            }
        }

        [Fact]
        public async Task Handler_BlocksLocalhostByName()
        {
            await using var server = await LoopbackServer.StartAsync("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");
            using var client = new HttpClient(RequestGuard.CreateHandler(() => false));

            await Assert.ThrowsAsync<HttpRequestException>(() => client.GetStringAsync($"http://localhost:{server.Port}/"));
            Assert.Equal(0, server.Requests);
        }

        [Fact]
        public async Task Handler_DoesNotFollowRedirects()
        {
            await using var server = await LoopbackServer.StartAsync("HTTP/1.1 302 Found\r\nLocation: http://169.254.169.254/latest/meta-data/\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
            using var client = new HttpClient(RequestGuard.CreateHandler(() => true));

            using var response = await client.GetAsync(server.Url);
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Equal(1, server.Requests);
        }

        [Fact]
        public async Task ReadBody_TooLarge_IsBlocked()
        {
            using var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[RequestGuard.MaxResponseBytes + 1])
            };
            await Assert.ThrowsAsync<RequestBlockedException>(() => RequestGuard.ReadBodyAsync(response, CancellationToken.None));
        }

        [Fact]
        public async Task ReadBody_TooLargeWithoutLength_IsBlocked()
        {
            using var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new UnknownLengthStream(RequestGuard.MaxResponseBytes + 10))
            };
            await Assert.ThrowsAsync<RequestBlockedException>(() => RequestGuard.ReadBodyAsync(response, CancellationToken.None));
        }

        [Fact]
        public void ForLog_DropsQueryAndCredentials()
        {
            Assert.Equal("http://10.0.0.5/api/state", RequestGuard.ForLog(new Uri("http://user:pw@10.0.0.5/api/state?token=abc")));
        }

        private sealed class UnknownLengthStream : System.IO.Stream
        {
            private long _left;

            public UnknownLengthStream(long length) => _left = length;

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override long Seek(long offset, System.IO.SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            public override int Read(byte[] buffer, int offset, int count)
            {
                var n = (int)Math.Min(count, _left);
                Array.Fill(buffer, (byte)'a', offset, n);
                _left -= n;
                return n;
            }
        }
    }
}
