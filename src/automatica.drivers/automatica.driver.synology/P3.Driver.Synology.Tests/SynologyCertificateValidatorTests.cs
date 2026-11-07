using System;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using P3.Driver.Synology.DriverFactory;
using Xunit;

namespace P3.Driver.Synology.Tests
{
    public class SynologyCertificateValidatorTests
    {
        private static X509Certificate2 CreateSelfSigned()
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=diskstation", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        }

        [Fact]
        public void ValidCertificateIsAccepted()
        {
            using var cert = CreateSelfSigned();
            Assert.True(SynologyCertificateValidator.Validate(cert, SslPolicyErrors.None, false, null));
        }

        [Fact]
        public void InvalidCertificateIsRejectedUnlessIgnored()
        {
            using var cert = CreateSelfSigned();
            Assert.False(SynologyCertificateValidator.Validate(cert, SslPolicyErrors.RemoteCertificateChainErrors, false, null));
            Assert.True(SynologyCertificateValidator.Validate(cert, SslPolicyErrors.RemoteCertificateChainErrors, true, ""));
        }

        [Fact]
        public void PinnedSelfSignedCertificateIsAccepted()
        {
            using var cert = CreateSelfSigned();
            var sha256 = cert.GetCertHashString(HashAlgorithmName.SHA256);
            var withColons = string.Join(":", System.Text.RegularExpressions.Regex.Matches(sha256, "..")).ToLowerInvariant();

            Assert.True(SynologyCertificateValidator.Validate(cert, SslPolicyErrors.RemoteCertificateChainErrors, false, withColons));
            Assert.True(SynologyCertificateValidator.Validate(cert, SslPolicyErrors.RemoteCertificateChainErrors, false, cert.Thumbprint));
        }

        [Fact]
        public void PinMismatchIsRejectedEvenIfErrorsAreIgnored()
        {
            using var cert = CreateSelfSigned();
            using var other = CreateSelfSigned();

            Assert.False(SynologyCertificateValidator.Validate(cert, SslPolicyErrors.None, true, other.GetCertHashString(HashAlgorithmName.SHA256)));
        }
    }
}
