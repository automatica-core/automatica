using System;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   Shared secret key.
    /// </summary>
    /// <remarks>
    ///   Defined in <see href="https://tools.ietf.org/html/rfc2930">RFC 2930</see>.
    /// </remarks>
    public class TKEYRecord : ResourceRecord
    {
        static readonly byte[] NoData = new byte[0];

        /// <summary>
        ///   Creates a new instance of the <see cref="TKEYRecord"/> class.
        /// </summary>
        public TKEYRecord() : base()
        {
            Type = DnsType.TKEY;
            Class = DnsClass.ANY;
            TTL = TimeSpan.Zero;
            var now = DateTime.UtcNow;
            OtherData = NoData;
        }

        /// <summary>
        ///   Identifies the cryptographic algorithm to create.
        /// </summary>
        /// <value>
        ///   Identifies the HMAC alogirthm.
        /// </value>
        /// <remarks>
        ///   The algorithm determines how the secret keying material agreed to 
        ///   using the TKEY RR is actually used to derive the algorithm specific key.
        /// </remarks>
        /// <seealso cref="TSIGRecord.Algorithm"/>
        public DomainName Algorithm { get; set; }

        /// <summary>
        ///   The start date for the <see cref="Key"/>.
        /// </summary>
        /// <value>
        ///   Resolution in seconds.
        /// </value>
        public DateTime Inception { get; set; }

        /// <summary>
        ///   The end date for the <see cref="Key"/>.
        /// </summary>
        /// <value>
        ///   Resolution in seconds.
        /// </value>
        public DateTime Expiration { get; set; }

        /// <summary>
        ///   The key exchange algorithm.
        /// </summary>
        /// <value>
        ///   One of the <see cref="KeyExchangeMode"/> values.
        /// </value>
        public KeyExchangeMode Mode { get; set; }

        /// <summary>
        ///   Expanded error code for TKEY.
        /// </summary>
        public MessageStatus Error { get; set; }

        /// <summary>
        ///   The key exchange data.
        /// </summary>
        /// <value>
        ///   The format depends on the <see cref="Mode"/>.
        /// </value>
        public byte[] Key { get; set; }

        /// <summary>
        ///   Other data.
        /// </summary>
        public byte[] OtherData { get; set; }

        /// <inheritdoc />
        public override void ReadData(WireReader reader, int length)
        {
            Algorithm = reader.ReadDomainName();
            Inception = reader.ReadDateTime32();
            Expiration = reader.ReadDateTime32();
            Mode = (KeyExchangeMode)reader.ReadUInt16();
            Error = (MessageStatus)reader.ReadUInt16();
            Key = reader.ReadUInt16LengthPrefixedBytes();
            OtherData = reader.ReadUInt16LengthPrefixedBytes();
        }

        /// <inheritdoc />
        public override void WriteData(WireWriter writer)
        {
            writer.WriteDomainName(Algorithm);
            writer.WriteDateTime32(Inception);
            writer.WriteDateTime32(Expiration);
            writer.WriteUInt16((ushort)Mode);
            writer.WriteUInt16((ushort)Error);
            writer.WriteUint16LengthPrefixedBytes(Key);
            writer.WriteUint16LengthPrefixedBytes(OtherData);
        }

        /// <inheritdoc />
        public override void ReadData(PresentationReader reader)
        {
            Algorithm = reader.ReadDomainName();
            Inception = reader.ReadDateTime();
            Expiration = reader.ReadDateTime();
            Mode = (KeyExchangeMode)reader.ReadUInt16();
            Error = (MessageStatus)reader.ReadUInt16();
            Key = Convert.FromBase64String(reader.ReadString());
            OtherData = Convert.FromBase64String(reader.ReadString());
        }

        /// <inheritdoc />
        public override void WriteData(PresentationWriter writer)
        {
            writer.WriteDomainName(Algorithm);
            writer.WriteDateTime(Inception);
            writer.WriteDateTime(Expiration);
            writer.WriteUInt16((ushort)Mode);
            writer.WriteUInt16((ushort)Error);
            writer.WriteBase64String(Key);
            writer.WriteBase64String(OtherData ?? NoData, appendSpace: false);
        }
    }

}
