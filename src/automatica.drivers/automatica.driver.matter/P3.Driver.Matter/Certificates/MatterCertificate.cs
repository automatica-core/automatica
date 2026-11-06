using System.Globalization;
using P3.Driver.Matter.Crypto;
using P3.Driver.Matter.Tlv;

namespace P3.Driver.Matter.Certificates
{
    /// <summary>
    /// A Matter operational certificate in its TLV form (spec 6.5). <see cref="ToTbsDer"/> rebuilds the
    /// X.509 TBSCertificate so the ECDSA signature can be checked against the issuer key.
    /// </summary>
    public sealed class MatterCertificate
    {
        public const int TagNodeId = 17;
        public const int TagFirmwareSigningId = 18;
        public const int TagIcacId = 19;
        public const int TagRcacId = 20;
        public const int TagFabricId = 21;
        public const int TagNocCat = 22;

        private static readonly DateTime MatterEpoch = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly Dictionary<int, string> DnOids = new()
        {
            [1] = "2.5.4.3", [2] = "2.5.4.4", [3] = "2.5.4.5", [4] = "2.5.4.6", [5] = "2.5.4.7", [6] = "2.5.4.8",
            [7] = "2.5.4.10", [8] = "2.5.4.11", [9] = "2.5.4.12", [10] = "2.5.4.41", [11] = "2.5.4.42",
            [12] = "2.5.4.43", [13] = "2.5.4.44", [14] = "2.5.4.46", [15] = "2.5.4.65",
            [16] = "0.9.2342.19200300.100.1.25",
            [TagNodeId] = "1.3.6.1.4.1.37244.1.1", [TagFirmwareSigningId] = "1.3.6.1.4.1.37244.1.2",
            [TagIcacId] = "1.3.6.1.4.1.37244.1.3", [TagRcacId] = "1.3.6.1.4.1.37244.1.4",
            [TagFabricId] = "1.3.6.1.4.1.37244.1.5", [TagNocCat] = "1.3.6.1.4.1.37244.1.6"
        };

        private static readonly string[] ExtendedKeyUsageOids =
        {
            null, "1.3.6.1.5.5.7.3.1", "1.3.6.1.5.5.7.3.2", "1.3.6.1.5.5.7.3.3", "1.3.6.1.5.5.7.3.4",
            "1.3.6.1.5.5.7.3.8", "1.3.6.1.5.5.7.3.9"
        };

        public byte[] Encoded { get; }
        public TlvElement Root { get; }
        public byte[] SerialNumber { get; }
        public TlvElement Issuer { get; }
        public TlvElement Subject { get; }
        public uint NotBefore { get; }
        public uint NotAfter { get; }
        public byte[] PublicKey { get; }
        public byte[] Signature { get; }
        public bool IsCa { get; private set; }
        public byte[] SubjectKeyId { get; private set; }
        public byte[] AuthorityKeyId { get; private set; }

        public MatterCertificate(byte[] encoded)
        {
            Encoded = encoded;
            Root = TlvReader.Decode(encoded);
            if (Root.Kind != TlvKind.Structure)
            {
                throw new MatterProtocolException("Matter certificate must be a TLV structure");
            }

            SerialNumber = Root.GetBytes(1);
            if (Root.GetUInt(2) != 1 || Root.GetUInt(7) != 1 || Root.GetUInt(8) != 1)
            {
                throw new MatterProtocolException("Unsupported certificate algorithm");
            }

            Issuer = Root.Require(3);
            NotBefore = (uint)Root.GetUInt(4);
            NotAfter = (uint)Root.GetUInt(5);
            Subject = Root.Require(6);
            PublicKey = Root.GetBytes(9);
            Signature = Root.GetBytes(11);

            var ext = Root.Get(10);
            if (ext != null)
            {
                foreach (var e in ext.Children)
                {
                    switch (e.Tag)
                    {
                        case 1:
                            IsCa = e.GetBool(1);
                            break;
                        case 4:
                            SubjectKeyId = e.AsBytes();
                            break;
                        case 5:
                            AuthorityKeyId = e.AsBytes();
                            break;
                    }
                }
            }
        }

        public ulong? SubjectNodeId => FindUInt(Subject, TagNodeId);
        public ulong? SubjectFabricId => FindUInt(Subject, TagFabricId);
        public ulong? SubjectRcacId => FindUInt(Subject, TagRcacId);
        public ulong? SubjectIcacId => FindUInt(Subject, TagIcacId);
        public ulong? IssuerFabricId => FindUInt(Issuer, TagFabricId);

        public uint[] SubjectCats => Subject.Children.Where(c => c.Tag == TagNocCat).Select(c => (uint)c.AsUInt()).ToArray();

        private static ulong? FindUInt(TlvElement dn, int tag)
        {
            var e = dn.Children.FirstOrDefault(c => c.Tag == tag);
            return e?.AsUInt();
        }

        public bool VerifySignature(byte[] issuerPublicKey)
        {
            return MatterCrypto.Verify(issuerPublicKey, ToTbsDer(), Signature);
        }

