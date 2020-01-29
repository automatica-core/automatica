using System;
using System.IO;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   TCP idle time.
    /// </summary>
    /// <remarks>
    ///   Signals a variable idle timeout.  This
    ///   signalling encourages the use of long-lived TCP connections by
    ///   allowing the state associated with TCP transport to be managed
    ///   effectively with minimal impact on the DNS transaction time.
    /// </remarks>
    /// <seealso href="https://tools.ietf.org/html/rfc7828"/>
    public class EdnsKeepaliveOption : EdnsOption
    {
        /// <summary>
        ///   Creates a new instance of the <see cref="EdnsKeepaliveOption"/> class.
        /// </summary>
        public EdnsKeepaliveOption()
        {
            Type = EdnsOptionType.Keepalive;
        }

        /// <summary>
        ///   The idle timeout value for the TCP connection.
        /// </summary>
        /// <value>
        ///   The resolution is 100 milliseconds.
        /// </value>
        public TimeSpan? Timeout { get; set; }

        /// <inheritdoc />
        public override void ReadData(WireReader reader, int length)
        {
            switch (length)
            {
                case 0:
                    Timeout = null;
                    break;
                case 2:
                    Timeout = TimeSpan.FromMilliseconds((int)reader.ReadUInt16() * 100);
                    break;
                default:
                    throw new InvalidDataException($"Invalid EdnsKeepAlive length of '{length}'.");
            }
        }

        /// <inheritdoc />
        public override void WriteData(WireWriter writer)
        {
            if (Timeout.HasValue)
            {
                writer.WriteUInt16((ushort)(Timeout.Value.TotalMilliseconds / 100));
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $";   Keepalive = {Timeout}";
        }

    }
}
