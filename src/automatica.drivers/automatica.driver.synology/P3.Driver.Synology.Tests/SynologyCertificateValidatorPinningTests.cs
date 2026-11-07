using System;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using P3.Driver.Synology.DriverFactory;
using Xunit;

namespace P3.Driver.Synology.Tests
{
    public class SynologyCertificateValidatorPinningTests
    {
        // RSA key generation is slow, the certificates are only read
        private static readonly X509Certificate2 SharedCertificate = TestCertificates.CreateSelfSigned();
        private static readonly X509Certificate2 SharedOther = TestCertificates.CreateSelfSigned("CN=other");

        private readonly X509Certificate2 _certificate = SharedCertificate;
        private readonly X509Certificate2 _other = SharedOther;

        private string Sha256 => _certificate.GetCertHashString(HashAlgorithmName.SHA256);
        private string Sha1 => _certificate.GetCertHashString(HashAlgorithmName.SHA1);

        #region without pin

        [Theory]
        [InlineData(SslPolicyErrors.RemoteCertificateNameMismatch)]
        [InlineData(SslPolicyErrors.RemoteCertificateChainErrors)]
        [InlineData(SslPolicyErrors.RemoteCertificateNotAvailable)]
        [InlineData(SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors)]
        public void Validate_NoPin_PolicyErrors_AreRejected(SslPolicyErrors errors)
        {
            Assert.False(SynologyCertificateValidator.Validate(_certificate, errors, false, null));
        }

