namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   Response codes.
    /// </summary>
    public enum MessageStatus : byte
    {
        /// <summary>
        ///    No error condition
        /// </summary>
        NoError = 0,

        /// <summary>
        ///    The name server was unable to interpret the query.
        /// </summary>
        FormatError = 1,

        /// <summary>
        ///    The name server was unable to process this query due to a
        ///    problem with the name server.
        /// </summary>
        ServerFailure = 2,

        /// <summary>
        ///    Meaningful only for responses from an authoritative name
        ///    server, this code signifies that the domain name 
        ///    referenced in the query does not exist.
        /// </summary>
        NameError = 3,

        /// <summary>
        ///    The name server does not support the requested kind of query.
        /// </summary>
        NotImplemented = 4,

        /// <summary>
        ///    The name server refuses to perform the specified operation for
        ///    policy reasons. 
        /// </summary>
        Refused = 5,

        /// <summary>
        ///    Some name that ought not to exist, does exist.
        /// </summary>
        YXDomain = 6,

        /// <summary>
        ///   Some RRset that ought not to exist, does exist.
        /// </summary>
        YXRRSet = 7,

        /// <summary>
        ///   Some RRset that ought not to exist, does exist.
        /// </summary>
        NXRRSet = 8,

        /// <summary>
        ///   The server is not authoritative for the zone named in the Zone Section.
        /// </summary>
        NotAuthoritative = 9,

        /// <summary>
        ///   A name used in the Prerequisite or Update Section is not within the
        ///   zone denoted by the Zone Section.
        /// </summary>
        NotZone = 10,

        /// <summary>
        ///   Invalid version.
        /// </summary>
        BadVersion = 16,

        /// <summary>
        ///   Invalid signature (TSIG).
        /// </summary>
        BadSignature = 16,

        /// <summary>
        ///   Invalid key (TSIG).
        /// </summary>
        BadKey = 17,

        /// <summary>
        /// Invalid date/time (TSIG).
        /// </summary>
        BadTime = 18,

        /// <summary>
        ///   Bad TKEY mode (TKEY).
        /// </summary>
        BADMODE = 19,

        /// <summary>
        ///   Duplicate key name (TKEY).
        /// </summary>
        BADNAME = 20,

        /// <summary>
        ///   Algorithm not supported (TKEY).
        /// </summary>
        BADALG = 21,
    }

}
