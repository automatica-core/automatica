namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   Padding for a <see cref="Message"/>.
    /// </summary>
    /// <remarks>
    ///  Padding is used to frustrate size-based correlation of the encrypted message.
    ///  <para>
    ///  Defined in <see href="https://tools.ietf.org/html/rfc7830">RFC 7830 - The EDNS(0) Padding Option</see>
    ///  </para>
    /// </remarks>
    public class EdnsPaddingOption : EdnsOption
    {
        /// <summary>
        ///   Creates a new instance of the <see cref="EdnsPaddingOption"/> class.
        /// </summary>
        public EdnsPaddingOption()
        {
            Type = EdnsOptionType.Padding;
        }

        /// <summary>
        ///   The padding bytes.
        /// </summary>
        /// <value>
        ///   The bytes used for padding.  Normally all bytes are zero.
        /// </value>
        public byte[] Padding { get; set; }

        /// <inheritdoc />
        public override void ReadData(WireReader reader, int length)
        {
            Padding = reader.ReadBytes(length);
        }

        /// <inheritdoc />
        public override void WriteData(WireWriter writer)
        {
            writer.WriteBytes(Padding);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $";   Padding = {(Padding == null ? "null" : Padding.Length.ToString())}";
        }

    }
}
