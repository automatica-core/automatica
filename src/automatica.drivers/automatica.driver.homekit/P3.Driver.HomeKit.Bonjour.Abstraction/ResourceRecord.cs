using System;
using System.IO;
using System.Linq;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   Contains some information on the named resource.
    /// </summary>
    /// <remarks>
    ///   The <see cref="ResourceRegistry"/> contains the metadata on known
    ///   resource records. When reading, if the registry does not contain
    ///   the record, then an <see cref="UnknownRecord"/> is used.
    /// </remarks>
    public class ResourceRecord : DnsObject, IPresentationSerialiser
    {
        /// <summary>
        ///   The default time interval that a resource record maybe cached.
        /// </summary>
        /// <value>
        ///   Defaults to 1 day.
        /// </value>
        public static TimeSpan DefaultTTL = TimeSpan.FromDays(1);

        /// <summary>
        ///   The default time interval that a resource record containing
        ///   a host name maybe cached.
        /// </summary>
        /// <value>
        ///   Defaults to 1 day.
        /// </value>
        /// <remarks>
        ///   Host names are in A, AAAA, and HINFO records.
        /// </remarks>
        public static TimeSpan DefaultHostTTL = TimeSpan.FromDays(1);

        /// <summary>
        ///   An owner name, i.e., the name of the node to which this
        ///   resource record pertains.
        /// </summary>
        public DomainName Name { get; set; }

        /// <summary>
        ///   The canonical form of the owner name.
        /// </summary>
        /// <remarks>
        ///   All uppercase US-ASCII letters in the <see cref="Name"/> are
        ///   replaced by the corresponding lowercase US-ASCII letters.
        /// </remarks>
        public string CanonicalName
        {
            // TOOD: Check usage.  Should not be used because its a string.
            get
            {
                return Name.ToCanonical().ToString();
            }
        }

        /// <summary>
        ///    One of the RR TYPE codes.
        /// </summary>
        public DnsType Type { get; set; }

        /// <summary>
        ///    One of the RR CLASS codes.
        /// </summary>
        /// <value>
        ///   Defaults to <see cref="DnsClass.IN"/>.
        /// </value>
        public DnsClass Class { get; set; } = DnsClass.IN;

        /// <summary>
        ///    Specifies the time interval
        ///    that the resource record may be cached before the source
        ///    of the information should again be consulted. 
        /// </summary>
        /// <value>
        ///    The resolution is 1 second. Defaults to 1 day.
        /// </value>
        /// <remarks>
        ///    Zero values are interpreted to mean that the RR can only be
        ///    used for the transaction in progress, and should not be
        ///    cached.
        /// </remarks>
        /// <seealso cref="DefaultTTL"/>
        public TimeSpan TTL { get; set; } = DefaultTTL;

        /// <summary>
        ///   Determines if the <see cref="TTL"/> has expired.
        /// </summary>
        /// <param name="from">
        ///   The time to compare against.  If <b>null</b>, the default value, then
        ///   <see cref="DateTime.Now"/> is used.
        /// </param>
        /// <returns>
        ///   <b>true</b> if the resource is no longer valid; otherwise <b>false</b>.
        /// </returns>
        public bool IsExpired(DateTime? from = null)
        {
            var now = from ?? DateTime.Now;
            return CreationTime + TTL <= now;
        }

        /// <summary>
        ///   The length of the resource specific data.
        /// </summary>
        /// <returns>
        ///   Number of bytes to represent the resource specific data.
        /// </returns>
        /// <remarks>
        ///   This is referred to as the <c>RDLENGTH</c> in the DNS spec.
        /// </remarks>
        public int GetDataLength()
        {
            using (var ms = new MemoryStream())
            {
                var writer = new WireWriter(ms);
                this.WriteData(writer);
                return (int) ms.Length;
            }
        }

        /// <summary>
        ///   The resource specific data.
        /// </summary>
        /// <returns>
        ///   A byte array, never <b>null</b>.
        /// </returns>
        /// <remarks>
        ///   This is referred to as the <c>RDATA</c> in the DNS spec.
        /// </remarks>
        public byte[] GetData()
        {
            using (var ms = new MemoryStream())
            {
                var writer = new WireWriter(ms);
                this.WriteData(writer);
                return ms.ToArray();
            }
        }

        /// <inheritdoc />
        public override IWireSerialiser Read(WireReader reader)
        {
            // Read standard properties of a resource record.
            Name = reader.ReadDomainName();
            Type = (DnsType)reader.ReadUInt16();
            Class = (DnsClass)reader.ReadUInt16();
            TTL = reader.ReadTimeSpan32();
            int length = reader.ReadUInt16();

            // Find a specific class for the TYPE or default
            // to UnknownRecord.
            var specific = ResourceRegistry.Create(Type); 
            specific.Name = Name;
            specific.Type = Type;
            specific.Class = Class;
            specific.TTL = TTL;

            // Read the specific properties of the resource record.
            var end = reader.Position + length;
            specific.ReadData(reader, length);
            if (reader.Position != end)
            {
                throw new InvalidDataException("Found extra data while decoding RDATA.");
            }

            return specific;
        }

        /// <summary>
        ///   Read the data that is specific to the resource record <see cref="Type"/>.
        /// </summary>
        /// <param name="reader">
        ///   The source of the resource record's data.
        /// </param>
        /// <param name="length">
        ///   The length, in bytes, of the data.
        /// </param>
        /// <remarks>
        ///   Derived classes must implement this method.
        /// </remarks>
        public virtual void ReadData(WireReader reader, int length)
        {
        }

        /// <inheritdoc />
        public override void Write(WireWriter writer)
        {
            writer.WriteDomainName(Name);
            writer.WriteUInt16((ushort)Type);
            writer.WriteUInt16((ushort)Class);
            writer.WriteTimeSpan32(TTL);

            writer.PushLengthPrefixedScope();
            WriteData(writer);
            writer.PopLengthPrefixedScope();
        }

        /// <summary>
        ///   Write the data that is specific to the resource record <see cref="System.Type"/>.
        /// </summary>
        /// <param name="writer">
        ///   The destination for the DNS object's data.
        /// </param>
        /// <remarks>
        ///   Derived classes must implement this method.
        /// </remarks>
        public virtual void WriteData(WireWriter writer)
        {
        }

        /// <summary>
        ///   Determines if the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">
        ///   The object to compare.
        /// </param>
        /// <returns>
        ///   <b>true</b> if the specified object is equal to the current object; otherwise, <b>false</b>.
        /// </returns>
        /// <remarks>
        ///   Two Resource Records are considered equal if their <see cref="Name"/>, 
        ///   <see cref="Class"/>, <see cref="Type"/> and <see cref="GetData">data fields</see>
        ///   are equal. Note that the <see cref="TTL"/> field is explicitly 
        ///   excluded from the comparison.
        /// </remarks>
        public override bool Equals(object obj)
        {
            var that = obj as ResourceRecord;
            if (that == null) return false;

            if (this.Name != that.Name) return false;
            if (this.Class != that.Class) return false;
            if (this.Type != that.Type) return false;

            return this.GetData().SequenceEqual(that.GetData());
        }

        /// <summary>
        ///   Value equality.
        /// </summary>
        /// <remarks>
        ///   Two Resource Records are considered equal if their <see cref="Name"/>, 
        ///   <see cref="Class"/>, <see cref="Type"/> and data fields
        ///   are equal. Note that the <see cref="TTL"/> field is explicitly 
        ///   excluded from the comparison.
        /// </remarks>
        public static bool operator ==(ResourceRecord a, ResourceRecord b)
        {
#pragma warning disable IDE0041 // Null check can be simplified
            if (ReferenceEquals(a, b)) return true;
            if (ReferenceEquals(a, null)) return false;
            if (ReferenceEquals(b, null)) return false;
#pragma warning restore IDE0041 // Null check can be simplified

            return a.Equals(b);
        }

        /// <summary>
        ///   Value inequality.
        /// </summary>
        /// <remarks>
        ///   Two Resource Records are considered equal if their <see cref="Name"/>, 
        ///   <see cref="Class"/>, <see cref="Type"/> and data fields
        ///   are equal. Note that the <see cref="TTL"/> field is explicitly 
        ///   excluded from the comparison.
        /// </remarks>
        public static bool operator !=(ResourceRecord a, ResourceRecord b)
        {
#pragma warning disable IDE0041 // Null check can be simplified
            if (ReferenceEquals(a, b)) return false;
            if (ReferenceEquals(a, null)) return true;
            if (ReferenceEquals(b, null)) return true;
#pragma warning restore IDE0041 // Null check can be simplified

            return !a.Equals(b);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return 
                Name?.GetHashCode() ?? 0
                ^ Class.GetHashCode()
                ^ Type.GetHashCode()
                ^ GetData().Aggregate(0, (r, b) => r ^ b.GetHashCode());

        }

        /// <summary>
        ///   Returns the textual representation.
        /// </summary>
        /// <returns>
        ///   The presentation format of this resource record. 
        /// </returns>
        public override string ToString()
        {
            using (var s = new StringWriter())
            {
                Write(new PresentationWriter(s));

                // Trim trailing whitespaces (tab, space, cr, lf, ...)
                var sb = s.GetStringBuilder();
                while (sb.Length > 0 && Char.IsWhiteSpace(sb[sb.Length-1]))
                {
                    --sb.Length;
                }

                return sb.ToString();
            }
        }

        /// <inheritdoc />
        public void Write(PresentationWriter writer)
        {
            writer.WriteDomainName(Name);
            if (TTL != DefaultTTL)
            {
                writer.WriteTimeSpan32(TTL);
            }
            writer.WriteDnsClass(Class);
            writer.WriteDnsType(Type);

            WriteData(writer);
            writer.WriteEndOfLine();
        }

        /// <summary>
        ///   Write the textual representation of the data that is specific to 
        ///   the resource record.
        /// </summary>
        /// <param name="writer">
        ///   The destination for the resource record's data.
        /// </param>
        /// <remarks>
        ///   Derived classes should implement this method.
        ///   <para>
        ///   By default, this will write the hex encoding of
        ///   the <see cref="GetData">RDATA</see> preceeded by
        ///   "\#" and the number integer bytes.
        ///   </para>
        /// </remarks>
        public virtual void WriteData(PresentationWriter writer)
        {
            var rdata = GetData();
            var hasData = rdata.Length > 0;
            writer.WriteStringUnencoded("\\#");
            writer.WriteUInt32((uint)rdata.Length, appendSpace: hasData);
            if (hasData)
                writer.WriteBase16String(rdata, appendSpace: false);
        }

        /// <summary>
        ///   Create a new <see cref="ResourceRecord"/> from the
        ///   specified string.
        /// </summary>
        /// <param name="text">
        ///   The presentation format.
        /// </param>
        public ResourceRecord Read(string text)
        {
            return Read(new PresentationReader(new StringReader(text)));
        }

        /// <inheritdoc />
        public ResourceRecord Read(PresentationReader reader)
        {
            return reader.ReadResourceRecord();
        }

        /// <summary>
        ///   Read the textual representation of the data that is specific to 
        ///   the resource record <see cref="Type"/>.
        /// </summary>
        /// <param name="reader">
        ///   The source of the resource record's data.
        /// </param>
        /// <remarks>
        ///   Derived classes must implement this method.
        /// </remarks>
        public virtual void ReadData(PresentationReader reader)
        {
        }

    }
}
