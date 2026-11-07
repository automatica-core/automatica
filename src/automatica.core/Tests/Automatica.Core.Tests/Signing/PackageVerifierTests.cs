using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Automatica.Core.Common.Update.Signing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Automatica.Core.Tests.Signing
{
    [Collection(PackageSigningCollection.Name)]
    public class PackageVerifierTests : IDisposable
    {
        private readonly PackageSigningFixture _fixture;
        private readonly List<string> _cleanup = new();

        public PackageVerifierTests(PackageSigningFixture fixture)
        {
            _fixture = fixture;
            PackageSigningFixture.ResetPolicy();
        }

        public void Dispose()
        {
            foreach (var path in _cleanup)
            {
                if (File.Exists(path)) File.Delete(path);
                if (Directory.Exists(path)) Directory.Delete(path, true);
            }
        }

        private string SignedPlugin(ECDsa key = null, string name = "TestDriver", string version = "1.2.0.0")
        {
            var zip = PackageSigningFixture.CreatePluginZip(name);
            _cleanup.Add(zip);
            PackageSigner.Sign(zip, key ?? _fixture.TrustedKey, PackageSignatureFormat.TypePlugin, name, version, null);
            return zip;
        }

        [Fact]
        public void PinnedKeys_AreValidP256Keys()
        {
            var keys = TrustedPackageKeys.Load(TrustedPackageKeys.PublicKeys);
            Assert.NotEmpty(keys);
            Assert.All(keys, a => Assert.Equal(256, a.KeySize));
        }

        [Fact]
        public void SignedPackage_IsValid()
        {
            var zip = SignedPlugin();

            var result = PackageVerifier.VerifyZip(zip, PackageSignatureFormat.TypePlugin, "TestDriver");

            Assert.Equal(PackageVerificationStatus.Valid, result.Status);
            Assert.Equal("1.2.0.0", result.Signature.Version);
            Assert.Equal(3, result.Signature.Files.Count);
            Assert.Equal(PackageSignatureFormat.GetKeyId(_fixture.TrustedKey), result.Signature.KeyId);
        }

        [Fact]
        public void UnsignedPackage_IsRejected_UnlessAllowed()
        {
            var zip = PackageSigningFixture.CreatePluginZip();
            _cleanup.Add(zip);

            var result = PackageVerifier.VerifyZip(zip, PackageSignatureFormat.TypePlugin);
            Assert.Equal(PackageVerificationStatus.Unsigned, result.Status);
            Assert.Throws<PackageVerificationException>(() => PackageVerifier.EnsureTrusted(result, "test", NullLogger.Instance));

            PackageSecurityPolicy.AllowUnsignedOverride = true;
            Assert.Null(PackageVerifier.EnsureTrusted(result, "test", NullLogger.Instance));
        }

        [Fact]
        public void UntrustedKey_IsRejected_EvenIfUnsignedIsAllowed()
        {
            PackageSecurityPolicy.AllowUnsignedOverride = true;
            var zip = SignedPlugin(_fixture.UntrustedKey);

            var result = PackageVerifier.VerifyZip(zip, PackageSignatureFormat.TypePlugin);

            Assert.Equal(PackageVerificationStatus.Invalid, result.Status);
            Assert.Throws<PackageVerificationException>(() => PackageVerifier.EnsureTrusted(result, "test", NullLogger.Instance));
        }

        [Fact]
        public void ModifiedEntry_IsRejected()
        {
            var zip = SignedPlugin();
            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Update))
            {
                archive.GetEntry("TestDriver/TestDriver.dll")!.Delete();
                using var writer = new StreamWriter(archive.CreateEntry("TestDriver/TestDriver.dll").Open());
                writer.Write("evil binary");
            }

            var result = PackageVerifier.VerifyZip(zip, PackageSignatureFormat.TypePlugin);

            Assert.Equal(PackageVerificationStatus.Invalid, result.Status);
            Assert.Contains("Hash mismatch", result.Error);
        }

        [Theory]
        [InlineData("TestDriver/Evil.dll")]
        [InlineData("Evil.dll")]
        public void AddedEntry_IsRejected(string entry)
        {
            var zip = SignedPlugin();
            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Update))
            {
                using var writer = new StreamWriter(archive.CreateEntry(entry).Open());
                writer.Write("evil binary");
            }

            var result = PackageVerifier.VerifyZip(zip, PackageSignatureFormat.TypePlugin);

            Assert.Equal(PackageVerificationStatus.Invalid, result.Status);
            Assert.Contains("not signed", result.Error);
        }

        [Fact]
        public void DuplicateEntry_IsRejected()
        {
            var zip = SignedPlugin();
            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Update))
            {
                // same name twice: ExtractToDirectory(overwrite) would write the second one
                using var writer = new StreamWriter(archive.CreateEntry("TestDriver/TestDriver.dll").Open());
                writer.Write("evil binary");
            }

            var result = PackageVerifier.VerifyZip(zip, PackageSignatureFormat.TypePlugin);

            Assert.Equal(PackageVerificationStatus.Invalid, result.Status);
        }

        [Fact]
        public void RemovedEntry_IsRejected()
        {
            var zip = SignedPlugin();
            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Update))
            {
                archive.GetEntry("TestDriver/Dependency.dll")!.Delete();
            }

            var result = PackageVerifier.VerifyZip(zip, PackageSignatureFormat.TypePlugin);

            Assert.Equal(PackageVerificationStatus.Invalid, result.Status);
            Assert.Contains("missing", result.Error);
        }

        [Fact]
        public void ModifiedSignedVersion_IsRejected()
        {
            var zip = SignedPlugin(version: "1.0.0.0");
            ReplaceSignature(zip, s => s.Version = "9.9.9.9");

            var result = PackageVerifier.VerifyZip(zip, PackageSignatureFormat.TypePlugin);

            Assert.Equal(PackageVerificationStatus.Invalid, result.Status);
        }

        [Fact]
        public void WrongTypeOrName_IsRejected()
        {
            var zip = SignedPlugin();

            Assert.Equal(PackageVerificationStatus.Invalid, PackageVerifier.VerifyZip(zip, PackageSignatureFormat.TypeUpdate).Status);
            Assert.Equal(PackageVerificationStatus.Invalid, PackageVerifier.VerifyZip(zip, PackageSignatureFormat.TypePlugin, "OtherDriver").Status);
        }

        [Fact]
        public void PathTraversalInSignature_IsRejected()
        {
            var zip = SignedPlugin();
            ReplaceSignature(zip, s => s.Files.Add(new PackageSignatureFile { Path = "../evil.dll", Sha256 = new string('0', 64) }));

            var result = PackageVerifier.VerifyZip(zip, PackageSignatureFormat.TypePlugin);

            Assert.Equal(PackageVerificationStatus.Invalid, result.Status);
        }

        [Fact]
        public void Downgrade_IsRejected_UnlessAllowed()
        {
            var signature = new PackageSignature { Version = "1.0.0.0" };

            PackageVerifier.EnsureNoDowngrade(signature, "1.0.0.0", "test", NullLogger.Instance);
            PackageVerifier.EnsureNoDowngrade(signature, "0.9.0.0", "test", NullLogger.Instance);
            PackageVerifier.EnsureNoDowngrade(signature, null, "test", NullLogger.Instance);
            Assert.Throws<PackageVerificationException>(() => PackageVerifier.EnsureNoDowngrade(signature, "1.0.0.1", "test", NullLogger.Instance));

            PackageSecurityPolicy.AllowDowngradeOverride = true;
            PackageVerifier.EnsureNoDowngrade(signature, "1.0.0.1", "test", NullLogger.Instance);
        }

        [Fact]
        public void ExtractedDirectory_IsVerified()
        {
            var zip = SignedPlugin();
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            _cleanup.Add(dir);
            ZipFile.ExtractToDirectory(zip, dir);

            Assert.Equal(PackageVerificationStatus.Valid,
                PackageVerifier.VerifyDirectory(dir, PackageSignatureFormat.TypePlugin, "TestDriver", "", true, true).Status);

            // installed plugin layout: the component directory with the signature file inside
            var componentDir = Path.Combine(dir, "TestDriver");
            File.Copy(Path.Combine(dir, PackageSignatureFormat.SignatureFileName), Path.Combine(componentDir, PackageSignatureFormat.SignatureFileName));
            Assert.Equal(PackageVerificationStatus.Valid,
                PackageVerifier.VerifyDirectory(componentDir, PackageSignatureFormat.TypePlugin, null, null, false, true).Status);

            File.WriteAllText(Path.Combine(componentDir, "Planted.dll"), "evil");
            Assert.Equal(PackageVerificationStatus.Invalid,
                PackageVerifier.VerifyDirectory(componentDir, PackageSignatureFormat.TypePlugin, null, null, false, true).Status);

            File.Delete(Path.Combine(componentDir, "Planted.dll"));
            File.WriteAllText(Path.Combine(componentDir, "TestDriver.dll"), "evil");
            Assert.Equal(PackageVerificationStatus.Invalid,
                PackageVerifier.VerifyDirectory(componentDir, PackageSignatureFormat.TypePlugin, null, null, false, true).Status);
        }

        [Fact]
        public void Payload_IsCanonical()
        {
            var files = new[]
            {
                new PackageSignatureFile { Path = "b/x.dll", Sha256 = new string('B', 64) },
                new PackageSignatureFile { Path = "a.dll", Sha256 = new string('a', 64) }
            };

            var payload = Encoding.UTF8.GetString(PackageSignatureFormat.BuildPayload("plugin", "Name", "1.0", null, files));

            Assert.Equal("AUTOMATICA-PACKAGE-SIGNATURE-V1\ntype=plugin\nname=Name\nversion=1.0\nrid=\nfiles=2\n" +
                         new string('a', 64) + " a.dll\n" + new string('b', 64) + " b/x.dll\n", payload);
        }

        [Fact]
        public void CompressionBomb_IsRejected()
        {
            var zip = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".zip");
            _cleanup.Add(zip);
            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            {
                using var stream = archive.CreateEntry("bomb.bin", CompressionLevel.SmallestSize).Open();
                var zeros = new byte[1024 * 1024];
                for (var i = 0; i < 50; i++)
                {
                    stream.Write(zeros);
                }
            }

            var result = PackageVerifier.VerifyZip(zip, PackageSignatureFormat.TypePlugin);

            Assert.Equal(PackageVerificationStatus.Invalid, result.Status);
        }

        private static void ReplaceSignature(string zip, Action<PackageSignature> change)
        {
            using var archive = ZipFile.Open(zip, ZipArchiveMode.Update);
            var entry = archive.GetEntry(PackageSignatureFormat.SignatureFileName)!;
            PackageSignature signature;
            using (var reader = new StreamReader(entry.Open()))
            {
                signature = Newtonsoft.Json.JsonConvert.DeserializeObject<PackageSignature>(reader.ReadToEnd());
            }
            entry.Delete();
            change(signature);
            using var writer = new StreamWriter(archive.CreateEntry(PackageSignatureFormat.SignatureFileName).Open());
            writer.Write(Newtonsoft.Json.JsonConvert.SerializeObject(signature));
        }
    }
}
