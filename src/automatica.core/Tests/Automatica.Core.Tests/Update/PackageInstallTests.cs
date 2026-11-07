using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Automatica.Core.Common.Update;
using Automatica.Core.Common.Update.Signing;
using Automatica.Core.Tests.Signing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Automatica.Core.Tests.Update
{
    /// <summary>
    /// Plugin and update installation on real zip files: nothing is extracted before the signature is verified
    /// </summary>
    [Collection(PackageSigningCollection.Name)]
    public sealed class PackageInstallTests : IDisposable
    {
        private readonly PackageSigningFixture _fixture;
        private readonly List<string> _cleanup = new();
        private readonly string _target;

        public PackageInstallTests(PackageSigningFixture fixture)
        {
            _fixture = fixture;
            PackageSigningFixture.ResetPolicy();
            _target = Path.Combine(Path.GetTempPath(), "automatica-install-" + Guid.NewGuid().ToString("N"));
            _cleanup.Add(_target);
        }

        public void Dispose()
        {
            PackageSigningFixture.ResetPolicy();
            foreach (var path in _cleanup)
            {
                if (File.Exists(path)) File.Delete(path);
                if (Directory.Exists(path)) Directory.Delete(path, true);
            }
        }

        private string Plugin(string name = "TestDriver", string version = "1.2.0.0", bool sign = true, System.Security.Cryptography.ECDsa key = null)
        {
            var zip = PackageSigningFixture.CreatePluginZip(name);
            _cleanup.Add(zip);
            if (sign)
            {
                PackageSigner.Sign(zip, key ?? _fixture.TrustedKey, PackageSignatureFormat.TypePlugin, name, version, null);
            }
            return zip;
        }

        private string UpdatePackage(string version)
        {
            var zip = PackageSigningFixture.CreateZip(new Dictionary<string, string>
            {
                { "automatica-update.manifest", "{}" },
                { "Automatica.Core.dll", "core binary" }
            });
            _cleanup.Add(zip);
            PackageSigner.Sign(zip, _fixture.TrustedKey, PackageSignatureFormat.TypeUpdate, PackageSignatureFormat.UpdateName, version, null);
            return zip;
        }

        private bool TargetIsEmpty => !Directory.Exists(_target) || Directory.GetFileSystemEntries(_target).Length == 0;

        [Fact]
        public void InstallPlugin_Signed_ExtractsTheFiles_WithoutTheManifest()
        {
            var signature = Automatica.Core.Common.Update.Plugin.InstallPlugin(NullLogger.Instance, Plugin(), _target, "TestDriver");

            Assert.Equal("TestDriver", signature.Name);
            Assert.Equal("1.2.0.0", signature.Version);
            Assert.True(File.Exists(Path.Combine(_target, "TestDriver", "TestDriver.dll")));
            Assert.True(File.Exists(Path.Combine(_target, "TestDriver", "Dependency.dll")));
            Assert.False(File.Exists(Path.Combine(_target, Automatica.Core.Common.Update.Plugin.ManifestFileName)));
        }

        [Fact]
        public void InstallPlugin_Unsigned_IsRejected_BeforeAnythingIsExtracted()
        {
            Assert.Throws<PackageVerificationException>(() =>
                Automatica.Core.Common.Update.Plugin.InstallPlugin(NullLogger.Instance, Plugin(sign: false), _target, "TestDriver"));

            Assert.True(TargetIsEmpty);
        }

        [Fact]
        public void InstallPlugin_Unsigned_IsInstalledWhenThePolicyAllowsIt()
        {
            PackageSecurityPolicy.AllowUnsignedOverride = true;

            var signature = Automatica.Core.Common.Update.Plugin.InstallPlugin(NullLogger.Instance, Plugin(sign: false), _target, "TestDriver");

            Assert.Null(signature);
            Assert.True(File.Exists(Path.Combine(_target, "TestDriver", "TestDriver.dll")));
        }

        [Fact]
        public void InstallPlugin_SignedForAnotherComponent_IsRejected()
        {
            var zip = Plugin(name: "TestDriver");

            Assert.Throws<PackageVerificationException>(() =>
                Automatica.Core.Common.Update.Plugin.InstallPlugin(NullLogger.Instance, zip, _target, "OtherDriver"));
            Assert.True(TargetIsEmpty);
        }

        [Fact]
        public void InstallPlugin_SignedWithAnUntrustedKey_IsRejected()
        {
            PackageSecurityPolicy.AllowUnsignedOverride = true;

            Assert.Throws<PackageVerificationException>(() =>
                Automatica.Core.Common.Update.Plugin.InstallPlugin(NullLogger.Instance, Plugin(key: _fixture.UntrustedKey), _target, "TestDriver"));
            Assert.True(TargetIsEmpty);
        }

        [Fact]
        public void InstallPlugin_TamperedAfterSigning_IsRejected()
        {
            var zip = Plugin();
            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Update))
            {
                archive.GetEntry("TestDriver/TestDriver.dll")!.Delete();
                using var writer = new StreamWriter(archive.CreateEntry("TestDriver/TestDriver.dll").Open());
                writer.Write("evil binary");
            }

            Assert.Throws<PackageVerificationException>(() =>
                Automatica.Core.Common.Update.Plugin.InstallPlugin(NullLogger.Instance, zip, _target, "TestDriver"));
            Assert.True(TargetIsEmpty);
        }

        [Fact]
        public void InstallPlugin_AnyComponentName_AcceptsEverySignedName()
        {
            var signature = Automatica.Core.Common.Update.Plugin.VerifyPluginPackage(NullLogger.Instance, Plugin(name: "SomeDriver"), null);

            Assert.Equal("SomeDriver", signature.Name);
        }

        [Theory]
        [InlineData("10.0.0.5", "10.0.0.5")]
        [InlineData("10.0.1.0", "10.0.0.5")]
        [InlineData("10.0.0.5", null)]
        [InlineData("10.0.0.5", "")]
        public void VerifyUpdatePackage_SameOrNewerVersion_IsAccepted(string signed, string installed)
        {
            var signature = Automatica.Core.Common.Update.Update.VerifyUpdatePackage(NullLogger.Instance, UpdatePackage(signed), installed);

            Assert.Equal(signed, signature.Version);
            Assert.Equal(PackageSignatureFormat.UpdateName, signature.Name);
        }

        [Fact]
        public void VerifyUpdatePackage_Downgrade_IsRejected_UnlessAllowed()
        {
            var zip = UpdatePackage("9.9.0.0");

            Assert.Throws<PackageVerificationException>(() => Automatica.Core.Common.Update.Update.VerifyUpdatePackage(NullLogger.Instance, zip, "10.0.0.0"));

            PackageSecurityPolicy.AllowDowngradeOverride = true;
            Assert.NotNull(Automatica.Core.Common.Update.Update.VerifyUpdatePackage(NullLogger.Instance, zip, "10.0.0.0"));
        }

        [Fact]
        public void VerifyUpdatePackage_APluginPackage_IsNotAnUpdate()
        {
            var plugin = Plugin(name: PackageSignatureFormat.UpdateName);

            Assert.Throws<PackageVerificationException>(() => Automatica.Core.Common.Update.Update.VerifyUpdatePackage(NullLogger.Instance, plugin, null));
        }

        [Fact]
        public void VerifyPluginPackage_AnUpdatePackage_IsNotAPlugin()
        {
            var update = UpdatePackage("10.0.0.5");

            Assert.Throws<PackageVerificationException>(() =>
                Automatica.Core.Common.Update.Plugin.VerifyPluginPackage(NullLogger.Instance, update, PackageSignatureFormat.UpdateName));
        }

        [Fact]
        public void GetUpdateManifest_Unsigned_ReturnsNull_AndExtractsNothing()
        {
            var zip = PackageSigningFixture.CreateZip(new Dictionary<string, string> { { "automatica-update.manifest", "{}" } });
            _cleanup.Add(zip);
            Directory.CreateDirectory(_target);

            Assert.Null(Automatica.Core.Common.Update.Update.GetUpdateManifest(NullLogger.Instance, zip, _target));
            Assert.True(TargetIsEmpty);
        }

        // ---- Loader: installed plugin directories ----

        /// <summary>
        /// The installed layout: the component directory with the signature file inside
        /// </summary>
        private string InstalledPluginDirectory(bool signed = true)
        {
            var zip = Plugin(sign: signed);
            ZipFile.ExtractToDirectory(zip, _target);
            var componentDir = Path.Combine(_target, "TestDriver");
            if (signed)
            {
                File.Copy(Path.Combine(_target, PackageSignatureFormat.SignatureFileName), Path.Combine(componentDir, PackageSignatureFormat.SignatureFileName));
            }
            return componentDir;
        }

        [Fact]
        public void Loader_SignedPluginDirectory_IsTrusted()
        {
            Assert.True(Automatica.Core.Runtime.Core.Plugins.Loader.IsFolderTrusted(InstalledPluginDirectory(), NullLogger.Instance));
        }

        [Fact]
        public void Loader_PlantedAssembly_MakesTheDirectoryUntrusted()
        {
            var dir = InstalledPluginDirectory();
            File.WriteAllText(Path.Combine(dir, "Planted.dll"), "evil");

            Assert.False(Automatica.Core.Runtime.Core.Plugins.Loader.IsFolderTrusted(dir, NullLogger.Instance));
        }

        [Fact]
        public void Loader_ModifiedAssembly_MakesTheDirectoryUntrusted()
        {
            var dir = InstalledPluginDirectory();
            File.WriteAllText(Path.Combine(dir, "Dependency.dll"), "evil");

            Assert.False(Automatica.Core.Runtime.Core.Plugins.Loader.IsFolderTrusted(dir, NullLogger.Instance));
        }

        [Fact]
        public void Loader_MissingSignedAssembly_MakesTheDirectoryUntrusted()
        {
            var dir = InstalledPluginDirectory();
            File.Delete(Path.Combine(dir, "Dependency.dll"));

            Assert.False(Automatica.Core.Runtime.Core.Plugins.Loader.IsFolderTrusted(dir, NullLogger.Instance));
        }

        [Fact]
        public void Loader_UnsignedLegacyDirectory_IsLoadedUnlessSignedPluginsAreRequired()
        {
            var dir = InstalledPluginDirectory(signed: false);

            Assert.True(Automatica.Core.Runtime.Core.Plugins.Loader.IsFolderTrusted(dir, NullLogger.Instance));

            PackageSecurityPolicy.RequireSignedPluginsOverride = true;
            Assert.False(Automatica.Core.Runtime.Core.Plugins.Loader.IsFolderTrusted(dir, NullLogger.Instance));
        }

        [Fact]
        public void Loader_CorruptSignatureFile_MakesTheDirectoryUntrusted()
        {
            var dir = InstalledPluginDirectory();
            File.WriteAllText(Path.Combine(dir, PackageSignatureFormat.SignatureFileName), "{ not a signature");

            Assert.False(Automatica.Core.Runtime.Core.Plugins.Loader.IsFolderTrusted(dir, NullLogger.Instance));
        }
    }
}
