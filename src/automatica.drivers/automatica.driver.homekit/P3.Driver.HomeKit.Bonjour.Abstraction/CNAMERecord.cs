namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   The canonical name for an alias.
    /// </summary>
    /// <remarks>
    ///  CNAME RRs cause no additional section processing, but name servers may
    ///  choose to restart the query at the canonical name in certain cases. See
    ///  the description of name server logic in [RFC - 1034] for details.
    /// </remarks>
    public class CNAMERecord : ResourceRecord
    {
        /// <summary>
        ///   Creates a new instance of the <see cref="CNAMERecord"/> class.
        /// </summary>
        public CNAMERecord() : base()
        {
            Type = DnsType.CNAME;
        }

        /// <summary>
        ///  A domain-name which specifies the canonical or primary
        ///  name for the owner. The owner name is an alias.
        /// </summary>
        public DomainName Target { get; set; }


        /// <inheritdoc />
        public override void ReadData(WireReader reader, int length)
        {
            Target = reader.ReadDomainName();
        }

        /// <inheritdoc />
        public override void ReadData(PresentationReader reader)
        {
            Target = reader.ReadDomainName();
        }

        /// <inheritdoc />
        public override void WriteData(WireWriter writer)
        {
            writer.WriteDomainName(Target);
        }

        /// <inheritdoc />
        public override void WriteData(PresentationWriter writer)
        {
            writer.WriteDomainName(Target, appendSpace: false);
        }

    }
}
