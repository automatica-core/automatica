namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   Name server ID.
    /// </summary>
    /// <remarks>
    ///  The identity of the name server.
    /// </remarks>
    /// <seealso href="https://tools.ietf.org/html/rfc5001"/>
    public class EdnsNSIDOption : EdnsOption
    {
        /// <summary>
        ///   Creates a new instance of the <see cref="EdnsNSIDOption"/> class.
        /// </summary>
        public EdnsNSIDOption()
        {
            Type = EdnsOptionType.NSID;
        }

        /// <summary>
        ///   The ID of the name server.
        /// </summary>
        /// <value>
        ///   The bytes used to identify the name server.
        /// </value>
        public byte[] Id { get; set; }

        /// <inheritdoc />
        public override void ReadData(WireReader reader, int length)
        {
            Id = reader.ReadBytes(length);
        }

        /// <inheritdoc />
        public override void WriteData(WireWriter writer)
        {
            writer.WriteBytes(Id);
        }

    }
}
