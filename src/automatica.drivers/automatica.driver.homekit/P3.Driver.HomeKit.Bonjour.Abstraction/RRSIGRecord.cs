using System;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   Signature for a RRSET with a particular name, class, and type.
    /// </summary>
    /// <remarks>
    ///   Defined in <see href="https://tools.ietf.org/html/rfc4034#section-3">RFC 4034 section 3</see>.
    /// </remarks>
    public class RRSIGRecord : ResourceRecord
    {
        /// <summary>
        ///   Creates a new instance of the <see cref="RRSIGRecord"/> class.
        /// </summary>
        public RRSIGRecord() : base()
        {
            Type = DnsType.RRSIG;
        }

        /// <summary>
        ///   The type of the RRset that is covered by this signature.
        /// </summary>
        /// <value>
        ///   One of the <see cref="DnsType"/> values.
        /// </value>
        public DnsType TypeCovered { get; set; }

        /// <summary>
        ///   Identifies the cryptographic algorithm to create the <see cref="Signature"/>.
        /// </summary>
        /// <value>
        ///   Identifies the type of key (RSA, ECDSA, ...) and the
        ///   hashing algorithm.
        /// </value>
        public SecurityAlgorithm Algorithm { get; set; }

        /// <summary>
        ///   The number of labels in the original RRSIG RR owner name.
        /// </summary>
        /// <remarks>
        ///   The significance of this field is that a validator
        ///   uses it to determine whether the answer was synthesized from a
        ///   wildcard.
        /// </remarks>
        public byte Labels { get; set; }

        /// <summary>
        ///   The TTL of the covered RRset as it appears in the authoritative zone.
        /// </summary>
        /// <value>
        ///   The resolution is 1 second.
        /// </value>
        public TimeSpan OriginalTTL { get; set; }

        /// <summary>
        ///   The end date for the <see cref="Signature"/>.
        /// </summary>
        /// <value>
        ///   The resolution is 1 second.
        /// </value>
        public DateTime SignatureExpiration { get; set; }

        /// <summary>
        ///   The start date for the <see cref="Signature"/>.
        /// </summary>
        /// <value>
        ///   The resolution is 1 second.
        /// </value>
        public DateTime SignatureInception { get; set; }

        /// <summary>
        ///   The key tag of the <see cref="DNSKEYRecord"/> that 
        ///   validates the <see cref="Signature"/>.
        /// </summary>
        /// <value>
        ///   The <see cref="DNSKEYRecord.KeyTag"/> method produces this value.
        /// </value>
        public ushort KeyTag { get; set; }

        /// <summary>
        ///   The owner name of the <see cref="DNSKEYRecord"/> that
        ///   validates the <see cref="Signature"/>.
        /// </summary>
        public DomainName SignerName { get; set; }

        /// <summary>
        ///   The cryptographic signature.
        /// </summary>
        /// <value>
        ///   The format depends upon the <see cref="Algorithm"/>.
        /// </value>
        public byte[] Signature { get; set; }

        /// <inheritdoc />
        public override void ReadData(WireReader reader, int length)
        {
            var end = reader.Position + length;

            TypeCovered = (DnsType)reader.ReadUInt16();
            Algorithm = (SecurityAlgorithm)reader.ReadByte();
            Labels = reader.ReadByte();
            OriginalTTL = reader.ReadTimeSpan32();
            SignatureExpiration = reader.ReadDateTime32();
            SignatureInception = reader.ReadDateTime32();
            KeyTag = reader.ReadUInt16();
            SignerName = reader.ReadDomainName();
            Signature = reader.ReadBytes(end - reader.Position);
        }

        /// <inheritdoc />
        public override void WriteData(WireWriter writer)
        {
            writer.WriteUInt16((ushort)TypeCovered);
            writer.WriteByte((byte)Algorithm);
            writer.WriteByte(Labels);
            writer.WriteTimeSpan32(OriginalTTL);
            writer.WriteDateTime32(SignatureExpiration);
            writer.WriteDateTime32(SignatureInception);
            writer.WriteUInt16(KeyTag);
            writer.WriteDomainName(SignerName, uncompressed: true);
            writer.WriteBytes(Signature);
        }

        /// <inheritdoc />
        public override void ReadData(PresentationReader reader)
        {
            TypeCovered = reader.ReadDnsType();
            Algorithm = (SecurityAlgorithm)reader.ReadByte();
            Labels = reader.ReadByte();
            OriginalTTL = reader.ReadTimeSpan32();
            SignatureExpiration = reader.ReadDateTime();
            SignatureInception = reader.ReadDateTime();
            KeyTag = reader.ReadUInt16();
            SignerName = reader.ReadDomainName();
            Signature = reader.ReadBase64String();
        }

        /// <inheritdoc />
        public override void WriteData(PresentationWriter writer)
        {
            writer.WriteDnsType(TypeCovered);
            writer.WriteByte((byte)Algorithm);
            writer.WriteByte(Labels);
            writer.WriteTimeSpan32(OriginalTTL);
            writer.WriteDateTime(SignatureExpiration);
            writer.WriteDateTime(SignatureInception);
            writer.WriteUInt16(KeyTag);
            writer.WriteDomainName(SignerName);
            writer.WriteBase64String(Signature, appendSpace: false);
        }
    }

}
