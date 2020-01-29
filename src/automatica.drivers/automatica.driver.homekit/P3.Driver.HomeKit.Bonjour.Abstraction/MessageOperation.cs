namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   The requested operation of a <see cref="Message"/>.
    /// </summary>
    /// <remarks>
    ///   Defines the standard and extended (EDNS(0)) operations.  Standard
    ///   values are between 0 and 15 (0xF). Extended values are between 16 and
    ///   4095 (0xFFF).
    /// </remarks>
    /// <seealso cref="Message.Opcode"/>
    public enum MessageOperation : ushort
    {
        /// <summary>
        ///   Standard query.
        /// </summary>
        Query = 0,

        /// <summary>
        ///   Inverse query (obsolete), see <see href="https://tools.ietf.org/html/rfc3425"/>.
        /// </summary>
        InverseQuery = 1,

        /// <summary>
        ///   A server status request.
        /// </summary>
        Status = 2,

        /// <summary>
        ///   Zone change, see <see href="https://tools.ietf.org/html/rfc1996"/>.
        /// </summary>
        Notify = 4,

        /// <summary>
        ///   Update message, see <see href="https://tools.ietf.org/html/rfc2136"/>.
        /// </summary>
        Update = 5,

    }
}
