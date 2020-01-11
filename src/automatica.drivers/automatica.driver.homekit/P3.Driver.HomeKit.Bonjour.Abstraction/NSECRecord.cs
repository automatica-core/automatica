using System.Collections.Generic;
using System.Linq;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   Contains the the next owner name and the set of RR
    ///   types present at the NSEC RR's owner name [RFC3845].
    /// </summary>
    public class NSECRecord : ResourceRecord
    {
        /// <summary>
        ///   Creates a new instance of the <see cref="NSECRecord"/> class.
        /// </summary>
        public NSECRecord() : base()
        {
            Type = DnsType.NSEC;
        }

        /// <summary>
        ///   The next owner name that has authoritative data or contains a
        ///   delegation point NS RRset
        /// </summary>
        /// <remarks>
        ///   Defaults to the <see cref="DomainName.Root"/>.
        /// </remarks>
        public DomainName NextOwnerName { get; set; } = DomainName.Root;

        /// <summary>
        ///   The sequence of RR types present at the NSEC RR's owner name.
        /// </summary>
        /// <value>
        ///   Defaults to the empty list.
        /// </value>
        public List<DnsType> Types { get; set; } = new List<DnsType>();

        /// <inheritdoc />
        public override void ReadData(WireReader reader, int length)
        {
            var end = reader.Position + length;
            NextOwnerName = reader.ReadDomainName();
            while (reader.Position < end)
            {
                Types.AddRange(reader.ReadBitmap().Select(t => (DnsType)t));
            }
        }

        /// <inheritdoc />
        public override void WriteData(WireWriter writer)
        {
            writer.WriteDomainName(NextOwnerName, uncompressed: true);
            writer.WriteBitmap(Types.Select(t => (ushort)t));
        }

        /// <inheritdoc />
        public override void ReadData(PresentationReader reader)
        {
            NextOwnerName = reader.ReadDomainName();
            while (!reader.IsEndOfLine())
            {
                Types.Add(reader.ReadDnsType());
            }
        }

        /// <inheritdoc />
        public override void WriteData(PresentationWriter writer)
        {
            writer.WriteDomainName(NextOwnerName);

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
