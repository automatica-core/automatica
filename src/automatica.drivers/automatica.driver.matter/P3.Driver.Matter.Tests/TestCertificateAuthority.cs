using System.Security.Cryptography;
using P3.Driver.Matter.Certificates;
using P3.Driver.Matter.Crypto;
using P3.Driver.Matter.Tlv;

namespace P3.Driver.Matter.Tests
{
    /// <summary>Issues Matter TLV certificates (RCAC and NOCs) like a commissioner's fabric CA does.</summary>
    internal sealed class TestCertificateAuthority : IDisposable
    {
        public const ulong RcacId = 0x0000_0000_CAFE_0001;

        public TestCertificateAuthority(ulong fabricId)
        {
            FabricId = fabricId;
            RootKey = MatterCrypto.CreateSigningKey();
            RootPublicKey = MatterCrypto.ExportPublicKey(RootKey);
            RootCert = Build(
                dn => dn.PutUInt(MatterCertificate.TagRcacId, RcacId),
                dn => dn.PutUInt(MatterCertificate.TagRcacId, RcacId),
                RootPublicKey, isCa: true, keyUsage: 0x0060, extendedKeyUsage: null, subjectKeyId: KeyId(RootPublicKey), authorityKeyId: KeyId(RootPublicKey),
                RootKey);
        }

        public ulong FabricId { get; }
        public ECDsa RootKey { get; }
        public byte[] RootPublicKey { get; }
        public byte[] RootCert { get; }

        public byte[] IssueNoc(byte[] subjectPublicKey, ulong nodeId)
        {
            return Build(
                dn => dn.PutUInt(MatterCertificate.TagRcacId, RcacId),
                dn =>
                {
                    dn.PutUInt(MatterCertificate.TagNodeId, nodeId);
                    dn.PutUInt(MatterCertificate.TagFabricId, FabricId);
                },
                subjectPublicKey, isCa: false, keyUsage: 0x0001, extendedKeyUsage: new byte[] { 2, 1 }, subjectKeyId: KeyId(subjectPublicKey),
                authorityKeyId: KeyId(RootPublicKey), RootKey);
        }

        private static byte[] KeyId(byte[] publicKey) => SHA1.HashData(publicKey);

        private static byte[] Build(Action<TlvWriter> issuer, Action<TlvWriter> subject, byte[] publicKey, bool isCa, ushort keyUsage,
            byte[] extendedKeyUsage, byte[] subjectKeyId, byte[] authorityKeyId, ECDsa issuerKey)
        {
            var serial = MatterCrypto.RandomBytes(8);
            serial[0] &= 0x7F;

            byte[] Encode(byte[] signature) => TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutBytes(1, serial);
                w.PutUInt(2, 1);
                w.StartList(3);
                issuer(w);
                w.EndContainer();
                w.PutUInt(4, 0x2A000000);
                w.PutUInt(5, 0);
                w.StartList(6);
                subject(w);
                w.EndContainer();
                w.PutUInt(7, 1);
                w.PutUInt(8, 1);
                w.PutBytes(9, publicKey);
                w.StartList(10);
                w.StartStructure(1);
                w.PutBool(1, isCa);
                w.EndContainer();
                w.PutUInt(2, keyUsage);
                if (extendedKeyUsage != null)
                {
                    w.StartArray(3);
                    foreach (var e in extendedKeyUsage)
                    {
                        w.PutUInt(null, e);
                    }

                    w.EndContainer();
                }

                w.PutBytes(4, subjectKeyId);
                w.PutBytes(5, authorityKeyId);
                w.EndContainer();
                w.PutBytes(11, signature);
                w.EndContainer();
            });

            var unsigned = new MatterCertificate(Encode(new byte[64]));
            var signature = MatterCrypto.Sign(issuerKey, unsigned.ToTbsDer());
            return Encode(signature);
        }

        public void Dispose()
        {
            RootKey.Dispose();
        }
    }
}
