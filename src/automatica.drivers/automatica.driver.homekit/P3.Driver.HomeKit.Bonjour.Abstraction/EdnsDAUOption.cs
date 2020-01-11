using System;
using System.Collections.Generic;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   DNSSEC Algorithm Understood.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///  Defined in <see href="https://tools.ietf.org/html/rfc6975">RFC 6975 - 
    ///  Signaling Cryptographic Algorithm Understanding in DNS Security Extensions(DNSSEC)</see>
    ///  </para>
    /// </remarks>
    public class EdnsDAUOption : EdnsOption
    {
        /// <summary>
        ///   Creates a new instance of the <see cref="EdnsDAUOption"/> class.
        /// </summary>
        public EdnsDAUOption()
        {
            Type = EdnsOptionType.DAU;
            Algorithms = new List<SecurityAlgorithm>();
        }

        /// <summary>
        ///   The understood algorithms.
        /// </summary>
        /// <value>
        ///   A list of implemented <see cref="SecurityAlgorithm"/>.
        /// </value>
        public List<SecurityAlgorithm> Algorithms { get; set; }

        /// <summary>
        ///   Create a new instance of the <see cref="EdnsDAUOption"/> class with
        ///   the known/implemented security algorithms.
        /// </summary>
        /// <remarks>
        ///   The <see cref="Algorithms"/> are obtained from the <see cref="SecurityAlgorithmRegistry"/>.
        /// </remarks>
        public static EdnsDAUOption Create()
        {
            var option = new EdnsDAUOption();
            option.Algorithms.AddRange(SecurityAlgorithmRegistry.Algorithms.Keys);
            return option;
        }

        /// <inheritdoc />
        public override void ReadData(WireReader reader, int length)
        {
            Algorithms.Clear();
            for (; length > 0; --length)
            {
                Algorithms.Add((SecurityAlgorithm)reader.ReadByte());
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
            return $";   DAU = {String.Join(", ", Algorithms)}";
        }

    }
}
