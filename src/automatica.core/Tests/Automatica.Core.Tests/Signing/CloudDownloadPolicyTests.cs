using System;
using Automatica.Core.Internals.Cloud;
using Xunit;

namespace Automatica.Core.Tests.Signing
{
    public class CloudDownloadPolicyTests
    {
        private const string CloudUrl = "https://cloud.automaticacore.com/";

        [Theory]
        [InlineData("https://automaticacore.blob.core.windows.net/plugins/x.acpkg", true)]
        [InlineData("https://cloud.automaticacore.com/download/x.zip", true)]
        [InlineData("https://cloud-dev.automaticacore.com/download/x.zip", true)]
        [InlineData("http://automaticacore.blob.core.windows.net/plugins/x.acpkg", false)]
        [InlineData("https://evil.com/x.acpkg", false)]
        [InlineData("https://blob.core.windows.net.evil.com/x.acpkg", false)]
        [InlineData("https://evilautomaticacore.com/x.acpkg", false)]
        [InlineData("https://user:pw@automaticacore.blob.core.windows.net/x.acpkg", false)]
        [InlineData("file:///etc/passwd", false)]
        [InlineData("/relative/x.acpkg", false)]
        [InlineData(null, false)]
        public void DownloadUrls_AreAllowListed(string url, bool allowed)
        {
            Assert.Equal(allowed, CloudDownloadPolicy.IsAllowed(url, CloudUrl, out _));
        }

        [Fact]
        public void CustomCloudHost_IsAllowed()
        {
            Assert.True(CloudDownloadPolicy.IsAllowed("https://my-cloud.example.org/file.zip", "https://my-cloud.example.org/", out _));
        }

        [Fact]
        public void ApiKey_IsNeverSentInClearText()
        {
            Assert.Throws<InvalidOperationException>(() =>
                BaseCloudApi.BuildRequestUri("http://cloud.automaticacore.com/", "/webapi/v2/ping", "key", Guid.Empty));

            var local = BaseCloudApi.BuildRequestUri("http://localhost:5000/", "/webapi/v2/ping", "key", Guid.Empty);
            Assert.Equal("http://localhost:5000/webapi/v2/ping/key/00000000-0000-0000-0000-000000000000", local.ToString());

            var remote = BaseCloudApi.BuildRequestUri(CloudUrl, "/webapi/v2/coreServerData/ping", "key", Guid.Empty);
            Assert.Equal("https://cloud.automaticacore.com/webapi/v2/coreServerData/ping/key/00000000-0000-0000-0000-000000000000", remote.ToString());
        }
    }
}
