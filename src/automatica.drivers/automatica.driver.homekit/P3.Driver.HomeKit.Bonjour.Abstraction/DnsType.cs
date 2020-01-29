using System;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{

    /// <summary>
    /// A resource record or query type. 
    /// </summary>
    /// <seealso cref="Question.Type"/>
    /// <seealso cref="ResourceRecord.Type"/>
    public enum DnsType : ushort
    {
        /// <summary>
        /// A host address.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
        /// <seealso cref="ARecord"/>
        A = 1,

        /// <summary>
        /// An authoritative name server.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.11">RFC 1035</seealso>
        /// <seealso cref="NSRecord"/>
        NS = 2,

        /// <summary>
        /// A mail destination (OBSOLETE - use MX).
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
        [Obsolete("Use MX")]
        MD = 3,

        /// <summary>
        /// A mail forwarder (OBSOLETE - use MX).
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
        [Obsolete("Use MX")]
        MF = 4,

        /// <summary>
        /// The canonical name for an alias.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.1">RFC 1035</seealso>
        /// <seealso cref="CNAMERecord"/>
        CNAME = 5,

        /// <summary>
        /// Marks the start of a zone of authority.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.13">RFC 1035</seealso>
        /// <seealso cref="SOARecord"/>
        SOA = 6,

        /// <summary>
        /// A mailbox domain name (EXPERIMENTAL).
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.3">RFC 1035</seealso>
        MB = 7,

        /// <summary>
        /// A mail group member (EXPERIMENTAL).
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.6">RFC 1035</seealso>
        MG = 8,

        /// <summary>
        /// A mailbox rename domain name (EXPERIMENTAL).
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.8">RFC 1035</seealso>
        MR = 9,

        /// <summary>
        /// A Null resource record (EXPERIMENTAL).
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.8">RFC 1035</seealso>
        /// <seealso cref="NULLRecord"/>
        NULL = 10,

        /// <summary>
        /// A well known service description.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc3232">RFC 3232</seealso>
        WKS = 11,

        /// <summary>
        /// A domain name pointer.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.12">RFC 1035</seealso>
        /// <seealso cref="PTRRecord"/>
        PTR = 12,

        /// <summary>
        /// Host information.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.11">RFC 1035</seealso>
        /// <seealso href="https://tools.ietf.org/html/rfc1010">RFC 1010</seealso>
        /// <seealso cref="HINFORecord"/>
        HINFO = 13,

        /// <summary>
        /// Mailbox or mail list information.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.11">RFC 1035</seealso>
        MINFO = 14,

        /// <summary>
        /// Mail exchange.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.9">RFC 1035</seealso>
        /// <seealso href="https://tools.ietf.org/html/rfc974">RFC 974</seealso>
        /// <seealso cref="MXRecord"/>
        MX = 15,

        /// <summary>
        /// Text resources.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3">RFC 1035</seealso>
        /// <seealso href="https://tools.ietf.org/html/rfc1464">RFC 1464</seealso>
        /// <seealso cref="TXTRecord"/>
        TXT = 16,

        /// <summary>
        /// Responsible Person.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1183">RFC 1183</seealso>
        /// <seealso cref="RPRecord"/>
        RP = 17,

        /// <summary>
        /// AFS Data Base location.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1183#section-1">RFC 1183</seealso>
        /// <seealso href="https://tools.ietf.org/html/rfc5864">RFC 5864</seealso>
        /// <seealso cref="AFSDBRecord"/>
        AFSDB = 18,

        /// <summary>
        /// An IPv6 host address.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc3596#section-2.2">RFC 3596</seealso>
        /// <seealso cref="AAAARecord"/>
        AAAA = 28,

        /// <summary>
        /// A resource record which specifies the location of the server(s) for a specific protocol and domain.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc2782">RFC 2782</seealso>
        /// <seealso cref="SRVRecord"/>
        SRV = 33,

        /// <summary>
        ///   Maps an entire domain name.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc6672">RFC 6672</seealso>
        /// <see cref="DNAMERecord"/>
        DNAME = 39,

        /// <summary>
        /// Option record.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc6891">RFC 6891</seealso>
        /// <see cref="OPTRecord"/>
        OPT = 41,

        /// <summary>
        ///   Delegation Signer.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc4034#section-5"/>
        /// <see cref="DSRecord"/>
        DS = 43,

        /// <summary>
        /// Signature for a RRSET with a particular name, class, and type.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc4034#section-3"/>
        /// <seealso cref="RRSIGRecord"/>
        RRSIG = 46,

        /// <summary>
        ///   Next secure owener.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc3845"/>
        /// <seealso cref="NSECRecord"/>
        NSEC = 47,

        /// <summary>
        ///   Public key cryptography to sign and authenticate resource records.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc4034#section-2.1"/>
        /// <seealso cref="DNSKEYRecord"/>
        DNSKEY = 48,

        /// <summary>
        ///   Authenticated next secure owner.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc5155"/>
        /// <seealso cref="NSEC3Record"/>
        NSEC3 = 50,

        /// <summary>
        ///   Parameters needed by authoritative servers to calculate hashed owner names.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc5155#section-4"/>
        /// <seealso cref="NSEC3PARAMRecord"/>
        NSEC3PARAM = 51,

        /// <summary>
        ///   Shared secret key.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc2930"/>
        /// <seealso cref="TKEYRecord"/>
        TKEY = 249,

        /// <summary>
        ///  Transactional Signature.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc2845"/>
        /// <seealso cref="TSIGRecord"/>
        TSIG = 250,

        /// <summary>
        /// A request for a transfer of an entire zone.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
        AXFR = 252,

        /// <summary>
        ///  A request for mailbox-related records (MB, MG or MR).
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
        MAILB = 253,

        /// <summary>
        ///  A request for mail agent RRs (Obsolete - see MX).
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
        [Obsolete("Use MX")]
        MAILA = 254,

        /// <summary>
        ///  A request for any record(s).
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
        ANY = 255,

        /// <summary>
        /// A Uniform Resource Identifier (URI) resource record.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc7553">RFC 7553</seealso>
        URI = 256,

        /// <summary>
        /// A certification authority authorization.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc6844">RFC 6844</seealso>
        CAA = 257,
    }
}