        public byte[] ToTbsDer()
        {
            var version = Der.ContextExplicit(0, Der.Integer(2));
            var serial = Der.Integer(SerialNumber);
            var sigAlg = Der.Sequence(Der.Oid("1.2.840.10045.4.3.2"));
            var issuer = EncodeName(Issuer);
            var validity = Der.Sequence(EncodeTime(NotBefore, false), EncodeTime(NotAfter, true));
            var subject = EncodeName(Subject);
            var spki = Der.Sequence(
                Der.Sequence(Der.Oid("1.2.840.10045.2.1"), Der.Oid("1.2.840.10045.3.1.7")),
                Der.BitString(0, PublicKey));

            var parts = new List<byte[]> { version, serial, sigAlg, issuer, validity, subject, spki };
            var ext = Root.Get(10);
            if (ext != null && ext.Children.Count > 0)
            {
                var extensions = ext.Children.Select(EncodeExtension).ToArray();
                parts.Add(Der.ContextExplicit(3, Der.Sequence(extensions)));
            }

            return Der.Sequence(parts.ToArray());
        }

        private static byte[] EncodeName(TlvElement dn)
        {
            var rdns = new List<byte[]>();
            foreach (var attr in dn.Children)
            {
                var tag = (int)attr.Tag;
                var printable = (tag & 0x80) != 0;
                var baseTag = tag & 0x7F;
                if (!DnOids.TryGetValue(baseTag, out var oid))
                {
                    throw new MatterProtocolException($"Unsupported DN attribute tag {tag}");
                }

                byte[] value;
                if (baseTag is TagNodeId or TagFirmwareSigningId or TagIcacId or TagRcacId or TagFabricId)
                {
                    value = Der.Utf8String(attr.AsUInt().ToString("X16", CultureInfo.InvariantCulture));
                }
                else if (baseTag == TagNocCat)
                {
                    value = Der.Utf8String(attr.AsUInt().ToString("X8", CultureInfo.InvariantCulture));
                }
                else if (baseTag == 16)
                {
                    value = Der.Ia5String(attr.AsString());
                }
                else
                {
                    value = printable ? Der.PrintableString(attr.AsString()) : Der.Utf8String(attr.AsString());
                }

                rdns.Add(Der.Set(Der.Sequence(Der.Oid(oid), value)));
            }

            return Der.Sequence(rdns.ToArray());
        }

        private static byte[] EncodeTime(uint matterSeconds, bool isNotAfter)
        {
            if (isNotAfter && matterSeconds == 0)
            {
                return Der.Tlv(0x18, "99991231235959Z"u8.ToArray());
            }

            var t = MatterEpoch.AddSeconds(matterSeconds);
            if (t.Year < 2050)
            {
                return Der.Tlv(0x17, System.Text.Encoding.ASCII.GetBytes(t.ToString("yyMMddHHmmss", CultureInfo.InvariantCulture) + "Z"));
            }

            return Der.Tlv(0x18, System.Text.Encoding.ASCII.GetBytes(t.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + "Z"));
        }

        private static byte[] EncodeExtension(TlvElement e)
        {
            switch (e.Tag)
            {
                case 1:
                {
                    var content = new List<byte[]>();
                    if (e.GetBool(1))
                    {
                        content.Add(Der.Boolean(true));
                    }

                    var pathLen = e.Get(2);
                    if (pathLen != null)
                    {
                        content.Add(Der.Integer((long)pathLen.AsUInt()));
                    }

                    return Der.Sequence(Der.Oid("2.5.29.19"), Der.Boolean(true), Der.OctetString(Der.Sequence(content.ToArray())));
                }
                case 2:
                    return Der.Sequence(Der.Oid("2.5.29.15"), Der.Boolean(true), Der.OctetString(EncodeKeyUsage((uint)e.AsUInt())));
                case 3:
                {
                    var oids = e.Children.Select(c =>
                    {
                        var idx = (int)c.AsUInt();
                        if (idx <= 0 || idx >= ExtendedKeyUsageOids.Length)
                        {
                            throw new MatterProtocolException($"Unknown extended key usage {idx}");
                        }

                        return Der.Oid(ExtendedKeyUsageOids[idx]);
                    }).ToArray();
                    return Der.Sequence(Der.Oid("2.5.29.37"), Der.Boolean(true), Der.OctetString(Der.Sequence(oids)));
                }
                case 4:
                    return Der.Sequence(Der.Oid("2.5.29.14"), Der.OctetString(Der.OctetString(e.AsBytes())));
                case 5:
                    return Der.Sequence(Der.Oid("2.5.29.35"), Der.OctetString(Der.Sequence(Der.ContextImplicitPrimitive(0, e.AsBytes()))));
                case 6:
                    return e.AsBytes();
                default:
                    throw new MatterProtocolException($"Unknown certificate extension tag {e.Tag}");
            }
        }

        /// <summary>KeyUsage as a DER BIT STRING: Matter bit n maps to X.509 bit n (MSB first), trailing zeros dropped.</summary>
        internal static byte[] EncodeKeyUsage(uint bits)
        {
            var bytes = new byte[2];
            for (var i = 0; i < 9; i++)
            {
                if ((bits & (1u << i)) != 0)
                {
                    bytes[i / 8] |= (byte)(0x80 >> (i % 8));
                }
            }

            var len = bytes[1] != 0 ? 2 : 1;
            var last = bytes[len - 1];
            byte unused = 0;
            if (last == 0)
            {
                unused = 0;
            }
            else
            {
                while ((last & (1 << unused)) == 0)
                {
                    unused++;
                }
            }

            return Der.BitString(unused, bytes[..len]);
        }
    }
}
