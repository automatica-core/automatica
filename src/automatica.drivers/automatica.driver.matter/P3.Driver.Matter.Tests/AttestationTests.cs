using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using P3.Driver.Matter.Certificates;
using P3.Driver.Matter.Tlv;

namespace P3.Driver.Matter.Tests
{
    public class AttestationTests
    {
        internal static void AssertChainsToTestPaa(X509Certificate2 dac, X509Certificate2 pai)
        {
            using var paa = X509Certificate2.CreateFromPem(DeviceAttestation.ReadResource("Chip-Test-PAA-FFF1-Cert.pem"));
            using var chain = new X509Chain();
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(paa);
            chain.ChainPolicy.ExtraStore.Add(pai);
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreWrongUsage;
            var ok = chain.Build(dac);
            Assert.True(ok, string.Join(", ", chain.ChainStatus.Select(s => s.StatusInformation)));
            Assert.Equal(3, chain.ChainElements.Count);
        }

        internal static void AssertValidCertificationDeclaration(byte[] cd)
        {
            using var signer = X509Certificate2.CreateFromPem(DeviceAttestation.ReadResource("Chip-Test-CD-Signing-Cert.pem"));
            var cms = new SignedCms();
            cms.Decode(cd);
            cms.CheckSignature(new X509Certificate2Collection(signer), verifySignatureOnly: true);

            var content = TlvReader.Decode(cms.ContentInfo.Content);
            Assert.Equal(1ul, content.GetUInt(0));
            Assert.Equal((ulong)DeviceAttestation.TestVendorId, content.GetUInt(1));
            Assert.Contains((ulong)DeviceAttestation.TestProductId, content.Require(2).Children.Select(c => c.AsUInt()));
        }

        [Fact]
        public void Test_credentials_chain_to_the_test_paa()
        {
            using var attestation = DeviceAttestation.CreateTestCredentials();
            using var dac = X509CertificateLoader.LoadCertificate(attestation.DacCertificate);
            using var pai = X509CertificateLoader.LoadCertificate(attestation.PaiCertificate);
            AssertChainsToTestPaa(dac, pai);
            Assert.Equal(DeviceAttestation.TestVendorId, attestation.VendorId);
        }

        [Fact]
        public void Certification_declaration_verifies_against_the_cd_signing_cert()
        {
            using var attestation = DeviceAttestation.CreateTestCredentials();
            AssertValidCertificationDeclaration(attestation.CertificationDeclaration);
        }

        [Fact]
        public void Dac_key_matches_the_dac()
        {
            using var attestation = DeviceAttestation.CreateTestCredentials();
            using var dac = X509CertificateLoader.LoadCertificate(attestation.DacCertificate);
            var data = new byte[] { 1, 2, 3 };
            var signature = attestation.SignWithChallenge(data, new byte[16]);
            using var pub = dac.GetECDsaPublicKey()!;
            Assert.True(pub.VerifyData(data.Concat(new byte[16]).ToArray(), signature, System.Security.Cryptography.HashAlgorithmName.SHA256));
        }

        [Fact]
        public void Generated_root_certificate_is_self_signed()
        {
            using var ca = new TestCertificateAuthority(1);
            var root = new MatterCertificate(ca.RootCert);
            Assert.True(root.IsCa);
            Assert.True(root.VerifySignature(ca.RootPublicKey));

            using var key = Crypto.MatterCrypto.CreateSigningKey();
            var noc = new MatterCertificate(ca.IssueNoc(Crypto.MatterCrypto.ExportPublicKey(key), 0x42));
            Assert.False(noc.IsCa);
            Assert.Equal(0x42ul, noc.SubjectNodeId);
            Assert.Equal(1ul, noc.SubjectFabricId);
            Assert.True(noc.VerifySignature(ca.RootPublicKey));
        }
    }
}
