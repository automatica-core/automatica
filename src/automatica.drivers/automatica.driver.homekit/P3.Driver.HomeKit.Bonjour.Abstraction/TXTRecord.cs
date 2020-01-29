using System.Collections.Generic;
using System.Text;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   Text strings.
    /// </summary>
    /// <remarks>
    ///   TXT RRs are used to hold descriptive text.  The semantics of the text
    ///   depends on the domain where it is found.
    /// </remarks>
    public class TXTRecord : ResourceRecord
    {
        /// <summary>
        ///   Creates a new instance of the <see cref="TXTRecord"/> class.
        /// </summary>
        public TXTRecord() : base()
        {
            Type = DnsType.TXT;
        }

        /// <summary>
        ///  The sequence of strings.
        /// </summary>
        public List<string> Strings { get; set; } = new List<string>();

        /// <inheritdoc />
        public override void ReadData(WireReader reader, int length)
        {
            while (length > 0)
            {
                var s = reader.ReadString();
                Strings.Add(s);
                length -= Encoding.UTF8.GetByteCount(s) + 1;
            }
        }

        /// <inheritdoc />
        public override void ReadData(PresentationReader reader)
        {
            while (!reader.IsEndOfLine())
            {
                Strings.Add(reader.ReadString());
            }
        }



        /// <inheritdoc />
        public override void WriteData(WireWriter writer)
        {
            foreach (var s in Strings)
            {
                writer.WriteString(s);
            }
        }

        /// <inheritdoc />
        public override void WriteData(PresentationWriter writer)
        {
            bool next = false;
            foreach (var s in Strings)
            {
                if (next)
                {
                    writer.WriteSpace();
                }
                writer.WriteString(s, appendSpace: false);
                next = true;
            }
        }

    }
}