        [Theory]
        [InlineData(SslPolicyErrors.RemoteCertificateNameMismatch)]
        [InlineData(SslPolicyErrors.RemoteCertificateChainErrors)]
        [InlineData(SslPolicyErrors.RemoteCertificateNotAvailable)]
        [InlineData(SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors)]
        public void Validate_NoPin_PolicyErrorsIgnored_AreAccepted(SslPolicyErrors errors)
        {
            Assert.True(SynologyCertificateValidator.Validate(_certificate, errors, true, null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Validate_NoPin_NoPolicyErrors_IsAccepted(bool ignoreSslErrors)
        {
            // a valid certificate must be accepted although errors are not ignored (b1e195d8)
            Assert.True(SynologyCertificateValidator.Validate(_certificate, SslPolicyErrors.None, ignoreSslErrors, null));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        public void Validate_EmptyPin_IsTreatedAsNoPin(string pin)
        {
            Assert.True(SynologyCertificateValidator.Validate(_certificate, SslPolicyErrors.None, false, pin));
            Assert.False(SynologyCertificateValidator.Validate(_certificate, SslPolicyErrors.RemoteCertificateChainErrors, false, pin));
        }

        [Theory]
        [InlineData(":::")]
        [InlineData("- -")]
        [InlineData("zz:yy")]
        public void Validate_PinWithoutHexDigits_IsTreatedAsNoPin(string pin)
        {
            Assert.False(SynologyCertificateValidator.Validate(_certificate, SslPolicyErrors.RemoteCertificateChainErrors, false, pin));
            Assert.True(SynologyCertificateValidator.Validate(_certificate, SslPolicyErrors.None, false, pin));
        }

        #endregion

        #region pin match

        [Fact]
        public void Validate_PinSha256Uppercase_MatchesSelfSignedCertificate()
        {
            Assert.True(SynologyCertificateValidator.Validate(_certificate, SslPolicyErrors.RemoteCertificateChainErrors, false, Sha256.ToUpperInvariant()));
        }

        [Fact]
        public void Validate_PinSha256Lowercase_Matches()
        {
            Assert.True(SynologyCertificateValidator.Validate(_certificate, SslPolicyErrors.RemoteCertificateChainErrors, false, Sha256.ToLowerInvariant()));
        }

        [Fact]
        public void Validate_PinSha256WithColonsMixedCase_Matches()
        {
            var pin = TestCertificates.WithColons(Sha256);
            pin = pin.Substring(0, 20).ToLowerInvariant() + pin.Substring(20);

            Assert.True(SynologyCertificateValidator.Validate(_certificate, SslPolicyErrors.RemoteCertificateChainErrors, false, pin));
        }

        [Theory]
        [InlineData(' ')]
        [InlineData('-')]
        [InlineData('.')]
        public void Validate_PinSha256WithOtherSeparatorsAndPadding_Matches(char separator)
        {
            var pin = "  " + TestCertificates.WithColons(Sha256).Replace(':', separator) + " ";

            Assert.True(SynologyCertificateValidator.Validate(_certificate, SslPolicyErrors.RemoteCertificateChainErrors, false, pin));
        }

        [Fact]
        public void Validate_PinSha1_Matches()
        {
            Assert.True(SynologyCertificateValidator.Validate(_certificate, SslPolicyErrors.RemoteCertificateChainErrors, false, Sha1));
        }

        [Fact]
        public void Validate_PinSha1WithColonsLowercase_Matches()
        {
            Assert.True(SynologyCertificateValidator.Validate(_certificate, SslPolicyErrors.RemoteCertificateChainErrors, false, TestCertificates.WithColons(Sha1).ToLowerInvariant()));
        }

        [Fact]
        public void Validate_PinMatch_AcceptsNameMismatchAndChainErrors()
        {
            var errors = SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors;

            Assert.True(SynologyCertificateValidator.Validate(_certificate, errors, false, Sha256));
        }

        [Fact]
        public void Validate_PinMatch_ValidCertificate_IsAccepted()
        {
            Assert.True(SynologyCertificateValidator.Validate(_certificate, SslPolicyErrors.None, false, Sha256));
        }

        #endregion

        #region pin mismatch

        [Fact]
        public void Validate_PinMismatch_ValidCertificate_IsRejected()
        {
            // a pin is stricter than the chain: a publicly valid but different certificate is not accepted
            Assert.False(SynologyCertificateValidator.Validate(_certificate, SslPolicyErrors.None, false, _other.GetCertHashString(HashAlgorithmName.SHA256)));
        }

        [Fact]
        public void Validate_PinMismatchSha1_IgnoreSslErrors_IsRejected()
        {
            Assert.False(SynologyCertificateValidator.Validate(_certificate, SslPolicyErrors.RemoteCertificateChainErrors, true, _other.GetCertHashString(HashAlgorithmName.SHA1)));
        }

        [Fact]
        public void Validate_PinTruncated_IsRejected()
        {
            Assert.False(SynologyCertificateValidator.Validate(_certificate, SslPolicyErrors.None, false, Sha256.Substring(0, Sha256.Length - 2)));
            Assert.False(SynologyCertificateValidator.Validate(_certificate, SslPolicyErrors.None, false, Sha256.Substring(0, 8)));
        }

        [Fact]
        public void Validate_PinWithExtraHexDigit_IsRejected()
        {
            Assert.False(SynologyCertificateValidator.Validate(_certificate, SslPolicyErrors.None, false, Sha256 + "0"));
            Assert.False(SynologyCertificateValidator.Validate(_certificate, SslPolicyErrors.None, false, Sha1 + "A"));
        }

        [Fact]
        public void Validate_PinOfAnotherHashAlgorithm_IsRejected()
        {
            Assert.False(SynologyCertificateValidator.Validate(_certificate, SslPolicyErrors.None, true, _certificate.GetCertHashString(HashAlgorithmName.SHA384)));
            Assert.False(SynologyCertificateValidator.Validate(_certificate, SslPolicyErrors.None, true, _certificate.GetCertHashString(HashAlgorithmName.SHA512)));
        }

        [Fact]
        public void Validate_PinOneDigitChanged_IsRejected()
        {
            var last = Sha256[Sha256.Length - 1];
            var changed = Sha256.Substring(0, Sha256.Length - 1) + (last == '0' ? '1' : '0');

            Assert.False(SynologyCertificateValidator.Validate(_certificate, SslPolicyErrors.None, true, changed));
        }

        [Fact]
        public void Validate_PinButNoCertificate_IsRejected()
        {
            Assert.False(SynologyCertificateValidator.Validate(null, SslPolicyErrors.None, true, Sha256));
            Assert.False(SynologyCertificateValidator.Validate(null, SslPolicyErrors.RemoteCertificateNotAvailable, true, Sha256));
        }

        #endregion

        #region normalize

        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("  ", null)]
        [InlineData("ab:cd:EF:01", "ABCDEF01")]
        [InlineData(" ab cd-ef ", "ABCDEF")]
        [InlineData("AB_CD", "ABCD")]
        [InlineData("::", "")]
        public void Normalize_StripsSeparatorsAndUppercases(string input, string expected)
        {
            Assert.Equal(expected, SynologyCertificateValidator.Normalize(input));
        }

        #endregion
    }
}
