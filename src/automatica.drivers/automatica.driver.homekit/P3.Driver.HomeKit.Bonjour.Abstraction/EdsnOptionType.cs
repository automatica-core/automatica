namespace P3.Driver.HomeKit.Bonjour.Abstraction
{

    /// <summary>
    ///   EDSN option codes.
    /// </summary>
    /// <remarks>
    ///   Codes are specified in <see href="https://www.iana.org/assignments/dns-parameters/dns-parameters.xhtml#dns-parameters-11">IANA - DNS EDNS0 Option Codes</see>.
    /// </remarks>
    /// <seealso cref="EdnsOption.Type"/>
    /// <seealso cref="OPTRecord"/>
    /// <seealso cref="EdnsOptionRegistry"/>
    public enum EdnsOptionType : ushort
    {

        /// <summary>
        ///   DNS Name Server Identifier (NSID) Option.
        /// </summary>
        /// <seealso cref="EdnsNSIDOption"/>
        /// <seealso href="https://tools.ietf.org/html/rfc5001"/>
        NSID = 3,

        /// <summary>
        ///   DNSSEC Algorithm Understood.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc6975"/>
        DAU = 5,

        /// <summary>
        ///   DS Hash Understood.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc6975"/>
        DHU = 6,

        /// <summary>
        ///   NSEC3 Hash Understood.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc6975"/>
        N3U = 7,

        /// <summary>
        ///   Client Subnet in DNS Queries.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc7871"/>
        ClientSubnet = 8,

        /// <summary>
        ///   Extension Mechanisms for DNS (EDNS) EXPIRE Option.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc7314"/>
        Expire = 9,

        /// <summary>
        ///   Domain Name System (DNS) Cookies.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc7873"/>
        Cookie = 10,

        /// <summary>
        ///   The edns-tcp-keepalive EDNS0 Option.
        /// </summary>
        /// <seealso cref="EdnsKeepaliveOption"/>
        /// <seealso href="https://tools.ietf.org/html/rfc7828"/>
        Keepalive = 11,

        /// <summary>
        ///   The EDNS(0) Padding Option.
        /// </summary>
        /// <seealso cref="EdnsPaddingOption"/>
        /// <seealso href="https://tools.ietf.org/html/rfc7830"/>
        Padding = 12,

        /// <summary>
        ///   CHAIN Query Requests in DNS.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc7901"/>
        Chain = 13,

        /// <summary>
        ///    Signaling Trust Anchor Knowledge in DNSSEC.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc8145"/>
        KeyTag = 14,

        /// <summary>
        ///   Minimum value for local or experiment use.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc6891"/>
        ExperimentalMin = 65001,

        /// <summary>
        ///   Maximum value for local or experiment use.s
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc6891"/>
        ExperimentalMax = 65534,

        /// <summary>
        ///   Reserved for future expansion.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc6891"/>
        FutureExpansion = 65535 
    }
}