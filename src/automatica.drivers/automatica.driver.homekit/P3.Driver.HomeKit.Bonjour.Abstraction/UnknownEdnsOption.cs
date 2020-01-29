using System;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   An unknown EDNS option.
    /// </summary>
    /// <remarks>
    ///   When an <see cref="EdnsOption"/> is read with a <see cref="EdnsOption.Type"/> that
    ///   is not <see cref="EdnsOptionRegistry">registered</see>, then this is used
    ///   to deserialise the information.
    /// </remarks>
    public class UnknownEdnsOption : EdnsOption
    {
        /// <summary>
        ///   Specfic data for the option.
        /// </summary>
        public byte[] Data { get; set; }

        /// <inheritdoc />
        public override void ReadData(WireReader reader, int length)
        {
            Data = reader.ReadBytes(length);
        }

        /// <inheritdoc />
        public override void WriteData(WireWriter writer)
        {
            writer.WriteBytes(Data);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $";   Type = {Type}; Data = {Convert.ToBase64String(Data)}";
        }

    }
}
