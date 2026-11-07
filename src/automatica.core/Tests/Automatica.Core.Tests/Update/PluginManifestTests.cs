using Automatica.Core.Common.Update;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.Common.Update.Signing;
using Automatica.Core.Tests.Signing;
using Newtonsoft.Json;
using System.IO.Compression;
using Xunit;

namespace Automatica.Core.Tests.Update
{
    [Collection(PackageSigningCollection.Name)]
    public class PluginManifestTests
    {
        private readonly PackageSigningFixture _fixture;

        public PluginManifestTests(PackageSigningFixture fixture)
        {
            _fixture = fixture;
            PackageSigningFixture.ResetPolicy();
        }

        /// <summary>
        /// Signs the package like automatica-cli does (component name and plugin version from the manifest)
        /// </summary>
        private void Sign(string file)
        {
            PluginManifest manifest;
            using (var archive = ZipFile.OpenRead(file))
            using (var reader = new StreamReader(archive.GetEntry("automatica-manifest.json")!.Open()))
            {
                manifest = JsonConvert.DeserializeObject<PluginManifest>(reader.ReadToEnd());
            }
            PackageSigner.Sign(file, _fixture.TrustedKey, PackageSignatureFormat.TypePlugin, manifest.Automatica.ComponentName,
                manifest.Automatica.PluginVersion?.ToString() ?? "0.0.0.1", null);
        }

        private static async Task<string> CopyResource(string name)
        {
            var tmpFile = Path.GetTempFileName();
            await using var fileStream = File.Create(tmpFile);
            await Assembly.GetExecutingAssembly().GetManifestResourceStream(name)!.CopyToAsync(fileStream);
            return tmpFile;
        }

        [Fact]
        public async Task UnsignedPackage_IsRejected()
        {
            var tmpPath = Directory.CreateDirectory(Path.Combine(ServerInfo.GetTempPath(), Guid.NewGuid().ToString("N")));
            var tmpFile = await CopyResource("Automatica.Core.Tests.P3.Driver.Automatica.Remote-0.3.0.1.acpkg");

            // nothing is extracted before the signature is checked
            Assert.Null(Plugin.GetPluginManifest(NullLogger.Instance, tmpFile, tmpPath.FullName));
            Assert.Empty(Directory.GetFileSystemEntries(tmpPath.FullName));

            PackageSecurityPolicy.AllowUnsignedOverride = true;
            Assert.NotNull(Plugin.GetPluginManifest(NullLogger.Instance, tmpFile, tmpPath.FullName));

            File.Delete(tmpFile);
            Directory.Delete(tmpPath.FullName, true);
        }

        [Fact]
        public async Task SignedPackage_WithOtherComponentName_IsRejected()
        {
            var tmpPath = Directory.CreateDirectory(Path.Combine(ServerInfo.GetTempPath(), Guid.NewGuid().ToString("N")));
            var tmpFile = await CopyResource("Automatica.Core.Tests.P3.Driver.Automatica.Remote-0.3.0.1.acpkg");

            // a valid signature for another plugin name must not be accepted for this manifest
            PackageSigner.Sign(tmpFile, _fixture.TrustedKey, PackageSignatureFormat.TypePlugin, "Knx", "0.3.0.1", null);

            Assert.Null(Plugin.GetPluginManifest(NullLogger.Instance, tmpFile, tmpPath.FullName));

            File.Delete(tmpFile);
            Directory.Delete(tmpPath.FullName, true);
        }
        [Fact]
        public async Task TestManifestParsing()
        {
            var tmpPath = Directory.CreateDirectory(Path.Combine(ServerInfo.GetTempPath(), Guid.NewGuid().ToString().Replace("-", "")));
            var tmpFile = Path.GetTempFileName();
            using (var fileStream = new StreamWriter(tmpFile))
            {
                await Assembly.GetExecutingAssembly().GetManifestResourceStream("Automatica.Core.Tests.P3.Driver.Automatica.Remote-0.3.0.1.acpkg").CopyToAsync(fileStream.BaseStream);
            }
            Sign(tmpFile);

            var manifest = Plugin.GetPluginManifest(NullLogger.Instance, tmpFile, tmpPath.FullName);


            File.Delete(tmpFile);
            Directory.Delete(tmpPath.FullName, true);

            Assert.Equal("AutomaticaRemote", manifest.Automatica.ComponentName);
            Assert.Equal(manifest.Automatica.ManifestVersion, new Version(0, 0, 0, 1));
            Assert.Null(manifest.Automatica.MinCoreServerVersion);

            Assert.Equal("P3.Driver.Automatica.Remote", manifest.Automatica.Name);
            Assert.Equal("P3.Driver.Automatica.Remote.dll", manifest.Automatica.Output);
            Assert.Empty(manifest.Automatica.Dependencies);
            Assert.Empty(manifest.Automatica.Resources);
        }

