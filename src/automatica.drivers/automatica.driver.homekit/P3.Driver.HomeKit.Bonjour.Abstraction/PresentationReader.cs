using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using SimpleBase;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   Methods to read DNS data items encoded in the presentation (text) format.
    /// </summary>
    public class PresentationReader
    {
        static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        System.IO.TextReader text;
        TimeSpan? defaultTTL = null;
        DomainName defaultDomainName = null;
        int parenLevel = 0;
        int previousChar = '\n';  // Assume a newline
        bool eolSeen = false;

        /// <summary>
        ///   Indicates that the token is at the begining of the line without
        ///   any leading whitespace.
        /// </summary>
        bool tokenStartsNewLine = false;

        /// <summary>
        ///   The reader relative position within the stream.
        /// </summary>
        public int Position;

        /// <summary>
        ///   Creates a new instance of the <see cref="PresentationReader"/> using the
        ///   specified <see cref="System.IO.TextReader"/>.
        /// </summary>
        /// <param name="text">
        ///   The source for data items.
        /// </param>
        public PresentationReader(TextReader text)
        {
            this.text = text;
        }

        /// <summary>
        ///   The origin domain name, sometimes called the zone name.
        /// </summary>
        /// <value>
        ///   Defaults to "".
        /// </value>
        /// <remarks>
        ///   <b>Origin</b> is used when the domain name "@" is used
        ///   for a domain name.
        /// </remarks>
        public DomainName Origin { get; set; } = DomainName.Root;

        /// <summary>
        ///   Read a byte.
        /// </summary>
        /// <returns>
        ///   The number as a byte.
        /// </returns>
        public byte ReadByte()
        {
            return byte.Parse(ReadToken(), CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///   Read an unsigned short.
        /// </summary>
        /// <returns>
        ///   The number as an unsigned short.
        /// </returns>
        public ushort ReadUInt16()
        {
            return ushort.Parse(ReadToken(), CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///   Read an unsigned int.
        /// </summary>
        /// <returns>
        ///   The number as an unsignd int.
        /// </returns>
        public uint ReadUInt32()
        {
            return uint.Parse(ReadToken(), CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///   Read a domain name.
        /// </summary>
        /// <returns>
        ///   The domain name as a string.
        /// </returns>
        public DomainName ReadDomainName()
        {
            return MakeAbsoluteDomainName(ReadToken(ignoreEscape: true));
        }

        DomainName MakeAbsoluteDomainName(string name)
        {
            // If an absolute name.
            if (name.EndsWith("."))
                return new DomainName(name.Substring(0, name.Length - 1));

            // Then its a relative name.
            return DomainName.Join(new DomainName(name), Origin);
        }

        /// <summary>
        ///   Read a string.
        /// </summary>
        /// <returns>
        ///   The string.
        /// </returns>
        public string ReadString()
        {
            return ReadToken();
        }

        /// <summary>
        ///   Read bytes encoded in base-64.
        /// </summary>
        /// <returns>
        ///   The bytes.
        /// </returns>
        /// <remarks>
        ///   This must be the last field in the RDATA because the string
        ///   can contain embedded spaces.
        /// </remarks>
        public byte[] ReadBase64String()
        {
            // Handle embedded space and CRLFs inside of parens.
            var sb = new StringBuilder();
            while (!IsEndOfLine())
            {
                sb.Append(ReadToken());
            }
            return Convert.FromBase64String(sb.ToString());
        }

        /// <summary>
        ///   Read a time span (interval) in 16-bit seconds.
        /// </summary>
        /// <returns>
        ///   A <see cref="TimeSpan"/> with second resolution.
        /// </returns>
        public TimeSpan ReadTimeSpan16()
        {
            return TimeSpan.FromSeconds(ReadUInt16());
        }

        /// <summary>
        ///   Read a time span (interval) in 32-bit seconds.
        /// </summary>
        /// <returns>
        ///   A <see cref="TimeSpan"/> with second resolution.
        /// </returns>
        public TimeSpan ReadTimeSpan32()
        {
            return TimeSpan.FromSeconds(ReadUInt32());
        }

        /// <summary>
        ///   Read an Internet address.
        /// </summary>
        /// <param name="length">
        ///   Ignored.
        /// </param>
        /// <returns>
        ///   An <see cref="IPAddress"/>.
        /// </returns>
        public IPAddress ReadIPAddress(int length = 4)
        {
            return IPAddress.Parse(ReadToken());
        }

        /// <summary>
        ///   Read a DNS Type.
        /// </summary>
        /// <remarks>
        ///   Either the name of a <see cref="DnsType"/> or
        ///   the string "TYPEx".
        /// </remarks>
        public DnsType ReadDnsType()
        {
            var token = ReadToken();
            if (token.StartsWith("TYPE"))
            {
                return (DnsType)ushort.Parse(token.Substring(4), CultureInfo.InvariantCulture);
            }
            return (DnsType)Enum.Parse(typeof(DnsType), token);
        }

        /// <summary>
        ///   Read a date/time.
        /// </summary>
        /// <returns>
        ///   The <see cref="DateTime"/>.
        /// </returns>
        /// <remarks>
        ///   Allows a <see cref="DateTime"/> in the form "yyyyMMddHHmmss" or
        ///   the number of seconds since the unix epoch (00:00:00 on 1 January 1970 UTC).
        /// </remarks>
        public DateTime ReadDateTime()
        {
            var token = ReadToken();
            if (token.Length == 14)
            {
                return DateTime.ParseExact(
                    token,
                    "yyyyMMddHHmmss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            }

            return UnixEpoch.AddSeconds(ulong.Parse(token, CultureInfo.InvariantCulture));
        }

        /// <summary>
        ///   Read hex encoded RDATA.
        /// </summary>
        /// <returns>
        ///   A byte array containing the RDATA.
        /// </returns>
        /// <remarks>
        ///   See <see href="https://tools.ietf.org/html/rfc3597#section-5"/> for all
        ///   the details.
        /// </remarks>
        public byte[] ReadResourceData()
        {
            var leadin = ReadToken();
            if (leadin != "#")
                throw new FormatException($"Expected RDATA leadin '\\#', not '{leadin}'.");
            var length = ReadUInt32();
            if (length == 0)
                return new byte[0];

            // Get the hex string.
            var sb = new StringBuilder();
            while (sb.Length < length * 2)
            {
                var word = ReadToken();
                if (word.Length == 0)
                    break;
                if (word.Length % 2 != 0)
                    throw new FormatException($"The hex word ('{word}') must have an even number of digits.");
                sb.Append(word);
            }
            if (sb.Length != length * 2)
                throw new FormatException("Wrong number of RDATA hex digits.");

            // Convert hex string into byte array.
            try
            {
                return Base16.Decode(sb.ToString()).ToArray();
            }
            catch (InvalidOperationException e)
            {
                throw new FormatException(e.Message);
            }
        }

        /// <summary>
        ///   Read a resource record.
        /// </summary>
        /// <returns>
        ///   A <see cref="ResourceRecord"/> or <b>null</b> if no more
        ///   resource records are available.
        /// </returns>
        /// <remarks>
        ///   Processes the "$ORIGIN" and "$TTL" specials that define the
        ///   <see cref="Origin"/> and a default time-to-live respectively.
        ///   <para>
        ///   A domain name can be "@" to refer to the <see cref="Origin"/>. 
        ///   A missing domain name will use the previous record's domain name.
        ///   </para>
        ///   <para>
        ///   Defaults the <see cref="ResourceRecord.Class"/> to <see cref="DnsClass.IN"/>.
        ///   Defaults the <see cref="ResourceRecord.TTL"/>  to either the "$TTL" or
        ///   the <see cref="ResourceRecord.DefaultTTL"/>.
        ///   </para>
        /// </remarks>
        public ResourceRecord ReadResourceRecord()
        {
            DomainName domainName = defaultDomainName;
            DnsClass klass = DnsClass.IN;
            TimeSpan? ttl = defaultTTL;
            DnsType? type = null;

            while (!type.HasValue)
            {
                var token = ReadToken(ignoreEscape: true);
                if (token == "")
                {
                    return null;
                }

                // Domain names and directives must be at the start of a line.
                if (tokenStartsNewLine)
                {
                    switch (token)
                    {
                        case "$ORIGIN":
                            Origin = ReadDomainName();
                            break;
                        case "$TTL":
                            defaultTTL = ttl = ReadTimeSpan32();
                            break;
                        case "@":
                            domainName = Origin;
                            defaultDomainName = domainName;
                            break;
                        default:
                            domainName = MakeAbsoluteDomainName(token);
                            defaultDomainName = domainName;
                            break;
                    }
                    continue;
                }

                // Is TTL?
                if (token.All(c => Char.IsDigit(c)))
                {
                    ttl = TimeSpan.FromSeconds(uint.Parse(token));
                    continue;
                }

                // Is TYPE?
                if (token.StartsWith("TYPE"))
                {
                    type = (DnsType)ushort.Parse(token.Substring(4), CultureInfo.InvariantCulture);
                    continue;
                }
                if (token.ToLowerInvariant() != "any" && Enum.TryParse<DnsType>(token, out DnsType t))
                {
                    type = t;
                    continue;
                }

                // Is CLASS?
                if (token.StartsWith("CLASS"))
                {
                    klass = (DnsClass)ushort.Parse(token.Substring(5), CultureInfo.InvariantCulture);
                    continue;
                }
                if (Enum.TryParse<DnsClass>(token, out DnsClass k))
                {
                    klass = k;
                    continue;
                }

                throw new InvalidDataException($"Unknown token '{token}', expected a Class, Type or TTL.");
            }

            if (domainName == null)
            { 
                throw new InvalidDataException("Missing resource record name.");
            }

            // Create the specific resource record based on the type.
            var resource = ResourceRegistry.Create(type.Value);
            resource.Name = domainName;
            resource.Type = type.Value;
            resource.Class = klass;
            if (ttl.HasValue)
            {
                resource.TTL = ttl.Value;
            }

            // Read the specific properties of the resource record.
            resource.ReadData(this);

            return resource;

        }

        /// <summary>
        ///   Determines if the reader is at the end of a line.
        /// </summary>
        public bool IsEndOfLine()
        {
            int c;
            while (parenLevel > 0)
            {
                while ((c = text.Peek()) >= 0)
                {
                    if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                    {
                        c = text.Read();
                        previousChar = c;
                        continue;
                    }
                    if (c == ')')
                    {
                        --parenLevel;
                        c = text.Read();
                        previousChar = c;
                        break;
                    }
                    return false;
                }

            }

            if (eolSeen)
                return true;

            while ((c = text.Peek()) >= 0)
            {
                // Skip space or tab.
                if (c == ' ' || c == '\t')
                {
                    c = text.Read();
                    previousChar = c;
                    continue;
                }

                return c == '\r' || c == '\n' || c == ';';
            }

            // EOF is end of line
            return true;
        }

        string ReadToken(bool ignoreEscape = false)
        {
            var sb = new StringBuilder();
            int c;
            bool skipWhitespace = true;
            bool inquote = false;
            bool incomment = false;
            eolSeen = false;

            while ((c = text.Read()) >= 0)
            {
                // Comments are terminated by a newline.
                if (incomment)
                {
                    if (c == '\r' || c == '\n')
                    {
                        incomment = false;
                        skipWhitespace = true;
                    }
                    previousChar = c;
                    continue;
                }

                // Handle escaped character.
                if (c == '\\')
                {
                    if (ignoreEscape)
                    {
                        if (sb.Length == 0)
                        {
                            tokenStartsNewLine = previousChar == '\r' || previousChar == '\n';
                        }
                        sb.Append((char)c);
                        previousChar = c;

                        c = text.Read();
                        if (0 <= c)
                        {
                            sb.Append((char)c);
                            previousChar = c;
                        }
                        continue;
                    }
                    previousChar = c;

                    // Handle decimal escapes \DDD
                    int ndigits = 0;
                    int ddd = 0;
                    for (; ndigits <= 3; ++ndigits)
                    {
                        c = text.Peek();
                        if ('0' <= c && c <= '9')
                        {
                            text.Read();
                            ddd = (ddd * 10) + (c - '0');
                            if (ddd > 0xFF)
                                throw new FormatException("Invalid value.");
                        }
                        else
                        {
                            break;
                        }
                    }
                    c = (ndigits > 0) ? ddd : text.Read();

                    sb.Append((char)c);
                    skipWhitespace = false;
                    previousChar = (char)c;
                    continue;
                }

                // Handle quoted strings.
                if (inquote)
                {
                    if (c == '"')
                    {
                        inquote = false;
                        break;
                    }
                    else
                    {
                        sb.Append((char)c);
                    }
                    previousChar = c;
                    continue;
                }
                if (c == '"')
                {
                    inquote = true;
                    previousChar = c;
                    continue;
                }

                // Ignore parens.
                if (c == '(')
                {
                    ++parenLevel;
                    c = ' ';
                }
                if (c == ')')
                {
                    --parenLevel;
                    c = ' ';
                }

                // Skip leading whitespace.
                if (skipWhitespace)
                {
                    if (Char.IsWhiteSpace((char)c))
                    {
                        previousChar = c;
                        continue;
                    }
                    skipWhitespace = false;
                }

                // Trailing whitespace, ends the token.
                if (Char.IsWhiteSpace((char)c))
                {
                    previousChar = c;
                    eolSeen = c == '\r' || c == '\n';
                    break;
                }

                // Handle start of comment.
                if (c == ';')
                {
                    incomment = true;
                    previousChar = c;
                    continue;
                }

                // Default handling, use the character as part of the token.
                if (sb.Length == 0)
                {
                    tokenStartsNewLine = previousChar == '\r' || previousChar == '\n';
                }
                sb.Append((char)c);
                previousChar = c;
            }

            return sb.ToString();
        }

    }
}
