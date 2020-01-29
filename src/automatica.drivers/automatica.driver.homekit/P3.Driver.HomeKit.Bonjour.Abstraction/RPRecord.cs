namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   The person responsible for a name.
    /// </summary>
    /// <remarks>
    ///  The responsible person identification to any name in the DNS.
    /// </remarks>
    /// <seealso href="https://tools.ietf.org/html/rfc1183"/>
    public class RPRecord : ResourceRecord
    {
        /// <summary>
        ///   Creates a new instance of the <see cref="RPRecord"/> class.
        /// </summary>
        public RPRecord() : base()
        {
            Type = DnsType.RP;
        }

        /// <summary>
        ///   The mailbox for the responsible person.
        /// </summary>
        /// <value>
        ///   Defaults to <see cref="DomainName.Root"/>.
        /// </value>
        public DomainName Mailbox { get; set; } = DomainName.Root;

        /// <summary>
        ///   The name of TXT records for the responsible person.
        /// </summary>
        /// <value>
        ///   Defaults to <see cref="DomainName.Root"/>.
        /// </value>
        public DomainName TextName { get; set; } = DomainName.Root;

        /// <inheritdoc />
        public override void ReadData(WireReader reader, int length)
        {
            Mailbox = reader.ReadDomainName();
            TextName = reader.ReadDomainName();
        }

        /// <inheritdoc />
        public override void ReadData(PresentationReader reader)
        {
            Mailbox = reader.ReadDomainName();
            TextName = reader.ReadDomainName();
        }

        /// <inheritdoc />
        public override void WriteData(WireWriter writer)
        {
            writer.WriteDomainName(Mailbox);
            writer.WriteDomainName(TextName);
        }

        /// <inheritdoc />
        public override void WriteData(PresentationWriter writer)
        {
            writer.WriteDomainName(Mailbox);
            writer.WriteDomainName(TextName, appendSpace: false);
        }

    }
}