        [Fact]
        public async Task TestManifestParsing2()
        {
            var tmpPath = Directory.CreateDirectory(Path.Combine(ServerInfo.GetTempPath(), Guid.NewGuid().ToString().Replace("-", "")));
            var tmpFile = Path.GetTempFileName();
            using (var fileStream = new StreamWriter(tmpFile))
            {
                await Assembly.GetExecutingAssembly().GetManifestResourceStream("Automatica.Core.Tests.P3.Driver.HueBridgeSimulator.DriverFactory-0.3.0.1.acpkg").CopyToAsync(fileStream.BaseStream);
            }
            Sign(tmpFile);

            var manifest = Plugin.GetPluginManifest(NullLogger.Instance, tmpFile, tmpPath.FullName);


            File.Delete(tmpFile);
            Directory.Delete(tmpPath.FullName, true);

            Assert.Equal("HueBridge", manifest.Automatica.ComponentName);
            Assert.Equal(manifest.Automatica.ManifestVersion, new Version(0, 0, 0, 1));
            Assert.Null(manifest.Automatica.MinCoreServerVersion);

            Assert.Equal("P3.Driver.HueBridgeSimulator.DriverFactory", manifest.Automatica.Name);
            Assert.Equal("P3.Driver.HueBridgeSimulator.DriverFactory.dll", manifest.Automatica.Output);
            Assert.Equal(2, manifest.Automatica.Dependencies.Count);

            Assert.Equal("P3.Driver.HueBridge.dll", manifest.Automatica.Dependencies[0]);
            Assert.Equal("Rssdp.dll", manifest.Automatica.Dependencies[1]);
            Assert.Empty(manifest.Automatica.Resources);
        }


        [Fact]
        public void TestManifestParsing3()
        {
            var manifestContent = "{\"Automatica\":{\"manifest-version\":{\"Major\":0,\"Minor\":1,\"Build\":0,\"Revision\":0,\"MajorRevision\":0,\"MinorRevision\":0},\"plugin-version\":{\"Major\":0,\"Minor\":1,\"Build\":0,\"Revision\":0,\"MajorRevision\":0,\"MinorRevision\":0},\"PluginGuid\":\"4326f436-3b3b-42fc-9fc5-d5301b2fe8d3\",\"Name\":\"P3.Driver.Automatica.Remote\",\"ComponentName\":\"AutomaticaRemote\",\"Type\":\"driver\",\"Output\":\"P3.Driver.Automatica.Remote.dll\",\"Resources\":[],\"Dependencies\":[],\"MinCoreServerVersion\":{\"Major\":0,\"Minor\":5,\"Build\":0,\"Revision\":0,\"MajorRevision\":0,\"MinorRevision\":0}}}";

            var manifest = JsonConvert.DeserializeObject<PluginManifest>(manifestContent);
            

            Assert.Equal(new Version(0,5,0,0), manifest.Automatica.MinCoreServerVersion);
        }

        [Fact]
        public async Task TestManifestParsingAndChecking()
        {
            var tmpPath = Directory.CreateDirectory(Path.Combine(ServerInfo.GetTempPath(), Guid.NewGuid().ToString().Replace("-", "")));
            var tmpFile = Path.GetTempFileName();
            using (var fileStream = new StreamWriter(tmpFile))
            {
                await Assembly.GetExecutingAssembly().GetManifestResourceStream("Automatica.Core.Tests.P3.Driver.HueBridgeSimulator.DriverFactory-0.3.0.1.acpkg").CopyToAsync(fileStream.BaseStream);
            }
            Sign(tmpFile);

            var manifest = Plugin.CheckPluginFile(NullLogger.Instance, tmpFile, false);
            Assert.True(manifest);
        }
    }
}
