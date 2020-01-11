using System.Collections.Generic;
using System.Linq;
using SimpleBase;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   Authenticated denial of existence for DNS Resource Record Sets.
    /// </summary>
    /// <remarks>
    ///   Defined by <see href="https://tools.ietf.org/html/rfc5155#section-3">RFC 5155 - DNS Security (DNSSEC) Hashed Authenticated Denial of Existence</see>.
    /// </remarks>
    public class NSEC3Record : ResourceRecord
    {
        /// <summary>
        ///   Creates a new instance of the <see cref="NSEC3Record"/> class.
        /// </summary>
        public NSEC3Record() : base()
        {
            Type = DnsType.NSEC3;
        }

        /// <summary>
        ///   The cryptographic hash algorithm used to create the <see cref="NextHashedOwnerName"/>.
        /// </summary>
        /// <value>
        ///   One of the <see cref="DigestType"/> value.
        /// </value>
        public DigestType HashAlgorithm { get; set; }

        /// <summary>
        ///  Indicates different processing.
        /// </summary>
        public NSEC3Flags Flags { get; set; }

        /// <summary>
        ///   Number of times to perform the <see cref="HashAlgorithm"/>.
        /// </summary>
        public ushort Iterations { get; set; }

        /// <summary>
        ///   Appended to the original owner name before hashing.
        /// </summary>
        /// <remarks>
        ///   Used to defend against pre-calculated dictionary attacks.
        /// </remarks>
        public byte[] Salt { get; set; }

        /// <summary>
        ///   The next hashed owner name that has authoritative data.
        /// </summary>
        public byte[] NextHashedOwnerName { get; set; }

        /// <summary>
        ///   The sequence of RR types present at the NSEC3 RR's owner name.
        /// </summary>
        /// <value>
        ///   Defaults to the empty list.
        /// </value>
        public List<DnsType> Types { get; set; } = new List<DnsType>();

        /// <inheritdoc />
        public override void ReadData(WireReader reader, int length)
        {
            var end = reader.Position + length;

            HashAlgorithm = (DigestType)reader.ReadByte();
            Flags = (NSEC3Flags)reader.ReadByte();
            Iterations = reader.ReadUInt16();
            Salt = reader.ReadByteLengthPrefixedBytes();
            NextHashedOwnerName = reader.ReadByteLengthPrefixedBytes();

            while (reader.Position < end)
            {
                Types.AddRange(reader.ReadBitmap().Select(t => (DnsType)t));
            }
        }

        /// <inheritdoc />
        public override void WriteData(WireWriter writer)
        {
            writer.WriteByte((byte)HashAlgorithm);
            writer.WriteByte((byte)Flags);
            writer.WriteUInt16(Iterations);
            writer.WriteByteLengthPrefixedBytes(Salt);
            writer.WriteByteLengthPrefixedBytes(NextHashedOwnerName);
            writer.WriteBitmap(Types.Select(t => (ushort)t));
        }

        /// <inheritdoc />
        public override void ReadData(PresentationReader reader)
        {
            HashAlgorithm = (DigestType)reader.ReadByte();
            Flags = (NSEC3Flags)reader.ReadByte();
            Iterations = reader.ReadUInt16();

            var salt = reader.ReadString();
            if (salt != "-")
                Salt = Base16.Decode(salt);

            NextHashedOwnerName = Base32.ExtendedHex.Decode(reader.ReadString());

            while (!reader.IsEndOfLine())
            {
                Types.Add(reader.ReadDnsType());
            }
        }

        /// <inheritdoc />
        public override void WriteData(PresentationWriter writer)
        {
            writer.WriteByte((byte)HashAlgorithm);
            writer.WriteByte((byte)Flags);
            writer.WriteUInt16(Iterations);

            if (Salt == null || Salt.Length == 0)
            {
                writer.WriteString("-");
            }
            else
            {
                writer.WriteBase16String(Salt);
            }

            writer.WriteString(Base32.ExtendedHex.Encode(NextHashedOwnerName, padding: false).ToLowerInvariant());

            bool next = false;
            foreach (var type in Types)
            {
                if (next)
                {
                    writer.WriteSpace();
                }
                writer.WriteDnsType(type, appendSpace: false);
                next = true;
            }
        }
    }
}
