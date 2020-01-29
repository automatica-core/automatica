namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   Andrew File System Database.
    /// </summary>
    /// <remarks>
    ///   Maps a domain name to the name of an AFS cell database server.
    /// </remarks>
    /// <seealso href="https://tools.ietf.org/html/rfc1183"/>
    public class AFSDBRecord : ResourceRecord
    {
        /// <summary>
        ///   Creates a new instance of the <see cref="AFSDBRecord"/> class.
        /// </summary>
        public AFSDBRecord() : base()
        {
            Type = DnsType.AFSDB;
        }

        /// <summary>
        ///  A 16 bit integer which specifies the type of AFS server.
        /// </summary>
        /// <value>
        ///   See <see href="https://tools.ietf.org/html/rfc1183#section-1"/>
        /// </value>
        public ushort Subtype { get; set; }

        /// <summary>
        ///  A domain-name which specifies a host running an AFS server.
        /// </summary>
        /// <value>
        ///   The name of an AFS server.
        /// </value>
        public DomainName Target { get; set; }


        /// <inheritdoc />
        public override void ReadData(WireReader reader, int length)
        {
            Subtype = reader.ReadUInt16();
            Target = reader.ReadDomainName();
        }

        /// <inheritdoc />
        public override void ReadData(PresentationReader reader)
        {
            Subtype = reader.ReadUInt16();
            Target = reader.ReadDomainName();
        }

        /// <inheritdoc />
        public override void WriteData(WireWriter writer)
        {
            writer.WriteUInt16(Subtype);
            writer.WriteDomainName(Target);
        }

        /// <inheritdoc />
        public override void WriteData(PresentationWriter writer)
        {
            writer.WriteUInt16(Subtype);
            writer.WriteDomainName(Target, appendSpace: false);
        }

    }
}
