using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Automatica.Core.Plugin.Standalone.Tests.Infrastructure;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Automatica.Core.Plugin.Standalone.Tests
{
    public class BrokerCertificatePinTests
    {
        private const string Hex = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

        [Theory]
        [InlineData(Hex)]
        [InlineData("0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF")]
        [InlineData("01:23:45:67:89:ab:cd:ef:01:23:45:67:89:ab:cd:ef:01:23:45:67:89:ab:cd:ef:01:23:45:67:89:ab:cd:ef")]
        [InlineData(" 01 23 45 67 89 AB CD EF 01 23 45 67 89 AB CD EF 01 23 45 67 89 AB CD EF 01 23 45 67 89 AB CD EF ")]
        public void Normalize_CommonFormats_GiveLowercaseHex(string input)
        {
            Assert.Equal(Hex, BrokerCertificatePin.Normalize(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("abc")]
        [InlineData(Hex + "0")]
        [InlineData("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcde")]
        public void Normalize_WrongLength_IsNull(string input)
        {
            Assert.Null(BrokerCertificatePin.Normalize(input));
        }

        [Fact]
        public void Fingerprint_IsTheLowercaseSha256OfTheBytes()
        {
            var data = Encoding.UTF8.GetBytes("certificate bytes");

            Assert.Equal(Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant(), BrokerCertificatePin.Fingerprint(data));
            Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", BrokerCertificatePin.Fingerprint(Array.Empty<byte>()));
        }

        [Fact]
        public void Matches_PinnedLeafCertificate_IsAccepted()
        {
            using var cert = TestCertificates.CreateServerCertificate();

            Assert.True(BrokerCertificatePin.Matches(cert, null, TestCertificates.Fingerprint(cert), new ListLogger()));
        }

        [Fact]
        public void Matches_OtherCertificate_IsRejectedAndLogged()
        {
            using var cert = TestCertificates.CreateServerCertificate();
            using var other = TestCertificates.CreateServerCertificate("other");
            var logger = new ListLogger();

            Assert.False(BrokerCertificatePin.Matches(cert, null, TestCertificates.Fingerprint(other), logger));
            Assert.True(logger.Contains(LogLevel.Error, "does not match the pinned fingerprint"));
        }

        [Fact]
        public void Matches_PinnedCertificateInTheChain_IsAccepted()
        {
            using var leaf = TestCertificates.CreateServerCertificate("leaf");
            using var pinned = TestCertificates.CreateServerCertificate("pinned-ca");
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(pinned);
            chain.ChainPolicy.VerificationTime = new DateTime(2050, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            // a self-signed certificate builds a one-element chain of itself
            chain.Build(pinned);

            Assert.True(BrokerCertificatePin.Matches(leaf, chain, TestCertificates.Fingerprint(pinned), new ListLogger()));
        }

        [Fact]
        public void Matches_NoCertificateOrNoPin_IsRejected()
        {
            using var cert = TestCertificates.CreateServerCertificate();

            Assert.False(BrokerCertificatePin.Matches(null, null, Hex, new ListLogger()));
            Assert.False(BrokerCertificatePin.Matches(cert, null, null, new ListLogger()));
        }

        [Fact]
        public void Matches_UppercasePin_IsNotNormalizedHere()
        {
            // the connection normalizes the pin before it calls Matches
            using var cert = TestCertificates.CreateServerCertificate();

            Assert.False(BrokerCertificatePin.Matches(cert, null, TestCertificates.Fingerprint(cert).ToUpperInvariant(), new ListLogger()));
        }
    }
}
