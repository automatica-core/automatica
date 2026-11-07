using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Automatica.Core.Satellite.Tests.Infrastructure;
using Xunit;

namespace Automatica.Core.Satellite.Tests
{
    [Collection(SatelliteStaticsCollection.Name)]
    public sealed class SatelliteSecretsTests : IDisposable
    {
        private readonly string _dir;

        public SatelliteSecretsTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "automatica-satellite-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            SatelliteSecrets.ConfigDirectoryOverride = _dir;
        }

        public void Dispose()
        {
            SatelliteSecrets.ConfigDirectoryOverride = null;
            Directory.Delete(_dir, true);
        }

        private string KeyPath => Path.Combine(_dir, SatelliteSecrets.JwtKeyFileName);
        private string CertPath => Path.Combine(_dir, SatelliteSecrets.CertificateFileName);

        [Fact]
        public void ConfigDirectory_UsesTheOverride()
        {
            Assert.Equal(_dir, SatelliteSecrets.ConfigDirectory);
        }

        [Fact]
        public void GetJwtKey_FirstCall_Creates32RandomBytes()
        {
            var key = SatelliteSecrets.GetJwtKey();

            Assert.Equal(32, key.Length);
            Assert.Equal(key, File.ReadAllBytes(KeyPath));
            Assert.Contains(key, b => b != 0);
        }

        [Fact]
        public void GetJwtKey_SecondCall_ReturnsTheStoredKey()
        {
            var first = SatelliteSecrets.GetJwtKey();

            Assert.Equal(first, SatelliteSecrets.GetJwtKey());
        }

        [Fact]
        public void GetJwtKey_ExistingLongKey_IsUsedAsIs()
        {
            var existing = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray();
            File.WriteAllBytes(KeyPath, existing);

            Assert.Equal(existing, SatelliteSecrets.GetJwtKey());
        }

        [Fact]
        public void GetJwtKey_TooShortKeyFile_IsReplaced()
        {
            File.WriteAllBytes(KeyPath, new byte[] { 1, 2, 3 });

            var key = SatelliteSecrets.GetJwtKey();

            Assert.Equal(32, key.Length);
            Assert.Equal(key, File.ReadAllBytes(KeyPath));
        }

        [Fact]
        public void GetJwtKey_IsOnlyReadableByTheOwnerOnUnix()
        {
            SatelliteSecrets.GetJwtKey();

            if (!OperatingSystem.IsWindows())
            {
                Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(KeyPath));
            }
        }

        [Fact]
        public void GetCertificate_CreatesASelfSignedServerCertificateAndReusesIt()
        {
            using var first = SatelliteSecrets.GetCertificate();
            using var second = SatelliteSecrets.GetCertificate();

            Assert.True(File.Exists(CertPath));
            Assert.True(first.HasPrivateKey);
            Assert.Equal(first.Thumbprint, second.Thumbprint);
            Assert.Equal("CN=automatica-satellite", first.Subject);

            var basic = first.Extensions.OfType<X509BasicConstraintsExtension>().Single();
            Assert.False(basic.CertificateAuthority);
            var eku = first.Extensions.OfType<X509EnhancedKeyUsageExtension>().Single();
            Assert.Contains(eku.EnhancedKeyUsages.Cast<System.Security.Cryptography.Oid>(), o => o.Value == "1.3.6.1.5.5.7.3.1");
            var san = first.Extensions.OfType<X509SubjectAlternativeNameExtension>().Single();
            Assert.Contains("localhost", san.EnumerateDnsNames());
            Assert.Contains(System.Net.IPAddress.Loopback, san.EnumerateIPAddresses());
        }

        [Fact]
        public void GetCertificate_CorruptFile_IsReplacedWithANewCertificate()
        {
            File.WriteAllBytes(CertPath, new byte[] { 1, 2, 3, 4 });

            using var cert = SatelliteSecrets.GetCertificate();

            Assert.True(cert.HasPrivateKey);
            using var reloaded = X509CertificateLoader.LoadPkcs12FromFile(CertPath, null);
            Assert.Equal(cert.Thumbprint, reloaded.Thumbprint);
        }
    }
}
