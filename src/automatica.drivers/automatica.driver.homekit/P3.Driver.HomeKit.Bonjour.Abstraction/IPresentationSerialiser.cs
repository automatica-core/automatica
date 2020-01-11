using System;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   Presentation format serialisation of a resource record.
    /// </summary>
    /// <remarks>
    ///   The text representation of a <see cref="ResourceRecord"/>.
    ///   It is also referred to as the "master file format".
    ///   See <see href="https://tools.ietf.org/html/rfc1035">RFC 1035 - 5 Master File</see>
    ///   and <see href="https://tools.ietf.org/html/rfc3597">RFC 3597 - Handling of Unknown DNS Resource Record (RR) Types</see>
    ///   for more details.
    ///   <para>
    ///   The <see cref="ResourceRecord"/> adds helper methods to
    ///   deal with a <see cref="String"/>.
    ///   </para>
    /// </remarks>
    public interface IPresentationSerialiser
    {

        /// <summary>
        ///   Reads the text representation of a resource record.
        /// </summary>
        /// <param name="reader">
        ///   The source of the <see cref="ResourceRecord"/>.
        /// </param>
        /// <returns>
        ///   The final resource record.
        /// </returns>
        /// <remarks>
        ///   Reading a <see cref="ResourceRecord"/> will return a new instance that
        ///   is type specific
        /// </remarks>
        ResourceRecord Read(PresentationReader reader);

        /// <summary>
        ///  Writes the text representation of a resource record.
        /// </summary>
        /// <param name="writer">
        ///   The destination of the <see cref="ResourceRecord"/>.
        /// </param>
        void Write(PresentationWriter writer);
    }
}
