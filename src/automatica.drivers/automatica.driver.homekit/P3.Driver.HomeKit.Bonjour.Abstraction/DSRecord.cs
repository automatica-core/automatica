using System;
using System.IO;
using System.Text;
using SimpleBase;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   Delegation Signer.
    /// </summary>
    /// <remarks>
    ///   Defined in <see href="https://tools.ietf.org/html/rfc4034#section-5">RFC 4034 section 5</see>.
    /// </remarks>
    public class DSRecord : ResourceRecord
    {
        /// <summary>
        ///   Creates a new instance of the <see cref="DSRecord"/> class.
        /// </summary>
        public DSRecord() : base()
        {
            Type = DnsType.DS;
        }

        /// <summary>
        ///   Creates a new instance of the <see cref="DSRecord"/> class
        ///   from the specified <see cref="DNSKEYRecord"/>.
        /// </summary>
        /// <param name="key">
        ///   The dns key to use.
        /// </param>
        /// <param name="force">
        ///   If <b>true</b>, key usage checks are ignored.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   Both <see cref="DNSKEYFlags.ZoneKey"/> and <see cref="DNSKEYFlags.SecureEntryPoint"/>
        ///   must be set.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <see cref="ResourceRecord.Name"/> of the <paramref name="key"/> is missing.
        /// </exception>
        public DSRecord(DNSKEYRecord key, bool force = false) 
            : this()
        {
            // Check the key.
            if (!force)
            {
                if ((key.Flags & DNSKEYFlags.ZoneKey) == DNSKEYFlags.None)
                    throw new ArgumentException("ZoneKey must be set.", "key");
                if ((key.Flags & DNSKEYFlags.SecureEntryPoint) == DNSKEYFlags.None)
                    throw new ArgumentException("SecureEntryPoint must be set.", "key");
            }

            byte[] digest;
            using (var ms = new MemoryStream())
            using (var hasher = DigestRegistry.Create(key.Algorithm))
            {
                var writer = new WireWriter(ms) { CanonicalForm = true };
                writer.WriteDomainName(key.Name);
                key.WriteData(writer);
                ms.Position = 0;
                digest = hasher.ComputeHash(ms);
            }
            Algorithm = key.Algorithm;
            Class = key.Class;
            KeyTag = key.KeyTag();
            Name = key.Name;
            TTL = key.TTL;
            Digest = digest;
            HashAlgorithm = DigestType.Sha1;
        }

        /// <summary>
        ///   The tag of the referenced <see cref="DNSKEYRecord"/>.
        /// </summary>
        public ushort KeyTag { get; set; }

        /// <summary>
        ///   The <see cref="SecurityAlgorithm"/> of the referenced <see cref="DNSKEYRecord"/>.
        /// </summary>
        public SecurityAlgorithm Algorithm {get; set; }

        /// <summary>
        ///   The cryptographic hash algorithm used to create the 
        ///   <see cref="Digest"/>.
        /// </summary>
        /// <value>
        ///   One of the <see cref="DigestType"/> value.
        /// </value>
        public DigestType HashAlgorithm { get; set; }

        /// <summary>
        ///   The digest of the referenced <see cref="DNSKEYRecord"/>.
        /// </summary>
        /// <remarks>
        ///   <c>digest = HashAlgorithm(DNSKEY owner name | DNSKEY RDATA)</c>
        /// </remarks>
        public byte[] Digest { get; set; }

        /// <inheritdoc />
        public override void ReadData(WireReader reader, int length)
        {
            var end = reader.Position + length;

            KeyTag = reader.ReadUInt16();
            Algorithm = (SecurityAlgorithm)reader.ReadByte();
            HashAlgorithm = (DigestType)reader.ReadByte();
            Digest = reader.ReadBytes(end - reader.Position);
        }

        /// <inheritdoc />
        public override void WriteData(WireWriter writer)
        {
            writer.WriteUInt16(KeyTag);
            writer.WriteByte((byte)Algorithm);
            writer.WriteByte((byte)HashAlgorithm);
            writer.WriteBytes(Digest);
        }

        /// <inheritdoc />
        public override void ReadData(PresentationReader reader)
        {
            KeyTag = reader.ReadUInt16();
            Algorithm = (SecurityAlgorithm)reader.ReadByte();
            HashAlgorithm = (DigestType)reader.ReadByte();

            // Whitespace is allowed within the hexadecimal text.
            var sb = new StringBuilder();
            while (!reader.IsEndOfLine())
            {
                sb.Append(reader.ReadString());
            }
            Digest = Base16.Decode(sb.ToString());
        }

        /// <inheritdoc />
        public override void WriteData(PresentationWriter writer)
        {
            writer.WriteUInt16(KeyTag);
            writer.WriteByte((byte)Algorithm);
            writer.WriteByte((byte)HashAlgorithm);
            writer.WriteBase16String(Digest, appendSpace: false);
        }
    }
}
