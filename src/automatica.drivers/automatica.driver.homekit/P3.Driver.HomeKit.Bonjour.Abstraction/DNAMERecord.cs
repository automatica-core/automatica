namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   Alias for a name and all its subnames.
    /// </summary>
    /// <remarks>
    ///  Alias for a name and all its subnames, unlike <see cref="CNAMERecord"/>, which is an 
    ///  alias for only the exact name. Like a CNAME record, the DNS lookup will continue by 
    ///  retrying the lookup with the new name.
    /// </remarks>
    public class DNAMERecord : ResourceRecord
    {
        /// <summary>
        ///   Creates a new instance of the <see cref="DNAMERecord"/> class.
        /// </summary>
        public DNAMERecord() : base()
        {
            Type = DnsType.DNAME;
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
            writer.WriteDomainName(Target, uncompressed: true);
        }

        /// <inheritdoc />
        public override void WriteData(PresentationWriter writer)
        {
            writer.WriteDomainName(Target, appendSpace: false);
        }

    }
}
