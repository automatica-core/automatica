using System;
using System.IO;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   Base class for all DNS objects.
    /// </summary>
    /// <remarks>
    ///   Provides helper methods for <see cref="IWireSerialiser">wire serialisation</see>,
    ///   cloning and caching.
    /// </remarks>
    public abstract class DnsObject : IWireSerialiser
#if !NETSTANDARD14
        , ICloneable
#endif
    {
        /// <summary>
        ///   When the object was created.
        /// </summary>
        /// <value>
        ///   Local time.
        /// </value>
        /// <remarks>
        ///   Cloning does not alter the value.
        /// </remarks>
        public DateTime CreationTime { get; set; } = DateTime.Now;

        /// <summary>
        ///   Length in bytes of the object when serialised.
        /// </summary>
        /// <returns>
        ///   Numbers of bytes when serialised.
        /// </returns>
        public int Length()
        {
            var writer = new WireWriter(Stream.Null);
            Write(writer);

            return writer.Position;
        }

        /// <summary>
        ///   Makes a deep copy of the object.
        /// </summary>
        /// <returns>
        ///   A deep copy of the dns object.
        /// </returns>
        /// <remarks>
        ///   Uses serialisation to make a copy.
        /// </remarks>
        public virtual object Clone()
        {
            using (var ms = new MemoryStream())
            {
                Write(ms);
                ms.Position = 0;
                var clone = (DnsObject)Read(ms);
                clone.CreationTime = CreationTime;
                return clone;
            }
        }

        /// <summary>
        ///   Makes a deep copy of the object.
        /// </summary>
        /// <typeparam name="T">
        ///   Some type derived from <see cref="DnsObject"/>.
        /// </typeparam>
        /// <returns>
        ///   A deep copy of the dns object.
        /// </returns>
        /// <remarks>
        ///   Use serialisation to make a copy.
        /// </remarks>
        public T Clone<T>() where T : DnsObject
        {
            return (T)Clone();
        }

        /// <summary>
        ///   Reads the DNS object from a byte array.
        /// </summary>
        /// <param name="buffer">
        ///   The source for the DNS object.
        /// </param>
        public IWireSerialiser Read(byte[] buffer)
        {
            return Read(buffer, 0, buffer.Length);
        }

        /// <summary>
        ///   Reads the DNS object from a byte array.
        /// </summary>
        /// <param name="buffer">
        ///   The source for the DNS object.
        /// </param>
        /// <param name="offset">
        ///   The offset into the <paramref name="buffer"/>.
        /// </param>
        /// <param name="count">
        ///   The number of bytes in the <paramref name="buffer"/>.
        /// </param>
        public IWireSerialiser Read(byte[] buffer, int offset, int count)
        {
            using (var ms = new MemoryStream(buffer, offset, count, false))
            {
                return Read(new WireReader(ms));
            }
        }

        /// <summary>
        ///   Reads the DNS object from a stream.
        /// </summary>
        /// <param name="stream">
        ///   The source for the DNS object.
        /// </param>
        public IWireSerialiser Read(Stream stream)
        {
            return Read(new WireReader(stream));
        }

        /// <inheritdoc />
        public abstract IWireSerialiser Read(WireReader reader);

        /// <summary>
        ///   Writes the DNS object to a byte array.
        /// </summary>
        /// <returns>
        ///   A byte array containing the binary representaton of the DNS object.
        /// </returns>
        public byte[] ToByteArray()
        {
            using (var ms = new MemoryStream())
            {
                Write(ms);
                return ms.ToArray();
            }
        }

        /// <summary>
        ///   Writes the DNS object to a stream.
        /// </summary>
        /// <param name="stream">
        ///   The destination for the DNS object.
        /// </param>
        public void Write(Stream stream)
        {
            Write(new WireWriter(stream));
        }

        /// <inheritdoc />
        public abstract void Write(WireWriter writer);

    }
}
