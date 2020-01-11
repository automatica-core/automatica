using System;
using System.Collections.Generic;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   DS Hash Understood.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///  Defined in <see href="https://tools.ietf.org/html/rfc6975">RFC 6975 - 
    ///  Signaling Cryptographic Algorithm Understanding in DNS Security Extensions(DNSSEC)</see>
    ///  </para>
    /// </remarks>
    /// <seealso cref="DSRecord"/>
    public class EdnsDHUOption : EdnsOption
    {
        /// <summary>
        ///   Creates a new instance of the <see cref="EdnsDHUOption"/> class.
        /// </summary>
        public EdnsDHUOption()
        {
            Type = EdnsOptionType.DHU;
            Algorithms = new List<DigestType>();
        }

        /// <summary>
        ///   The understood hashing algorithms.
        /// </summary>
        /// <value>
        ///   A list of implemented <see cref="DigestType"/>.
        /// </value>
        public List<DigestType> Algorithms { get; set; }

        /// <summary>
        ///   Create a new instance of the <see cref="EdnsDHUOption"/> class with
        ///   the known/implemented hashing algorithms.
        /// </summary>
        /// <remarks>
        ///   The <see cref="Algorithms"/> are obtained from the <see cref="DigestRegistry"/>.
        /// </remarks>
        public static EdnsDHUOption Create()
        {
            var option = new EdnsDHUOption();
            option.Algorithms.AddRange(DigestRegistry.Digests.Keys);
            return option;
        }

        /// <inheritdoc />
        public override void ReadData(WireReader reader, int length)
        {
            Algorithms.Clear();
            for (; length > 0; --length)
            {
                Algorithms.Add((DigestType)reader.ReadByte());
            }
        }

        /// <inheritdoc />
        public override void WriteData(WireWriter writer)
        {
            foreach (var algorithm in Algorithms)
            {
                writer.WriteByte((byte)algorithm);
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $";   DHU = {String.Join(", ", Algorithms)}";
        }

    }
}
