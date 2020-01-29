using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    /// A domain name consists of one or more parts, <see cref="Labels"/>, that are 
    /// conventionally delimited by dots, such as "example.org".
    /// </summary>
    /// <remarks>
    ///   Equality is based on the number of and the case-insenstive contents of <see cref="Labels"/>.
    /// </remarks>
    public class DomainName : IEquatable<DomainName>
    {
        const string dot = ".";
        const char dotChar = '.';
        const string escapedDot = @"\.";
        const string backslash = @"\";
        const char backslashChar = '\\';
        const string escapedBackslash = @"\092";

        /// <summary>
        ///   The root name space.
        /// </summary>
        /// <value>
        ///  The empty string.
        /// </value>
        /// <remarks>
        ///   The DNS is a hierarchical naming system for computers, services, or any 
        ///   resource participating in the Internet. The top of that hierarchy is 
        ///   the root domain. The root domain does not have a formal name and its
        ///   label in the DNS hierarchy is an empty string. 
        /// </remarks>
        public static DomainName Root = new DomainName(String.Empty);

        List<string> labels = new List<string>();

        /// <summary>
        ///   A sequence of labels that make up the domain name.
        /// </summary>
        /// <value>
        ///   A sequece of strings.
        /// </value>
        /// <remarks>
        ///   The last label is the TLD (top level domain).
        /// </remarks>
        public IReadOnlyList<string> Labels => labels;

        /// <summary>
        ///   Creates a new instance of the <see cref="DomainName"/> class from
        ///   the specified name.
        /// </summary>
        /// <param name="name">
        ///   The dot separated labels; such as "example.org".
        /// </param>
        /// <remarks>
        ///   The name can contain backslash to escape a character.
        ///   See <see href="https://tools.ietf.org/html/rfc4343">RFC 4343</see> 
        ///   for the character escaping rules.
        ///   <note>
        ///   To use us backslash in a domain name (highly unusaual), you must use a double backslash.
        ///   </note>
        /// </remarks>
        public DomainName(string name)
        {
            Parse(name);
        }

        /// <summary>
        ///   Creates a new instance of the <see cref="DomainName"/> class from
        ///   the sequence of label.
        /// </summary>
        /// <param name="labels">
        ///   The <see cref="Labels"/>.
        /// </param>
        /// <remarks>
        ///   The labels are not parsed; character escaping is not performed.
        /// </remarks>
        public DomainName(params string[] labels)
        {
            this.labels.AddRange(labels);
        }

        /// <summary>
        ///   Combine multiple domain names to form one.
        /// </summary>
        /// <param name="names">
        ///   The domain names to join.
        /// </param>
        /// <returns>
        ///   A new domain containing all the <paramref name="names"/>.
        /// </returns>
        public static DomainName Join(params DomainName[] names)
        {
            var joinedName = new DomainName();
            foreach (var name in names)
            {
                joinedName.labels.AddRange(name.Labels);
            }
            return joinedName;
        }

        /// <summary>
        ///   Returns the textual representation.
        /// </summary>
        /// <returns>
        ///   The concatenation of the <see cref="Labels"/> separated by a dot.
        /// </returns>
        /// <remarks>
        ///   If a label contains a dot or backslash, then it is escaped with a backslash.
        /// </remarks>
        public override string ToString()
        {
            return string.Join(dot, Labels.Select(EscapeLabel));
        }

        string EscapeLabel(string label)
        {
            var sb = new StringBuilder();
            foreach (var c in label)
            {
                if (c == backslashChar)
                {
                    sb.Append(escapedBackslash);
                }
                else if (c == dotChar)
                {
                    sb.Append(escapedDot);
                }
                else if (c <= 32 || c > 0x7E)
                {
                    sb.Append(backslashChar);
                    sb.Append(((int)c).ToString("000", CultureInfo.InvariantCulture));
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        ///   Gets the canonical form of the domain name.
        /// </summary>
        /// <returns>
        ///   A domain name in the canonical form.
        /// </returns>
        /// <remarks>
        ///   All uppercase US-ASCII letters in the <see cref="Labels"/> are
        ///   replaced by the corresponding lowercase US-ASCII letters.
        /// </remarks>
        public DomainName ToCanonical()
        {
            var labels = Labels
                .Select(l => l.ToLowerInvariant())
                .ToArray();
            return new DomainName(labels);
        }

        /// <summary>
        ///   Determines if this domain name is a subdomain of or equals an another
        ///   domain name.
        /// </summary>
        /// <param name="domain">
        ///   Another domain.
        /// </param>
        /// <returns>
        ///   <b>true</b> if this domain name is a subdomain of <paramref name="domain"/>
        ///   or equals <paramref name="domain"/>.
        /// </returns>
        public bool BelongsTo(DomainName domain)
        {
            return this == domain || IsSubdomainOf(domain);
        }

        /// <summary>
        ///   Determines if this domain name is a subdomain of another
        ///   domain name.
        /// </summary>
        /// <param name="domain">
        ///   Another domain.
        /// </param>
        /// <returns>
        ///   <b>true</b> if this domain name is a subdomain of <paramref name="domain"/>.
        /// </returns>
        public bool IsSubdomainOf(DomainName domain)
        {
            if (domain == null)
            {
                return false;
            }
            if (labels.Count <= domain.labels.Count)
            {
                return false;
            }
            var i = labels.Count - 1;
            var j = domain.labels.Count - 1;
            for (; 0 <= j; --i, --j)
            {
                if (!LabelsEqual(labels[i], domain.labels[j]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///   Gets the parent's domain name.
        /// </summary>
        /// <returns>
        ///   The domain name of the parent or <b>null</b> if
        ///   there is no parent; e.g. this is the root.
        /// </returns>
        public DomainName Parent()
        {
            if (labels.Count == 0)
            {
                return null;
            }

            return new DomainName(labels.Skip(1).ToArray());
        }

        void Parse(string name)
        {
            labels.Clear();
            var label = new StringBuilder();
            var n = name.Length;
            for (int i = 0; i < n; ++i)
            {
                var c = name[i];

                // An escaped character is either \C or \DDD.
                if (c == '\\')
                {
                    c = name[++i];
                    if (!char.IsDigit(c))
                    {
                        label.Append(c);
                    }
                    else
                    {
                        var number = c - '0';
                        number = (number * 10) + (name[++i] - '0');
                        number = (number * 10) + (name[++i] - '0');
                        label.Append((char)number);
                    }
                    continue;
                }

                // End of label?
                if (c == dotChar)
                {
                    labels.Add(label.ToString());
                    label.Clear();
                    continue;
                }

                // Just part of the label.
                label.Append(c);
            }
            if (label.Length > 0)
            {
                labels.Add(label.ToString());
            }

        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ToString().ToLowerInvariant().GetHashCode();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var that = obj as DomainName;
            return (that == null)
               ? false
               : this.Equals(that);
        }

        /// <inheritdoc />
        public bool Equals(DomainName that)
        {
            if (that is null)
            {
                return false;
            }
            var n = this.labels.Count;
            if (n != that.labels.Count)
            {
                return false;
            }
            for (var i = 0; i < n; ++i)
            {
                if (!LabelsEqual(this.labels[i], that.labels[i]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///   Value equality.
        /// </summary>
        public static bool operator ==(DomainName a, DomainName b)
        {
            if (object.ReferenceEquals(a, b)) return true;
            if (a is null) return false;
            if (b is null) return false;

            return a.Equals(b);
        }

        /// <summary>
        ///   Value inequality.
        /// </summary>
        public static bool operator !=(DomainName a, DomainName b)
        {
            return !(a == b);
        }

        /// <summary>
        ///   Implicit casting of a <see cref="string"/> to a <see cref="DomainName"/>.
        /// </summary>
        /// <param name="s">
        ///   A possibly escaped domain name.
        /// </param>
        /// <returns>
        ///   A new <see cref="DomainName"/>
        /// </returns>
        /// <remarks>
        ///    Equivalent to <code>new DomainName(s)</code>
        /// </remarks>
        public static implicit operator DomainName(string s)
        {
            return new DomainName(s);
        }

        /// <summary>
        ///   Determines if the two domain name labels are equal.
        /// </summary>
        /// <param name="a">A domain name label</param>
        /// <param name="b">A domain name label</param>
        /// <returns>
        ///   <b>true</b> if <paramref name="a"/> and <paramref name="b"/> are
        ///   considered equal.
        /// </returns>
        /// <remarks>
        ///   Uses a case-insenstive algorithm, where 'A-Z' are equivalent to 'a-z'.
        /// </remarks>
        public static bool LabelsEqual(string a, string b)
        {
#if NETSTANDARD14
            return a?.ToLowerInvariant() == b?.ToLowerInvariant();
#else
            return 0 == StringComparer.InvariantCultureIgnoreCase.Compare(a, b);
#endif
        }

    }
}
