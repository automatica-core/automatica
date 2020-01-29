using System;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   Transaction Signature.
    /// </summary>
    /// <remarks>
    ///   Defined in <see href="https://tools.ietf.org/html/rfc2845">RFC 2845</see>.
    /// </remarks>
    public class TSIGRecord : ResourceRecord
    {
        static readonly byte[] NoData = new byte[0];

        /// <summary>
        ///  The <see cref="Algorithm"/> name for HMACMD5.
        /// </summary>
        public const string HMACMD5 = "HMAC-MD5.SIG-ALG.REG.INT";

        /// <summary>
        ///  The <see cref="Algorithm"/> name for GSSTSIG.
        /// </summary>
        public const string GSSTSIG = "gss-tsig";

        /// <summary>
        ///  The <see cref="Algorithm"/> name for HMACSHA1.
        /// </summary>
        public const string HMACSHA1 = "hmac-sha1";

        /// <summary>
        ///  The <see cref="Algorithm"/> name for HMACSHA224.
        /// </summary>
        public const string HMACSHA224 = "hmac-sha224";

        /// <summary>
        ///  The <see cref="Algorithm"/> name for HMACSHA256.
        /// </summary>
        public const string HMACSHA256 = "hmac-sha256";

        /// <summary>
        ///  The <see cref="Algorithm"/> name for HMACSHA384.
        /// </summary>
        public const string HMACSHA384 = "hmac-sha384";

        /// <summary>
        ///  The <see cref="Algorithm"/> name for HMACSHA512.
        /// </summary>
        public const string HMACSHA512 = "hmac-sha512";

        /// <summary>
        ///   Creates a new instance of the <see cref="TSIGRecord"/> class.
        /// </summary>
        public TSIGRecord() : base()
        {
            Type = DnsType.TSIG;
            Class = DnsClass.ANY;
            TTL = TimeSpan.Zero;
            var now = DateTime.UtcNow;
            TimeSigned = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Kind);
            Fudge = TimeSpan.FromSeconds(300);
            OtherData = NoData;
        }

        /// <summary>
        ///   Identifies the cryptographic algorithm to create the <see cref="MAC"/>.
        /// </summary>
        /// <value>
        ///   Identifies the HMAC alogirthm.
        /// </value>
        public DomainName Algorithm { get; set; }

        /// <summary>
        ///   When the record was signed.
        /// </summary>
        /// <value>
        ///   Must be in <see cref="DateTimeKind.Utc"/>.
        ///   Resolution in seconds.
        ///   Defaults to <see cref="DateTime.UtcNow"/> less the milliseconds.
        /// </value>
        public DateTime TimeSigned { get; set; }

        /// <summary>
        ///   The message authentication code.
        /// </summary>
        /// <value>
        ///   The format depends on the <see cref="Algorithm"/>.
        /// </value>
        /// <remarks>
        ///   See <see href="https://tools.ietf.org/html/rfc2845#section-3">Protocol Operation</see>
        ///   for details on generating the MAC.
        /// </remarks>
        public byte[] MAC { get; set; }

        /// <summary>
        ///    Permitted error in <see cref="TimeSigned"/>.
        /// </summary>
        /// <value>
        ///   Defaults to 300 seconds.
        /// </value>
        public TimeSpan Fudge { get; set; }

        /// <summary>
        ///   The Original <see cref="Message.Id"/>.
        /// </summary>
        public ushort OriginalMessageId { get; set; }

        /// <summary>
        ///   Expanded error code for TSIG.
        /// </summary>
        /// <value>
        ///   <see cref="MessageStatus.NoError"/>, <see cref="MessageStatus.BadSignature"/>
        ///   <see cref="MessageStatus.BadKey"/> or <see cref="MessageStatus.BadTime"/>.
        /// </value>
        public MessageStatus Error { get; set; }

        /// <summary>
        ///   Other data.
        /// </summary>
        public byte[] OtherData { get; set; }

        /// <inheritdoc />
        public override void ReadData(WireReader reader, int length)
        {
            Algorithm = reader.ReadDomainName();
            TimeSigned = reader.ReadDateTime48();
            Fudge = reader.ReadTimeSpan16();
            MAC = reader.ReadUInt16LengthPrefixedBytes();
            OriginalMessageId = reader.ReadUInt16();
            Error = (MessageStatus)reader.ReadUInt16();
            OtherData = reader.ReadUInt16LengthPrefixedBytes();
        }

        /// <inheritdoc />
        public override void WriteData(WireWriter writer)
        {
            writer.WriteDomainName(Algorithm);
            writer.WriteDateTime48(TimeSigned);
            writer.WriteTimeSpan16(Fudge);
            writer.WriteUint16LengthPrefixedBytes(MAC);
            writer.WriteUInt16(OriginalMessageId);
            writer.WriteUInt16((ushort)Error);
            writer.WriteUint16LengthPrefixedBytes(OtherData);
        }

        /// <inheritdoc />
        public override void ReadData(PresentationReader reader)
        {
            Algorithm = reader.ReadDomainName();
            TimeSigned = reader.ReadDateTime();
            Fudge = reader.ReadTimeSpan16();
            MAC = Convert.FromBase64String(reader.ReadString());
            OriginalMessageId = reader.ReadUInt16();
            Error = (MessageStatus)reader.ReadUInt16();
            OtherData = Convert.FromBase64String(reader.ReadString());
        }

        /// <inheritdoc />
        public override void WriteData(PresentationWriter writer)
        {
            writer.WriteDomainName(Algorithm);
            writer.WriteDateTime(TimeSigned);
            writer.WriteTimeSpan16(Fudge);
            writer.WriteBase64String(MAC);
            writer.WriteUInt16(OriginalMessageId);
            writer.WriteUInt16((ushort)Error);
            writer.WriteBase64String(OtherData ?? NoData, appendSpace: false);
        }
    }

}
