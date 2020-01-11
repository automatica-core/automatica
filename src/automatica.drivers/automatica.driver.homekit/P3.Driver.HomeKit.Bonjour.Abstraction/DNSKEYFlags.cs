using System;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   The usage of a <see cref="DNSKEYRecord">key</see>.
    /// </summary>
    [Flags]
    public enum DNSKEYFlags : ushort
    {
        /// <summary>
        ///  No specific usage.
        /// </summary>
        None = 0x0000,

        /// <summary>
        ///   Used by a parent zone's <see cref="DSRecord"/>.
        /// </summary>
        SecureEntryPoint = 0x0001,

        /// <summary>
        ///   Used to sign the zone.
        /// </summary>
        /// <remarks>
        ///   When set, the <see cref="ResourceRecord.Name"/> must be the
        ///   name of the zone.
        /// </remarks>
        ZoneKey = 0x0100,
    }

}
