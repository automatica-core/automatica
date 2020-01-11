namespace P3.Driver.HomeKit.Bonjour.Abstraction
{

    /// <summary>
    ///  Identities the security algorithm used by DNSSEC resource records.
    /// </summary>
    /// <remarks>
    ///   The values are maintained by IANA at <see href="https://www.iana.org/assignments/dns-sec-alg-numbers/dns-sec-alg-numbers.xhtml#dns-sec-alg-numbers-1"/>
    ///   <para>
    ///   Implemented security algorithms are obtained from the <see cref="SecurityAlgorithmRegistry"/>.
    ///   </para>
    /// </remarks>
    /// <seealso cref="DNSKEYRecord"/>
    public enum SecurityAlgorithm : byte
    {
        /// <summary>
        ///  Delete DS
        /// </summary>
        DELETE = 0,

        /// <summary>
        ///   RSA/MD5 (deprecated)
        /// </summary>
        /// <remarks>
        ///   Must not be implemented according to <see href="https://tools.ietf.org/html/rfc6944">RFC 6944</see>.
        /// </remarks>
        RSAMD5 = 1,

        /// <summary>
        ///  Diffie-Hellman
        /// </summary>
        DH = 2,

        /// <summary>
        ///  DSA/SHA1
        /// </summary>
        DSA = 3,

        /// <summary>
        ///  RSA/SHA-1
        /// </summary>
        RSASHA1 = 5,

        /// <summary>
        ///  DSA-NSEC3-SHA1
        /// </summary>
        DSANSEC3SHA1 = 6,

        /// <summary>
        ///  RSASHA1-NSEC3-SHA1
        /// </summary>
        RSASHA1NSEC3SHA1 = 7,

        /// <summary>
        ///  RSA/SHA-256
        /// </summary>
        RSASHA256 = 8,

        /// <summary>
        ///  RSA/SHA-512
        /// </summary>
        RSASHA512 = 10,

        /// <summary>
        ///  GOST R 34.10-2001
        /// </summary>
        ECCGOST = 12,

        /// <summary>
        ///  ECDSA Curve P-256 with SHA-256
        /// </summary>
        ECDSAP256SHA256 = 13,

        /// <summary>
        ///  ECDSA Curve P-384 with SHA-384
        /// </summary>
        ECDSAP384SHA384 = 14,

        /// <summary>
        ///  Ed25519
        /// </summary>
        ED25519 = 15,

        /// <summary>
        ///  Ed448
        /// </summary>
        ED448 = 16,

        /// <summary>
        ///  Indirect Keys
        /// </summary>
        INDIRECT = 252,

        /// <summary>
        ///  Private algorithm
        /// </summary>
        PRIVATEDNS = 253,

        /// <summary>
        ///  Private algorithm OID
        /// </summary>
        PRIVATEOID = 254,
    }
}