namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///  Identifies the network of the <see cref="ResourceRecord"/>.
    /// </summary>
    /// <remarks>
    ///   The values are maintained by IANA at <see href="https://www.iana.org/assignments/dns-parameters/dns-parameters.xhtml#dns-parameters-2"/>.
    /// </remarks>
    public enum DnsClass : ushort
    {
        /// <summary>
        ///   The Internet.
        /// </summary>
        IN = 1,

        /// <summary>
        ///   The CSNET class (Obsolete - used only for examples insome obsolete RFCs).
        /// </summary>
        CS = 2,

        /// <summary>
        ///   The CHAOS class.
        /// </summary>
        CH = 3,

        /// <summary>
        ///   Hesiod[Dyer 87].
        /// </summary>
        HS = 4,

        /// <summary>
        ///   Used in UPDATE message to signify no class.
        /// </summary>
        None = 254,

        /// <summary>
        ///   Only used in QCLASS.
        /// </summary>
        /// <seealso cref="Question.Class"/>
        ANY = 255
    }
}
