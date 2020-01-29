namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   A domain name pointer.
    /// </summary>
    /// <remarks>
    ///  PTR records cause no additional section processing.  These RRs are used
    ///  in special domains to point to some other location in the domain space.
    ///  These records are simple data, and don't imply any special processing
    ///  similar to that performed by CNAME, which identifies aliases.See the
    ///  description of the IN-ADDR.ARPA domain for an example.
    /// </remarks>
    public class PTRRecord : ResourceRecord
    {
        /// <summary>
        ///   Creates a new instance of the <see cref="PTRRecord"/> class.
        /// </summary>
        public PTRRecord() : base()
        {
            Type = DnsType.PTR;
        }

        /// <summary>
        ///  A domain-name which points to some location in the
        ///  domain name space.
        /// </summary>
        public DomainName DomainName { get; set; }


        /// <inheritdoc />
        public override void ReadData(WireReader reader, int length)
        {
            DomainName = reader.ReadDomainName();
        }

        /// <inheritdoc />
        public override void ReadData(PresentationReader reader)
        {
            DomainName = reader.ReadDomainName();
        }

        /// <inheritdoc />
        public override void WriteData(WireWriter writer)
        {
            writer.WriteDomainName(DomainName);
        }

        /// <inheritdoc />
        public override void WriteData(PresentationWriter writer)
        {
            writer.WriteDomainName(DomainName, appendSpace: false);
        }

    }
}
