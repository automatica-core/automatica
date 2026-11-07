using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Common.Update.Signing;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cloud;
using Automatica.Core.Internals.Core;
using Automatica.Core.Tests.Signing;
using Automatica.Push.Helper;
using Automatica.Push.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Automatica.Core.Tests.Update
{
    /// <summary>
    /// Every plugin that is not installed is reported to the plugin page (PluginFailed), it must never stay at "installing"
    /// </summary>
    [Collection(PackageSigningCollection.Name)]
    public class PluginDownloaderTests : IDisposable
    {
        private readonly PackageSigningFixture _fixture;
        private readonly List<string> _cleanup = new();
        private readonly List<(string Method, object[] Args)> _sent = new();
        private readonly Mock<IHubContext<UpdateHub>> _hub = new();
        private readonly Mock<ICloudApi> _api = new();
        private readonly Mock<ICoreServer> _coreServer = new();
        private readonly string _componentName = "TestDriver" + Guid.NewGuid().ToString("N");

        public PluginDownloaderTests(PackageSigningFixture fixture)
        {
            _fixture = fixture;
            PackageSigningFixture.ResetPolicy();

            var proxy = new Mock<IClientProxy>();
            proxy.Setup(a => a.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Callback<string, object[], CancellationToken>((method, args, _) => { lock (_sent) _sent.Add((method, (object[])args[0])); })
                .Returns(Task.CompletedTask);
            var clients = new Mock<IHubClients>();
            clients.Setup(a => a.Group(HubAccess.AdministratorsGroup)).Returns(proxy.Object);
            _hub.SetupGet(a => a.Clients).Returns(clients.Object);

            var reason = (string)null;
            _api.Setup(a => a.IsTrustedDownloadUrl(It.IsAny<string>(), out reason)).Returns(true);
        }

        public void Dispose()
        {
            foreach (var path in _cleanup.Where(File.Exists))
            {
                File.Delete(path);
            }
        }

        private Plugin CreatePlugin(string package) => new()
        {
            PluginGuid = Guid.NewGuid(),
            ComponentName = _componentName,
            AzureUrl = package == null ? "https://example.invalid/plugin.acpkg" : new Uri(package).AbsoluteUri,
            AzureFileName = _componentName + ".acpkg"
        };

        private string UnsignedPackage()
        {
            var zip = PackageSigningFixture.CreatePluginZip(_componentName);
            _cleanup.Add(zip);
            return zip;
        }

        private string SignedPackage()
        {
            var zip = UnsignedPackage();
            PackageSigner.Sign(zip, _fixture.TrustedKey, PackageSignatureFormat.TypePlugin, _componentName, "1.0.0.0", null);
            return zip;
        }

        private async Task<PluginDownloader> Download(Plugin plugin)
        {
            var downloader = new PluginDownloader(plugin, false, _api.Object, _hub.Object, _coreServer.Object, NullLogger.Instance);
            await downloader.Download();
            if (downloader.StagedFile != null)
            {
                _cleanup.Add(downloader.StagedFile);
            }
            return downloader;
        }

        private object[] Sent(string method) => _sent.Where(a => a.Method == method).Select(a => a.Args).SingleOrDefault();

        [Fact]
        public async Task UnsignedPackage_IsReportedAsFailed_AndNotInstalled()
        {
            var plugin = CreatePlugin(UnsignedPackage());

            var downloader = await Download(plugin);

            var failed = Sent("PluginFailed");
            Assert.NotNull(failed);
            Assert.Equal(plugin.PluginGuid, failed[0]);
            Assert.Contains("not signed", (string)failed[1]);
            Assert.Null(downloader.StagedFile);
            Assert.False(File.Exists(Path.Combine(Automatica.Core.Base.Common.ServerInfo.GetPluginUpdateDirectory(), plugin.AzureFileName)));
            _coreServer.Verify(a => a.UpdatePlugins(It.IsAny<IReadOnlyCollection<PluginUpdate>>()), Times.Never);
            Assert.Null(Sent("PluginLoaded"));
        }

        [Fact]
        public async Task UntrustedUrl_IsReportedAsFailed()
        {
            var reason = "not a trusted host";
            _api.Setup(a => a.IsTrustedDownloadUrl(It.IsAny<string>(), out reason)).Returns(false);
            var plugin = CreatePlugin(null);

            await Download(plugin);

            var failed = Sent("PluginFailed");
            Assert.Equal(plugin.PluginGuid, failed[0]);
            Assert.Contains(reason, (string)failed[1]);
        }

        [Fact]
        public async Task InvalidFileName_IsReportedAsFailed()
        {
            var plugin = CreatePlugin(null);
            plugin.AzureFileName = "../evil.acpkg";

            await Download(plugin);

            Assert.Equal(plugin.PluginGuid, Sent("PluginFailed")[0]);
        }

        [Fact]
        public async Task DownloadError_IsReportedAsFailed()
        {
            var plugin = CreatePlugin(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".missing"));

            await Download(plugin);

            Assert.Equal(plugin.PluginGuid, Sent("PluginFailed")[0]);
        }

        [Fact]
        public async Task FailedInstall_IsReportedAsFailed_NotAsLoaded()
        {
            var plugin = CreatePlugin(SignedPackage());
            _coreServer.Setup(a => a.UpdatePlugins(It.IsAny<IReadOnlyCollection<PluginUpdate>>()))
                .ReturnsAsync((IReadOnlyCollection<PluginUpdate> updates) => new PluginUpdateSummary(false, updates));

            await Download(plugin);

            Assert.NotNull(Sent("PluginFinished"));
            Assert.Equal(new object[] { plugin.PluginGuid, PluginDownloader.InstallFailedMessage }, Sent("PluginFailed"));
            Assert.Null(Sent("PluginLoaded"));
            _coreServer.Verify(a => a.Restart(), Times.Never);
        }

        [Fact]
        public async Task InstalledPlugin_IsReportedAsLoaded()
        {
            var plugin = CreatePlugin(SignedPackage());
            _coreServer.Setup(a => a.UpdatePlugins(It.IsAny<IReadOnlyCollection<PluginUpdate>>())).ReturnsAsync(PluginUpdateSummary.None);

            await Download(plugin);

            Assert.Equal(plugin.PluginGuid, Sent("PluginLoaded")[0]);
            Assert.Null(Sent("PluginFailed"));
        }
    }
}
