namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   Contains some information on an Extended DNS option.
    /// </summary>
    /// <remarks>
    ///   EdnsOptions are found in the <see cref="OPTRecord"/>.
    ///   <para>
    ///   The <see cref="EdnsOptionRegistry"/> contains the metadata on known
    ///   EDNS options. When reading, if the registry does not contain
    ///   the record, then an <see cref="UnknownEdnsOption"/> is used.
    ///   </para>
    /// </remarks>
    /// <seealso cref="OPTRecord"/>
    /// <seealso href="https://www.iana.org/assignments/dns-parameters/dns-parameters.xhtml#dns-parameters-11">IANA - DNS EDNS0 Option Codes</seealso>.
    public abstract class EdnsOption
    {

        /// <summary>
        ///   The option type.
        /// </summary>
        /// <value>
        ///   A code to specify the type of EDNS option.
        /// </value>
        /// <remarks>
        ///   Codes are specified in <see href="https://www.iana.org/assignments/dns-parameters/dns-parameters.xhtml#dns-parameters-11">IANA - DNS EDNS0 Option Codes</see>.
        /// </remarks>
        public EdnsOptionType Type { get; set; }

        /// <summary>
        ///   Read the data that is specific to the option <see cref="EdnsOption.Type"/>.
        /// </summary>
        /// <param name="reader">
        ///   The source of the option's data.
        /// </param>
        /// <param name="length">
        ///   The length, in bytes, of the data.
        /// </param>
        /// <remarks>
        ///   Derived classes must implement this method.
        /// </remarks>
        public abstract void ReadData(WireReader reader, int length);

        /// <summary>
        ///   Write the data that is specific to the resource record <see cref="EdnsOption.Type"/>.
        /// </summary>
        /// <param name="writer">
        ///   The destination for the option's data.
        /// </param>
        /// <remarks>
        ///   Derived classes must implement this method.
        /// </remarks>
        public abstract void WriteData(WireWriter writer);

    }
}
