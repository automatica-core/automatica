using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using P3.Driver.Matter.Crypto;
using P3.Driver.Matter.Tlv;

namespace P3.Driver.Matter.Certificates
{
    /// <summary>
    /// Device attestation material (spec 6.2): DAC + key, PAI and a Certification Declaration.
    /// <see cref="CreateTestCredentials"/> uses the public CSA test PKI (VID 0xFFF1 / PID 0x8000) that
    /// ships with the Matter SDK, which controllers accept for development devices.
    /// </summary>
    public sealed class DeviceAttestation : IDisposable
    {
        public const ushort TestVendorId = 0xFFF1;
        public const ushort TestProductId = 0x8000;

        public byte[] DacCertificate { get; }
        public byte[] PaiCertificate { get; }
        public ECDsa DacKey { get; }
        public byte[] CertificationDeclaration { get; }
        public ushort VendorId { get; }
        public ushort ProductId { get; }

        public DeviceAttestation(byte[] dac, byte[] pai, ECDsa dacKey, byte[] certificationDeclaration, ushort vendorId, ushort productId)
        {
            DacCertificate = dac;
            PaiCertificate = pai;
            DacKey = dacKey;
            CertificationDeclaration = certificationDeclaration;
            VendorId = vendorId;
            ProductId = productId;
        }

        public static DeviceAttestation CreateTestCredentials()
        {
            var dac = PemToDer(ReadResource("Chip-Test-DAC-FFF1-8000-0000-Cert.pem"));
            var pai = PemToDer(ReadResource("Chip-Test-PAI-FFF1-8000-Cert.pem"));
            var dacKey = ECDsa.Create();
            dacKey.ImportFromPem(ReadResource("Chip-Test-DAC-FFF1-8000-0000-Key.pem"));

            using var cdKey = ECDsa.Create();
            cdKey.ImportFromPem(ReadResource("Chip-Test-CD-Signing-Key.pem"));
            using var cdCert = X509Certificate2.CreateFromPem(ReadResource("Chip-Test-CD-Signing-Cert.pem"));
            var ski = cdCert.Extensions.OfType<X509SubjectKeyIdentifierExtension>().First().SubjectKeyIdentifierBytes.ToArray();

            var cd = CreateCertificationDeclaration(TestVendorId, new[] { TestProductId }, 0x000E, cdKey, ski);
            return new DeviceAttestation(dac, pai, dacKey, cd, TestVendorId, TestProductId);
        }

        internal static string ReadResource(string fileName)
        {
            var asm = typeof(DeviceAttestation).Assembly;
            var name = asm.GetManifestResourceNames().First(n => n.EndsWith(fileName, StringComparison.Ordinal));
            using var stream = asm.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static byte[] PemToDer(string pem)
        {
            var fields = PemEncoding.Find(pem);
            return Convert.FromBase64String(pem[fields.Base64Data]);
        }

        /// <summary>The CD TLV content (spec 6.3.1) wrapped in a CMS SignedData without signed attributes.</summary>
        public static byte[] CreateCertificationDeclaration(ushort vendorId, ushort[] productIds, uint deviceTypeId, ECDsa signingKey, byte[] subjectKeyId)
        {
            var content = TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutUInt(0, 1);
                w.PutUInt(1, vendorId);
                w.StartArray(2);
                foreach (var pid in productIds)
                {
                    w.PutUInt(null, pid);
                }

                w.EndContainer();
                w.PutUInt(3, deviceTypeId);
                w.PutString(4, "ZIG20142ZB330003-24");
                w.PutUInt(5, 0);
                w.PutUInt(6, 0);
                w.PutUInt(7, 0x2694);
                w.PutUInt(8, 0);
                w.EndContainer();
            });

            var signature = signingKey.SignData(content, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
            var sha256 = Der.Sequence(Der.Oid("2.16.840.1.101.3.4.2.1"));
            var signerInfo = Der.Sequence(
                Der.Integer(3),
                Der.ContextImplicitPrimitive(0, subjectKeyId),
                sha256,
                Der.Sequence(Der.Oid("1.2.840.10045.4.3.2")),
                Der.OctetString(signature));
            var signedData = Der.Sequence(
                Der.Integer(3),
                Der.Set(sha256),
                Der.Sequence(Der.Oid("1.2.840.113549.1.7.1"), Der.ContextExplicit(0, Der.OctetString(content))),
                Der.Set(signerInfo));
            return Der.Sequence(Der.Oid("1.2.840.113549.1.7.2"), Der.ContextExplicit(0, signedData));
        }

        /// <summary>Signs attestation or NOCSR elements with the DAC key (spec 11.18.4.7 / 11.18.4.9).</summary>
        public byte[] SignWithChallenge(byte[] elements, byte[] attestationChallenge)
        {
            return MatterCrypto.Sign(DacKey, elements.Concat(attestationChallenge).ToArray());
        }

        public void Dispose()
        {
            DacKey.Dispose();
        }
    }
}
