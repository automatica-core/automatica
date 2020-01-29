namespace P3.Driver.HomeKit.Bonjour.Abstraction
{

    /// <summary>
    ///  Identities the cryptographic digest algorithm used by the resource records.
    /// </summary>
    /// <remarks>
    ///   The values are maintained by IANA at <see href="https://www.iana.org/assignments/ds-rr-types/ds-rr-types.xhtml#ds-rr-types-1"/>.
    ///   <para>
    ///   Implemented digest algorithms are obtained from the <see cref="DigestRegistry"/>.
    ///   </para>
    /// </remarks>
    /// <seealso cref="ResourceRecord"/>
    /// <seealso href="https://www.ietf.org/rfc/rfc4034.txt">RFC 4035</seealso>
    public enum DigestType : byte
    {
        /// <summary>
        /// SHA-1.
        /// </summary>
        Sha1 = 1,

        /// <summary>
        /// SHA-256
        /// </summary>
        Sha256 = 2,

        /// <summary>
        ///   GOST R 34.11-94.
        /// </summary>
        GostR34_11_94 = 3,

        /// <summary>
        /// SHA-384
        /// </summary>
        Sha384 = 4,

        /// <summary>
        /// SHA-512 (not in IANA registry)
        /// </summary>
        Sha512 = 5,
    }
}