namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   An authoritative name server.
    /// </summary>
    /// <remarks>
    ///   NS records cause both the usual additional section processing to locate
    ///   a type A record, and, when used in a referral, a special search of the
    ///   zone in which they reside for glue information.
    ///   
    ///   The NS RR states that the named host should be expected to have a zone
    ///   starting at owner name of the specified class.  Note that the class may
    ///   not indicate the protocol family which should be used to communicate
    ///   with the host, although it is typically a strong hint.For example,
    ///   hosts which are name servers for either Internet (IN) or Hesiod (HS)
    ///   class information are normally queried using IN class protocols.
    /// </remarks>
    public class NSRecord : ResourceRecord
    {
        /// <summary>
        ///   Creates a new instance of the <see cref="NSRecord"/> class.
        /// </summary>
        public NSRecord() : base()
        {
            Type = DnsType.NS;
        }

        /// <summary>
        ///   A domain-name which specifies a host which should be
        ///   authoritative for the specified class and domain.
        /// </summary>
        public DomainName Authority { get; set; }


        /// <inheritdoc />
        public override void ReadData(WireReader reader, int length)
        {
            Authority = reader.ReadDomainName();
        }

        /// <inheritdoc />
        public override void ReadData(PresentationReader reader)
        {
            Authority = reader.ReadDomainName();
        }

        /// <inheritdoc />
        public override void WriteData(WireWriter writer)
        {
            writer.WriteDomainName(Authority);
        }

        /// <inheritdoc />
        public override void WriteData(PresentationWriter writer)
        {
            writer.WriteDomainName(Authority, appendSpace: false);
        }

    }
}
